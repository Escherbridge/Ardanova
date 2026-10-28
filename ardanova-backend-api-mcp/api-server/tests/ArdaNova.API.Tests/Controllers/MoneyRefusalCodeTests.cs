namespace ArdaNova.API.Tests.Controllers;

using System.Net;
using System.Security.Claims;
using System.Text;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Azoa;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// SR-05 P3a / P3-17: every money refusal carries a stable <c>code</c>. Driven through the
/// controller action with the REAL node clients (<c>AzoaNodeClient.MapError</c> and the typed
/// gateway) - only the node's HTTP answer is faked - so a code the transport dropped would fail here.
/// </summary>
public sealed class MoneyRefusalCodeTests
{
    private const string UserId = "user-1";
    private static readonly Guid AvatarId = Guid.Parse("6f27b1a4-6d34-4f6b-9d6c-a3d2f9c9c111");

    // -- Add funds: what the node answers at checkout --------------------------------------------

    /// <summary>Node status, node code, node message -> BFF status, money code, reason.</summary>
    public static TheoryData<int, string, string, int, string, string?> CheckoutAnswers => new()
    {
        { 403, "KYB_REQUIRED", "Forbidden: business verification is required.", 403, MoneyRefusalCodes.KybRequired, null },
        { 403, "FORBIDDEN", "Consent for this subject is not in force.", 403, MoneyRefusalCodes.ConsentRevoked, null },
        { 401, "FORBIDDEN", "Invalid token.", 401, MoneyRefusalCodes.Unauthenticated, null },
        { 409, "INTENT_FINISHED", "That payment already finished.", 409, MoneyRefusalCodes.Definite, MoneyRefusalReasons.Finished },
        { 409, "INTENT_EXPIRED", "That payment expired unpaid.", 409, MoneyRefusalCodes.Definite, MoneyRefusalReasons.Expired },
        { 409, "REQUEST_KEY_IN_USE", "That key covers a different payment.", 409, MoneyRefusalCodes.Definite, MoneyRefusalReasons.KeyInUse },
        { 409, "INTENT_NEEDS_REVIEW", "That payment is held for review.", 409, MoneyRefusalCodes.Unconfirmed, MoneyRefusalReasons.NeedsReview },
        // Today's node names no outcome on a same-key 409, so the caller must keep its key.
        { 409, "CONFLICT", "That payment conflicts with existing state.", 409, MoneyRefusalCodes.Unconfirmed, null },
        { 422, "REQUEST_DECLINED", "Declined.", 400, MoneyRefusalCodes.Definite, null },
    };

    [Theory]
    [MemberData(nameof(CheckoutAnswers))]
    public async Task Begin_NodeAnswer_IsLabelledByTheNodesOwnCode(
        int nodeStatus, string nodeCode, string nodeMessage, int bffStatus, string moneyCode, string? reason)
    {
        var controller = CreateAddFundsController(RealFiatRail(_ => NodeError(nodeStatus, nodeCode, nodeMessage)));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(bffStatus);
        var body = AsBody(response);
        body.Code.Should().Be(moneyCode);
        body.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task Begin_NodeRateLimited_IsRailBusyWithARetryHint()
    {
        var controller = CreateAddFundsController(RealFiatRail(_ => new HttpResponseMessage((HttpStatusCode)429)));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(503);
        var body = AsBody(response);
        body.Code.Should().Be(MoneyRefusalCodes.RailBusy);
        body.RetryAfterSeconds.Should().Be(30);
    }

    [Fact]
    public async Task Begin_EmptySuccessAnswer_IsUnconfirmedNeverBusy()
    {
        // A 2xx with no body may still be an admitted payment: never "busy", never final.
        var controller = CreateAddFundsController(RealFiatRail(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(503);
        var body = AsBody(response);
        body.Code.Should().Be(MoneyRefusalCodes.Unconfirmed);
        body.RetryAfterSeconds.Should().BeNull();
    }

    [Fact]
    public async Task Begin_OnRampSwitchedOff_MapsToPaymentsNotConfigured()
    {
        // A REAL collaborator: DisabledFiatRailGateway is what AddApplication wires when the on-ramp is off.
        var controller = CreateAddFundsController(new DisabledFiatRailGateway());

        var response = await Begin(controller);

        response.StatusCode.Should().Be(503);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.PaymentsNotConfigured);
    }

    [Fact]
    public async Task Begin_ReadinessGateUnderReview_MapsToUnderReview()
    {
        var controller = CreateAddFundsController(
            new DisabledFiatRailGateway(),
            Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.UnderReview(reviewDueAt: null)));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.UnderReview);
    }

    [Fact]
    public async Task Begin_ReadinessGateSetupIncomplete_MapsToSetupRequired()
    {
        var controller = CreateAddFundsController(
            new DisabledFiatRailGateway(),
            Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("add funds")));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.SetupRequired);
    }

    [Fact]
    public async Task Begin_ReadinessGateCouldNotConfirm_MapsToUnconfirmed()
    {
        var controller = CreateAddFundsController(
            new DisabledFiatRailGateway(),
            Result<AzoaCustodialAccountStatusDto>.Conflict(ValueEligibilityMessages.ServiceUnavailable));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(409);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.Unconfirmed);
    }

    // -- Custodial account: through the controller, the real service, gateway and node client ----

    /// <summary>Node code and message on a 403 -> money code.</summary>
    public static TheoryData<string, string, string> CustodialForbiddenAnswers => new()
    {
        { "KYB_REQUIRED", "Forbidden: business verification is required.", MoneyRefusalCodes.KybRequired },
        // KYC_FORBIDDEN also fires for KYC that was never finished: setup the person still has to do.
        { "FORBIDDEN", "KYC_FORBIDDEN: identity verification is not complete.", MoneyRefusalCodes.SetupRequired },
        // A coded 403 that is not the KYB gate is the tenant's own authorization: configuration.
        { "FORBIDDEN", "Caller lacks the 'tenant:provision' scope.", MoneyRefusalCodes.PaymentsNotConfigured },
    };

    [Theory]
    [MemberData(nameof(CustodialForbiddenAnswers))]
    public async Task BeginKyc_NodeForbidden_IsNamedByTheNodesOwnCode(string nodeCode, string nodeMessage, string moneyCode)
    {
        var controller = CreateCustodialController(_ => NodeError(403, nodeCode, nodeMessage));

        var result = await controller.BeginKyc(CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(moneyCode);
    }

    [Fact]
    public async Task RevokeConsent_NoCustodyTransportConfigured_IsAConfigurationFaultNotKyb()
    {
        // The gateway is built without an IHttpClientFactory, exactly as an unconfigured deployment is.
        var controller = CreateCustodialController(_ => throw new InvalidOperationException("no node call expected"));

        var result = await controller.RevokeConsent(CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(503);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.PaymentsNotConfigured);
    }

    // -- Payouts: through the controller, the real PayoutService and the real cash-out client -----

    [Fact]
    public async Task BeginWithdrawDestinationSetup_NodeRefusesTenantPrincipal_MapsToAccountHolderRequired()
    {
        // F-H8 (oasis-sleek FiatPayoutController.BeginAccountSetup): the only documented 403 on this POST.
        var controller = CreatePayoutsController(_ => NodeError(
            403, "FORBIDDEN", "Payout account setup can only be done by the account holder."));

        var result = await controller.BeginWithdrawDestinationSetup(CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.AccountHolderRequired);
    }

    [Fact]
    public async Task Withdraw_NodeRateLimited_IsRailBusyAnd503()
    {
        // AzoaCashOutClient re-words every refusal, so only the preserved code can still say "429".
        var controller = CreatePayoutsController(_ => new HttpResponseMessage((HttpStatusCode)429));

        var result = await controller.Withdraw(
            new WithdrawRequestDto { RequestKey = "key-1", AmountMinorUnits = 500 }, CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(503);
        var body = AsBody(response);
        body.Code.Should().Be(MoneyRefusalCodes.RailBusy);
        body.RetryAfterSeconds.Should().Be(30);
    }

    [Fact]
    public async Task Withdraw_NodeAnswersWithNoPayload_IsUnconfirmedNeverBusy()
    {
        var controller = CreatePayoutsController(_ => Json(HttpStatusCode.OK, """{ "isError": false }"""));

        var result = await controller.Withdraw(
            new WithdrawRequestDto { RequestKey = "key-1", AmountMinorUnits = 500 }, CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(503);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.Unconfirmed);
    }

    [Fact]
    public async Task GetWithdrawAvailability_CashOutNotWired_MapsToPaymentsNotConfigured()
    {
        var controller = new PayoutsController(new Mock<IPayoutService>().Object, cashOut: null)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextFor(UserId) },
        };

        var result = await controller.GetWithdrawAvailability(CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(503);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.PaymentsNotConfigured);
    }

    // -- helpers ------------------------------------------------------------------------------

    private static async Task<ObjectResult> Begin(AddFundsController controller)
    {
        controller.Request.Headers["X-Idempotency-Key"] = "stable-key-1";
        var result = await controller.Begin(
            new AddFundsRequestDto { AmountMinorUnits = 500 }, CancellationToken.None);
        return result.Should().BeAssignableTo<ObjectResult>().Subject;
    }

    private static MoneyRefusalBody AsBody(ObjectResult response)
    {
        var value = response.Value!;
        var type = value.GetType();
        return new MoneyRefusalBody(
            (string)type.GetProperty("code")!.GetValue(value)!,
            (string)type.GetProperty("message")!.GetValue(value)!,
            (int?)type.GetProperty("retryAfterSeconds")!.GetValue(value),
            (string?)type.GetProperty("reason")!.GetValue(value));
    }

    private static HttpResponseMessage NodeError(int status, string code, string message)
        => Json(
            (HttpStatusCode)status,
            $$"""{ "isError": true, "code": "{{code}}", "message": "{{message}}" }""");

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpClient NodeTransport(Func<HttpRequestMessage, HttpResponseMessage> answer)
        => new(new StubHandler(answer)) { BaseAddress = new Uri("https://azoa.example") };

    private static AzoaFiatRailGateway RealFiatRail(Func<HttpRequestMessage, HttpResponseMessage> answer)
        => new(NodeTransport(answer), NullLogger<AzoaFiatRailGateway>.Instance, new ArdaOnRampOptions());

    private static AddFundsController CreateAddFundsController(
        IFiatRailGateway fiatRail,
        Result<AzoaCustodialAccountStatusDto>? readiness = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AddFundsController.ClientBaseUrlConfigurationKey] = "https://app.example.test",
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Testing");

        return new AddFundsController(
            fiatRail,
            ReadyEligibility(readiness).Object,
            new Mock<IAzoaCustodialAccountService>().Object,
            configuration,
            environment.Object,
            NullLogger<AddFundsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextFor(UserId) },
        };
    }

    private static AzoaCustodialAccountController CreateCustodialController(
        Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var node = new AzoaCustodialNodeClient(NodeTransport(answer), NullLogger<AzoaCustodialNodeClient>.Instance);
        var gateway = new AzoaCustodialAccountGateway(node);
        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { id = UserId });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Azoa:TenantId"] = Guid.NewGuid().ToString() })
            .Build();
        var accounts = new AzoaCustodialAccountService(
            users.Object, new Mock<IUnitOfWork>().Object, gateway, configuration);

        return new AzoaCustodialAccountController(accounts, gateway)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextFor(UserId) },
        };
    }

    private static PayoutsController CreatePayoutsController(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var cashOutNode = new AzoaCashOutClient(NodeTransport(answer), NullLogger<AzoaCashOutClient>.Instance);
        var payouts = new PayoutService(
            new Mock<IRepository<PayoutRequest>>().Object,
            new Mock<IRepository<ProjectTokenConfig>>().Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IMapper>().Object,
            new Mock<ITokenBalanceService>().Object,
            new Mock<IExchangeService>().Object,
            ReadyEligibility().Object,
            cashOutNode: cashOutNode);

        return new PayoutsController(payouts, payouts)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextFor(UserId) },
        };
    }

    private static Mock<IAzoaValueEligibilityService> ReadyEligibility(
        Result<AzoaCustodialAccountStatusDto>? readiness = null)
    {
        var eligibility = new Mock<IAzoaValueEligibilityService>();
        eligibility
            .Setup(v => v.RequireReadyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readiness ?? Result<AzoaCustodialAccountStatusDto>.Success(
                new AzoaCustodialAccountStatusDto { AvatarId = AvatarId.ToString(), Ready = true }));
        return eligibility;
    }

    private static DefaultHttpContext HttpContextFor(string userId) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            "test")),
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(answer(request));
    }
}

namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// SR-05 P3a: every money endpoint's error body carries a stable <c>code</c> alongside its human
/// <c>message</c>. Table-driven over the distinct reachable mappings, driven through the
/// controller action (the highest fast public seam for AddFundsController/PayoutsController; see
/// remarks on <see cref="MoneyRefusal_Resolve_CustodialAccountLifecycleContext_DefaultsToKybRequired"/>
/// for why AzoaCustodialAccountController's own mapping is proven at the MoneyRefusal seam instead).
/// </summary>
public sealed class MoneyRefusalCodeTests
{
    // -- AddFundsController: the units:issue checkout probe (TenantConsentProbe context) --------

    [Fact]
    public async Task Begin_NodeForbiddenAtCheckout_MapsToConsentRevoked()
    {
        // The node's own collapse of a 403 at this route - see MoneyRefusal's class remarks for
        // why consent_revoked is the call-site default rather than a re-read of the node's code.
        var controller = CreateAddFundsController(
            fiatRailResult: Result<FiatCheckoutSession>.Forbidden("AZOA node denied the request."));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(403);
        var body = AsBody(response);
        body.Code.Should().Be(MoneyRefusalCodes.ConsentRevoked);
    }

    [Fact]
    public async Task Begin_NodeKybRequiredMessage_MapsToKybRequired()
    {
        // Forward-compatible path: if a later phase widens AzoaNodeClient to preserve the node's
        // own KYB_REQUIRED-prefixed message, MoneyRefusal reads it directly instead of falling
        // back to the TenantConsentProbe default.
        var controller = CreateAddFundsController(
            fiatRailResult: Result<FiatCheckoutSession>.Forbidden(
                "KYB_REQUIRED: this tenant must complete business verification first."));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.KybRequired);
    }

    [Fact]
    public async Task Begin_OnRampSwitchedOff_MapsToPaymentsNotConfigured()
    {
        // A REAL collaborator, not a mock: DisabledFiatRailGateway is what AddApplication wires up
        // when the on-ramp is off, and it never touches the network.
        var controller = CreateAddFundsController(fiatRail: new DisabledFiatRailGateway());

        var response = await Begin(controller);

        response.StatusCode.Should().Be(503);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.PaymentsNotConfigured);
    }

    [Fact]
    public async Task Begin_FiatRailUnauthorized_MapsToUnauthenticated()
    {
        var controller = CreateAddFundsController(
            fiatRailResult: Result<FiatCheckoutSession>.Unauthorized("AZOA node authentication failed."));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(401);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.Unauthenticated);
    }

    [Fact]
    public async Task Begin_ReadinessGateUnderReview_MapsToUnderReview()
    {
        var controller = CreateAddFundsController(readiness: Result<AzoaCustodialAccountStatusDto>.Forbidden(
            ValueEligibilityMessages.UnderReview(reviewDueAt: null)));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.UnderReview);
    }

    [Fact]
    public async Task Begin_ReadinessGateSetupIncomplete_MapsToSetupRequired()
    {
        var controller = CreateAddFundsController(readiness: Result<AzoaCustodialAccountStatusDto>.Forbidden(
            ValueEligibilityMessages.SetupIncomplete("add funds")));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.SetupRequired);
    }

    [Fact]
    public async Task Begin_ReadinessGateCouldNotConfirm_MapsToUnconfirmed()
    {
        var controller = CreateAddFundsController(readiness: Result<AzoaCustodialAccountStatusDto>.Conflict(
            ValueEligibilityMessages.ServiceUnavailable));

        var response = await Begin(controller);

        response.StatusCode.Should().Be(409);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.Unconfirmed);
    }

    // -- PayoutsController: the payout-destination route family (PayoutDestinationSetup context) -

    [Fact]
    public async Task BeginWithdrawDestinationSetup_NodeForbidden_MapsToAccountHolderRequired()
    {
        // F-H8 (oasis-sleek FiatPayoutController ~112-117) is the only documented 403 on this
        // route family; AzoaCashOutClient.Refused already collapses it to this canned text before
        // the controller sees it.
        var cashOut = new Mock<ICashOutService>();
        cashOut.Setup(c => c.BeginDestinationSetupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CashOutDestination>.Forbidden(
                "Your account is not set up to receive money yet, so nothing was sent. "
                + "Finish setting up where your money goes and try again."));
        var controller = CreatePayoutsController(cashOut.Object);

        var result = await controller.BeginWithdrawDestinationSetup(CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(403);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.AccountHolderRequired);
    }

    [Fact]
    public async Task Withdraw_NodeRateLimited_MapsToRailBusyWithRetryHint()
    {
        var cashOut = new Mock<ICashOutService>();
        cashOut.Setup(c => c.WithdrawAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CashOutRecord>.Failure("AZOA node rate limit exceeded."));
        var controller = CreatePayoutsController(cashOut.Object);

        var result = await controller.Withdraw(
            new WithdrawRequestDto { RequestKey = "key-1", AmountMinorUnits = 500 }, CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(400);
        var body = AsBody(response);
        body.Code.Should().Be(MoneyRefusalCodes.RailBusy);
        body.RetryAfterSeconds.Should().Be(30);
    }

    [Fact]
    public async Task GetWithdrawAvailability_CashOutNotWired_MapsToPaymentsNotConfigured()
    {
        var controller = CreatePayoutsController(cashOut: null);

        var result = await controller.GetWithdrawAvailability(CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(503);
        AsBody(response).Code.Should().Be(MoneyRefusalCodes.PaymentsNotConfigured);
    }

    // -- AzoaCustodialAccountController's mapping, proven at the seam its ToActionResult calls ---

    /// <remarks>
    /// AzoaCustodialAccountController.ToActionResult does nothing beyond
    /// <c>MoneyRefusal.From(result, MoneyRefusalContext.CustodialAccountLifecycle)</c> before
    /// wrapping the body in a status code - so this proves the controller's mapping without
    /// requiring the test project to also reference ArdaNova.Infrastructure for
    /// IAzoaConsentRevocationGateway (a type RevokeConsent's own type-check needs to compile,
    /// unrelated to the refusal-code logic under test here).
    /// </remarks>
    [Fact]
    public void MoneyRefusal_Resolve_CustodialAccountLifecycleContext_DefaultsToKybRequired()
    {
        var refusal = MoneyRefusal.From(
            Result<AzoaCustodialAccountStatusDto>.Forbidden("AZOA node denied the request."),
            MoneyRefusalContext.CustodialAccountLifecycle);

        refusal.Code.Should().Be(MoneyRefusalCodes.KybRequired);
    }

    // -- helpers ------------------------------------------------------------------------------

    private static async Task<ObjectResult> Begin(AddFundsController controller)
    {
        controller.Request.Headers["X-Idempotency-Key"] = "stable-key-1";
        var result = await controller.Begin(
            new AddFundsRequestDto { AmountMinorUnits = 500 }, CancellationToken.None);
        // BadRequestObjectResult/UnauthorizedObjectResult/ConflictObjectResult etc. are all
        // ObjectResult subtypes carrying the same anonymous error body - assignable, not exact.
        return result.Should().BeAssignableTo<ObjectResult>().Subject;
    }

    private static MoneyRefusalBody AsBody(ObjectResult response)
    {
        var value = response.Value!;
        var type = value.GetType();
        var code = (string)type.GetProperty("code")!.GetValue(value)!;
        var message = (string)type.GetProperty("message")!.GetValue(value)!;
        var retryAfterSeconds = (int?)type.GetProperty("retryAfterSeconds")!.GetValue(value);
        return new MoneyRefusalBody(code, message, retryAfterSeconds);
    }

    private static AddFundsController CreateAddFundsController(
        IFiatRailGateway? fiatRail = null,
        Result<FiatCheckoutSession>? fiatRailResult = null,
        Result<AzoaCustodialAccountStatusDto>? readiness = null)
    {
        var resolvedFiatRail = fiatRail ?? StubFiatRail(
            fiatRailResult ?? Result<FiatCheckoutSession>.Forbidden("AZOA node denied the request."));

        var valueEligibility = new Mock<IAzoaValueEligibilityService>();
        valueEligibility
            .Setup(v => v.RequireReadyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readiness ?? Result<AzoaCustodialAccountStatusDto>.Success(
                new AzoaCustodialAccountStatusDto { AvatarId = Guid.NewGuid().ToString(), Ready = true }));

        var accounts = new Mock<IAzoaCustodialAccountService>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AddFundsController.ClientBaseUrlConfigurationKey] = "https://app.example.test",
            })
            .Build();

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Testing");

        var controller = new AddFundsController(
            resolvedFiatRail,
            valueEligibility.Object,
            accounts.Object,
            configuration,
            environment.Object,
            NullLogger<AddFundsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextFor("user-1") },
        };
        return controller;
    }

    private static IFiatRailGateway StubFiatRail(Result<FiatCheckoutSession> result)
    {
        var fiatRail = new Mock<IFiatRailGateway>();
        fiatRail
            .Setup(f => f.BeginAddFundsAsync(It.IsAny<FiatCheckoutStart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return fiatRail.Object;
    }

    private static PayoutsController CreatePayoutsController(ICashOutService? cashOut)
    {
        var payoutService = new Mock<IPayoutService>();
        return new PayoutsController(payoutService.Object, cashOut)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextFor("user-1") },
        };
    }

    private static DefaultHttpContext HttpContextFor(string userId) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            "test")),
    };
}

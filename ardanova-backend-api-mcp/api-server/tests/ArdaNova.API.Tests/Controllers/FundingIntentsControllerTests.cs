namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class FundingIntentsControllerTests
{
    [Fact]
    public async Task CreateCheckout_UsesActorClaimAndRequiredIdempotencyHeader()
    {
        var service = new Mock<IFundingIntentService>();
        service.Setup(item => item.CreateCheckoutAsync(
                It.IsAny<CreateFundingIntentDto>(),
                "user-1",
                "9e68f25a-589c-472a-ab2d-0b4161c5ab89",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FundingCheckoutDto>.Success(new FundingCheckoutDto
            {
                IntentId = "funding-1",
                CheckoutUrl = "https://checkout.stripe.test/cs_123",
            }));
        var controller = CreateController(service.Object, "user-1");
        controller.Request.Headers["X-Idempotency-Key"] = "9e68f25a-589c-472a-ab2d-0b4161c5ab89";

        var result = await controller.CreateCheckout(new CreateFundingIntentDto
        {
            ProjectTokenConfigId = "config-1",
            Amount = "12.34",
            DisclosureVersion = "funding-disclosure-v1",
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        service.VerifyAll();
    }

    [Fact]
    public async Task GetStatus_MapsForeignActorToForbidden()
    {
        var service = new Mock<IFundingIntentService>();
        service.Setup(item => item.GetStatusAsync("funding-1", "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FundingIntentStatusDto>.Forbidden("not yours"));
        var controller = CreateController(service.Object, "user-1");

        var result = await controller.GetStatus("funding-1", CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateCheckout_MapsAccountSetupFailureToReadableForbiddenResponse()
    {
        var service = new Mock<IFundingIntentService>();
        service.Setup(item => item.CreateCheckoutAsync(
                It.IsAny<CreateFundingIntentDto>(),
                "user-1",
                "9e68f25a-589c-472a-ab2d-0b4161c5ab89",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FundingCheckoutDto>.Forbidden(
                "Complete your account setup before you can fund a project."));
        var controller = CreateController(service.Object, "user-1");
        controller.Request.Headers["X-Idempotency-Key"] = "9e68f25a-589c-472a-ab2d-0b4161c5ab89";

        var result = await controller.CreateCheckout(new CreateFundingIntentDto
        {
            ProjectTokenConfigId = "config-1",
            Amount = "12.34",
            DisclosureVersion = "funding-disclosure-v1",
        }, CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(403);
    }

    public static TheoryData<Result<FundingCheckoutDto>, int, string, string?> CheckoutRefusals => new()
    {
        // Pre-persist refusals and a voided key release the caller's key; a final state is resolved
        // through the void route (it may or may not have taken anything); in-progress ones never do.
        { Result<FundingCheckoutDto>.ValidationError("Sponsorship is not open for this project"), 400, MoneyRefusalCodes.Definite, null },
        { Result<FundingCheckoutDto>.Conflict("This sponsorship request was cancelled.").WithCode(UpstreamFailureCodes.RequestVoided), 409, MoneyRefusalCodes.Definite, MoneyRefusalReasons.Voided },
        { Result<FundingCheckoutDto>.Conflict("On hold. Nothing was taken.").WithCode(UpstreamFailureCodes.IntentFinished), 409, MoneyRefusalCodes.Finished, MoneyRefusalReasons.Finished },
        { Result<FundingCheckoutDto>.Conflict("That key was already used for a different sponsorship").WithCode(UpstreamFailureCodes.RequestKeyInUse), 409, MoneyRefusalCodes.Finished, MoneyRefusalReasons.KeyInUse },
        { Result<FundingCheckoutDto>.Conflict("This sponsorship is being completed; check back shortly."), 409, MoneyRefusalCodes.Unconfirmed, null },
        { Result<FundingCheckoutDto>.Failure("The request could not be completed"), 503, MoneyRefusalCodes.Unconfirmed, null },
    };

    [Theory]
    [MemberData(nameof(CheckoutRefusals))]
    public async Task CreateCheckout_RefusalCarriesTheCodeTheCallerReleasesItsKeyOn(
        Result<FundingCheckoutDto> refusal, int status, string moneyCode, string? reason)
    {
        var service = new Mock<IFundingIntentService>();
        service.Setup(item => item.CreateCheckoutAsync(
                It.IsAny<CreateFundingIntentDto>(), "user-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refusal);
        var controller = CreateController(service.Object, "user-1");
        controller.Request.Headers["X-Idempotency-Key"] = "9e68f25a-589c-472a-ab2d-0b4161c5ab89";

        var result = await controller.CreateCheckout(new CreateFundingIntentDto
        {
            ProjectTokenConfigId = "config-1",
            Amount = "12.34",
            DisclosureVersion = "funding-disclosure-v1",
        }, CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(status);
        var body = response.Value!;
        body.GetType().GetProperty("code")!.GetValue(body).Should().Be(moneyCode);
        body.GetType().GetProperty("reason")!.GetValue(body).Should().Be(reason);
    }

    public static TheoryData<Result<FundingIntentVoidDto>, int> FailedVoids => new()
    {
        { Result<FundingIntentVoidDto>.ValidationError("A canonical idempotency key is required"), 400 },
        { Result<FundingIntentVoidDto>.Conflict("This sponsorship is being completed; check back shortly."), 409 },
        { Result<FundingIntentVoidDto>.Failure("The request could not be completed"), 503 },
    };

    [Theory]
    [MemberData(nameof(FailedVoids))]
    public async Task VoidRequest_AFailedVoidNeverCarriesAReleasingCode(Result<FundingIntentVoidDto> refusal, int status)
    {
        var service = new Mock<IFundingIntentService>();
        service.Setup(item => item.VoidRequestAsync("user-1", "key-1", It.IsAny<CancellationToken>())).ReturnsAsync(refusal);
        var controller = CreateController(service.Object, "user-1");

        var result = await controller.VoidRequest("key-1", CancellationToken.None);

        var response = result.Should().BeAssignableTo<ObjectResult>().Subject;
        response.StatusCode.Should().Be(status);
        response.Value!.GetType().GetProperty("code")!.GetValue(response.Value).Should().Be(MoneyRefusalCodes.Unconfirmed);
    }

    private static FundingIntentsController CreateController(IFundingIntentService service, string userId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                "test")),
        };
        return new FundingIntentsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}

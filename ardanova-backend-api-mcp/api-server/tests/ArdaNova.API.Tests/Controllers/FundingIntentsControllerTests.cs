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

namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class AzoaAvatarControllerTests
{
    [Fact]
    public void Controller_RequiresActorAssertionPolicy()
    {
        var authorization = typeof(AzoaAvatarController)
            .GetCustomAttribute<AuthorizeAttribute>();

        authorization.Should().NotBeNull();
        authorization!.Policy.Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    [Fact]
    public async Task GetStatus_UsesSubjectFromVerifiedActorIdentity()
    {
        var avatars = new Mock<IAzoaAvatarService>(MockBehavior.Strict);
        avatars.Setup(service => service.GetStatusAsync("actor-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAvatarStatusDto>.Success(new AzoaAvatarStatusDto()));
        var controller = CreateController(
            avatars.Object,
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "untrusted-user")], "ApiKey"),
            ActorIdentity("actor-123"));

        var result = await controller.GetStatus(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        avatars.Verify(service => service.GetStatusAsync("actor-123", It.IsAny<CancellationToken>()), Times.Once);
        avatars.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatus_WithoutOneVerifiedActorIdentity_FailsBeforeServiceInvocation()
    {
        var avatars = new Mock<IAzoaAvatarService>(MockBehavior.Strict);
        var controller = CreateController(
            avatars.Object,
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "untrusted-user"),
                    new Claim(ActorAssertionMiddleware.ClaimType, "v2"),
                ],
                "ApiKey"));

        var result = await controller.GetStatus(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        avatars.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatus_WithAmbiguousActorIdentities_FailsBeforeServiceInvocation()
    {
        var avatars = new Mock<IAzoaAvatarService>(MockBehavior.Strict);
        var controller = CreateController(
            avatars.Object,
            ActorIdentity("actor-123"),
            ActorIdentity("actor-456"));

        var result = await controller.GetStatus(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        avatars.VerifyNoOtherCalls();
    }

    private static AzoaAvatarController CreateController(
        IAzoaAvatarService avatarService,
        params ClaimsIdentity[] identities)
        => new(avatarService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identities),
                },
            },
        };

    private static ClaimsIdentity ActorIdentity(string actorId)
        => new(
            [
                new Claim(ClaimTypes.NameIdentifier, actorId),
                new Claim(ActorAssertionMiddleware.ClaimType, "v2"),
            ],
            ActorAssertionMiddleware.AuthenticationType);
}

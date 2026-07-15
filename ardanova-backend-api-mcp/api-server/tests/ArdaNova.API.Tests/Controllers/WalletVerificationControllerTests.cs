namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class WalletVerificationControllerTests
{
    [Fact]
    public void WalletController_HasNoDirectVerifyAction()
        => typeof(WalletsController).GetMethod("Verify", BindingFlags.Instance | BindingFlags.Public).Should().BeNull();

    [Fact]
    public async Task IssueVerificationChallenge_UsesAuthenticatedActor()
    {
        var wallets = new Mock<IWalletService>();
        var verification = new Mock<IWalletVerificationService>();
        wallets.Setup(item => item.GetByIdAsync("wallet-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WalletDto>.Success(new WalletDto { Id = "wallet-1", UserId = "actor-1", Address = "ADDRESS", Provider = WalletProvider.PERA }));
        verification.Setup(item => item.IssueAsync("actor-1", "wallet-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WalletVerificationChallengeDto>.Success(new WalletVerificationChallengeDto { ChallengeId = "challenge-1" }));

        var result = await WithActor(new WalletsController(wallets.Object, verification.Object), "actor-1")
            .IssueVerificationChallenge("wallet-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        verification.VerifyAll();
    }

    [Fact]
    public async Task CompleteVerificationChallenge_ForeignWalletIsForbiddenBeforeProofService()
    {
        var wallets = new Mock<IWalletService>();
        var verification = new Mock<IWalletVerificationService>(MockBehavior.Strict);
        wallets.Setup(item => item.GetByIdAsync("wallet-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WalletDto>.Success(new WalletDto { Id = "wallet-1", UserId = "other", Address = "ADDRESS", Provider = WalletProvider.PERA }));

        var result = await WithActor(new WalletsController(wallets.Object, verification.Object), "actor-1")
            .CompleteVerificationChallenge("wallet-1", new CompleteWalletVerificationDto { ChallengeId = "challenge", Nonce = "nonce", Signature = "signature" }, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        verification.VerifyNoOtherCalls();
    }

    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actorId)],
                    "ActorAssertion"))
            }
        };
        return controller;
    }
}

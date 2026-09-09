namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

public class AdminAzoaBindingControllerTests
{
    [Fact]
    public async Task GetBinding_ReturnsBoundPointers_ByUserId()
    {
        var user = MakeUser("user-1", verificationLevel: VerificationLevel.PRO, avatarId: "avatar-1");
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await MakeController(users).GetBinding("user-1", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AzoaBindingResponse>().Subject;
        response.UserId.Should().Be("user-1");
        response.AzoaAvatarId.Should().Be("avatar-1");
        response.VerificationLevel.Should().Be("PRO");
    }

    [Fact]
    public async Task GetBinding_FallsBackToEmailLookup_WhenIdentifierIsNotAUserId()
    {
        var user = MakeUser("user-2", email: "member@example.com");
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("member@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        users.Setup(item => item.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await MakeController(users).GetBinding("member@example.com", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBinding_ReturnsNotFound_WhenNoUserMatches()
    {
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await MakeController(users).GetBinding("missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Unbind_RejectsAMissingReason()
    {
        var users = new Mock<IRepository<User>>(MockBehavior.Strict);

        var result = await MakeController(users)
            .Unbind("user-1", new UnbindAzoaAccountRequest(Reason: "  "), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Unbind_RejectsAnOverlongReason()
    {
        var users = new Mock<IRepository<User>>(MockBehavior.Strict);
        var overlong = new string('x', 1001);

        var result = await MakeController(users)
            .Unbind("user-1", new UnbindAzoaAccountRequest(Reason: overlong), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Unbind_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await MakeController(users)
            .Unbind("missing", new UnbindAzoaAccountRequest(Reason: "node rebuild"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Unbind_ReturnsConflict_WhenTheUserHasNoBindingToClear()
    {
        var user = MakeUser("user-1");
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await MakeController(users)
            .Unbind("user-1", new UnbindAzoaAccountRequest(Reason: "node rebuild"), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Unbind_ClearsAllThreeAzoaPointers_AndPersists()
    {
        var user = MakeUser(
            "user-1",
            verificationLevel: VerificationLevel.VERIFIED,
            avatarId: "avatar-1",
            walletId: "wallet-1",
            walletAddress: "0xADDRESS");
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        users.Setup(item => item.GetByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        users.Setup(item => item.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await MakeController(users, unitOfWork, actorId: "operator-1")
            .Unbind("user-1", new UnbindAzoaAccountRequest(Reason: "devnet volume wipe"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AzoaBindingUnboundResponse>().Subject;
        response.OperatorActorId.Should().Be("operator-1");
        response.Reason.Should().Be("devnet volume wipe");
        response.PreviousAzoaAvatarId.Should().Be("avatar-1");
        response.PreviousAzoaWalletId.Should().Be("wallet-1");
        response.PreviousAzoaWalletAddress.Should().Be("0xADDRESS");

        user.azoaAvatarId.Should().BeNull();
        user.azoaWalletId.Should().BeNull();
        user.azoaWalletAddress.Should().BeNull();
        users.Verify(item => item.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, VerificationLevel.ANONYMOUS)]
    [InlineData("2026-01-01", VerificationLevel.VERIFIED)]
    public async Task Unbind_DemotesAProUser_ToTheirEmailVerificationState(
        string? emailVerifiedAt, VerificationLevel expectedLevel)
    {
        var user = MakeUser(
            "user-1",
            verificationLevel: VerificationLevel.PRO,
            avatarId: "avatar-1",
            emailVerified: emailVerifiedAt is null ? null : DateTime.Parse(emailVerifiedAt));
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        users.Setup(item => item.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await MakeController(users, unitOfWork)
            .Unbind("user-1", new UnbindAzoaAccountRequest(Reason: "node rebuild"), CancellationToken.None);

        user.verificationLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public async Task Unbind_LeavesANonProLevelUnchanged()
    {
        var user = MakeUser(
            "user-1",
            verificationLevel: VerificationLevel.ANONYMOUS,
            walletId: "wallet-1");
        var users = new Mock<IRepository<User>>();
        users.Setup(item => item.GetByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        users.Setup(item => item.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await MakeController(users, unitOfWork)
            .Unbind("user-1", new UnbindAzoaAccountRequest(Reason: "node rebuild"), CancellationToken.None);

        user.verificationLevel.Should().Be(VerificationLevel.ANONYMOUS);
    }

    private static User MakeUser(
        string id,
        string email = "member@example.com",
        VerificationLevel verificationLevel = VerificationLevel.ANONYMOUS,
        string? avatarId = null,
        string? walletId = null,
        string? walletAddress = null,
        DateTime? emailVerified = null)
        => new()
        {
            id = id,
            email = email,
            verificationLevel = verificationLevel,
            azoaAvatarId = avatarId,
            azoaWalletId = walletId,
            azoaWalletAddress = walletAddress,
            emailVerified = emailVerified,
        };

    private static AdminAzoaBindingController MakeController(
        Mock<IRepository<User>> users,
        Mock<IUnitOfWork>? unitOfWork = null,
        string actorId = "operator-1")
    {
        var controller = new AdminAzoaBindingController(
            users.Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
            NullLogger<AdminAzoaBindingController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actorId)],
                    "ActorAssertion")),
            },
        };
        return controller;
    }
}

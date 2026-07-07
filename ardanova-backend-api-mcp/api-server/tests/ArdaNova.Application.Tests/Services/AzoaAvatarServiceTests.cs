namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

public class AzoaAvatarServiceTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAzoaAvatarNode> _nodeMock;
    private readonly AzoaAvatarService _sut;

    public AzoaAvatarServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _nodeMock = new Mock<IAzoaAvatarNode>();
        _sut = new AzoaAvatarService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _nodeMock.Object);
    }

    private static User NewUser(string? azoaAvatarId = null, string? walletId = null, string? walletAddress = null)
    {
        return new User
        {
            id = Guid.NewGuid().ToString(),
            email = "alice@example.com",
            name = "Alice",
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            isVerified = false,
            totalXP = 0,
            level = 1,
            tier = UserTier.BRONZE,
            trustScore = 0m,
            verificationLevel = VerificationLevel.ANONYMOUS,
            azoaAvatarId = azoaAvatarId,
            azoaWalletId = walletId,
            azoaWalletAddress = walletAddress,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };
    }

    // ── EnsureAvatarAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureAvatarAsync_WhenUserHasNoAvatar_RegistersAndPersistsId()
    {
        // Arrange
        var user = NewUser(azoaAvatarId: null);
        var avatarId = Guid.NewGuid().ToString();

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _nodeMock.Setup(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAvatarRef>.Success(new AzoaAvatarRef(avatarId, "alice", "alice@example.com")));
        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.EnsureAvatarAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AvatarId.Should().Be(avatarId);
        result.Value!.AvatarLinked.Should().BeTrue();
        user.azoaAvatarId.Should().Be(avatarId);

        _nodeMock.Verify(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureAvatarAsync_WhenAvatarAlreadyLinked_IsIdempotentNoOp()
    {
        // Arrange
        var existingAvatarId = Guid.NewGuid().ToString();
        var user = NewUser(azoaAvatarId: existingAvatarId);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.EnsureAvatarAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AvatarId.Should().Be(existingAvatarId);

        // Idempotent: no second register, no persistence.
        _nodeMock.Verify(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAvatarAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.EnsureAvatarAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        _nodeMock.Verify(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAvatarAsync_WhenNodeFails_PropagatesFailureAndDoesNotPersist()
    {
        // Arrange
        var user = NewUser(azoaAvatarId: null);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _nodeMock.Setup(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAvatarRef>.Failure("AZOA node unavailable"));

        // Act
        var result = await _sut.EnsureAvatarAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        user.azoaAvatarId.Should().BeNull();

        // No persistence on node failure.
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAvatarAsync_WhenNodeReturnsForbidden_PropagatesForbidden()
    {
        // Arrange — fail-closed KYC (KYC_FORBIDDEN) must survive as Forbidden.
        var user = NewUser(azoaAvatarId: null);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _nodeMock.Setup(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAvatarRef>.Forbidden("KYC_FORBIDDEN: approval required"));

        // Act
        var result = await _sut.EnsureAvatarAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── IsTier2ReadyAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsTier2ReadyAsync_WhenNoAvatar_ReturnsFalseWithReason()
    {
        // Arrange
        var user = NewUser(azoaAvatarId: null);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.IsTier2ReadyAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Ready.Should().BeFalse();
        result.Value!.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task IsTier2ReadyAsync_WhenAvatarPresent_ReturnsTrue()
    {
        // Arrange
        var user = NewUser(azoaAvatarId: Guid.NewGuid().ToString());
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.IsTier2ReadyAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Ready.Should().BeTrue();
        result.Value!.Reason.Should().BeNull();
        result.Value!.AvatarId.Should().Be(user.azoaAvatarId);
    }

    [Fact]
    public async Task IsTier2ReadyAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.IsTier2ReadyAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ── GetStatusAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_ReturnsCorrectDto()
    {
        // Arrange
        var avatarId = Guid.NewGuid().ToString();
        var walletId = Guid.NewGuid().ToString();
        var user = NewUser(azoaAvatarId: avatarId, walletId: walletId, walletAddress: "ALGOWALLET123");

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetStatusAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AvatarId.Should().Be(avatarId);
        result.Value!.WalletId.Should().Be(walletId);
        result.Value!.WalletAddress.Should().Be("ALGOWALLET123");
        result.Value!.AvatarLinked.Should().BeTrue();
        result.Value!.WalletBound.Should().BeTrue();
        result.Value!.Tier2Ready.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatusAsync_WhenUnlinked_ReportsNotLinkedNoWalletNotReady()
    {
        // Arrange
        var user = NewUser(azoaAvatarId: null);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetStatusAsync(user.id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AvatarId.Should().BeNull();
        result.Value!.AvatarLinked.Should().BeFalse();
        result.Value!.WalletBound.Should().BeFalse();
        result.Value!.Tier2Ready.Should().BeFalse();
    }

    // ── No-keys-persisted invariant ────────────────────────────────────────────

    [Fact]
    public async Task EnsureAvatarAsync_PersistsOnlyThinReference_NoKeysOrBalances()
    {
        // Arrange — capture what gets written; only azoaAvatarId should change.
        var user = NewUser(azoaAvatarId: null);
        var avatarId = Guid.NewGuid().ToString();
        User? persisted = null;

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _nodeMock.Setup(n => n.RegisterAvatarAsync(It.IsAny<AzoaAvatarRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAvatarRef>.Success(new AzoaAvatarRef(avatarId)));
        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => persisted = u)
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.EnsureAvatarAsync(user.id);

        // Assert — thin reference only; wallet fields untouched (chain is source of truth).
        persisted.Should().NotBeNull();
        persisted!.azoaAvatarId.Should().Be(avatarId);
        persisted!.azoaWalletId.Should().BeNull();
        persisted!.azoaWalletAddress.Should().BeNull();
    }
}

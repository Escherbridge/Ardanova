namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

/// <summary>
/// Acceptance tests for the exactly-once money path (track
/// <c>treasury-reward-to-azoa-allocation</c>; contract §6, §7). Mirrors
/// <c>AzoaAvatarServiceTests</c>: mocks the Application-owned
/// <see cref="IAzoaAllocationNode"/> port and the <c>User</c> repository.
/// </summary>
public class AzoaAllocationServiceTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IAzoaAllocationNode> _nodeMock;
    private readonly AzoaAllocationService _sut;

    public AzoaAllocationServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _nodeMock = new Mock<IAzoaAllocationNode>();
        _sut = new AzoaAllocationService(
            _userRepositoryMock.Object,
            _nodeMock.Object);
    }

    private static User NewUser(string? azoaAvatarId)
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
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };
    }

    private void SetupUser(User user) =>
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private static AzoaAllocationOutcome Outcome(string idempotencyKey, bool replayed = false) =>
        new(
            AvatarId: Guid.NewGuid(),
            WalletId: Guid.NewGuid(),
            WalletAddress: "ALGOWALLET123",
            WalletProvisioned: true,
            OperationId: Guid.NewGuid(),
            Replayed: replayed,
            IdempotencyKey: idempotencyKey);

    private void SetupNodeSuccess(bool replayed = false) =>
        _nodeMock.Setup(n => n.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, AzoaAllocationCommand _, string key, CancellationToken _) =>
                Result<AzoaAllocationOutcome>.Success(Outcome(key, replayed)));

    // ── Stable idempotency keys (§6, §7) ─────────────────────────────────────────

    [Fact]
    public async Task AllocateRewardAsync_BuildsRewardTaskIdKey()
    {
        // Arrange
        var avatarId = Guid.NewGuid().ToString();
        var user = NewUser(avatarId);
        SetupUser(user);
        SetupNodeSuccess();

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert — the key is recipient-bound (security M1): {avatarId}:{eventKey}.
        result.IsSuccess.Should().BeTrue();
        result.Value!.IdempotencyKey.Should().Be($"{avatarId}:reward:task-123");
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            $"{avatarId}:reward:task-123",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AllocateRewardAsync_WithEscrowReleaseId_BuildsMultiPayKey()
    {
        // Arrange
        var avatarId = Guid.NewGuid().ToString();
        var user = NewUser(avatarId);
        SetupUser(user);
        SetupNodeSuccess();

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward", escrowReleaseId: "rel-9");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IdempotencyKey.Should().Be($"{avatarId}:reward:task-123:rel-9");
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            $"{avatarId}:reward:task-123:rel-9",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AllocateFundingSettlementAsync_UsesPaymentIntentIdVerbatimAsKey()
    {
        // Arrange — the provider id IS the event key (no prefix), so at-least-once
        // webhook delivery can never double-allocate; it is then recipient-bound.
        var avatarId = Guid.NewGuid().ToString();
        var user = NewUser(avatarId);
        SetupUser(user);
        SetupNodeSuccess();

        // Act
        var result = await _sut.AllocateFundingSettlementAsync(user.id, "pi_3Stripe123", "250", "Settlement");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IdempotencyKey.Should().Be($"{avatarId}:pi_3Stripe123");
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            $"{avatarId}:pi_3Stripe123",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefundAsync_BuildsRefundEscrowIdKey()
    {
        // Arrange
        var avatarId = Guid.NewGuid().ToString();
        var user = NewUser(avatarId);
        SetupUser(user);
        SetupNodeSuccess();

        // Act
        var result = await _sut.RefundAsync(user.id, "escrow-77", "50", "Refund", Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IdempotencyKey.Should().Be($"{avatarId}:refund:escrow-77");
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            $"{avatarId}:refund:escrow-77",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Exactly-once redelivery surfaces Replayed (§7) ───────────────────────────

    [Fact]
    public async Task AllocateRewardAsync_WhenNodeReplays_SurfacesReplayedTrueAsSuccess()
    {
        // Arrange — a redelivered economic event replays the same op, no second
        // value move. Must be treated as success with Replayed=true (§6, §7).
        var user = NewUser(Guid.NewGuid().ToString());
        SetupUser(user);
        SetupNodeSuccess(replayed: true);

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Replayed.Should().BeTrue();
    }

    [Fact]
    public async Task AllocateRewardAsync_FirstDelivery_SurfacesReplayedFalse()
    {
        // Arrange
        var user = NewUser(Guid.NewGuid().ToString());
        SetupUser(user);
        SetupNodeSuccess(replayed: false);

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Replayed.Should().BeFalse();
    }

    // ── Avatar resolution (§3) ───────────────────────────────────────────────────

    [Fact]
    public async Task AllocateRewardAsync_WhenRecipientHasNoAvatar_ReturnsFailureAndNeverCallsNode()
    {
        // Arrange — recipient must onboard first; the port is never called.
        var user = NewUser(azoaAvatarId: null);
        SetupUser(user);

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AllocateRewardAsync_WhenRecipientNotFound_ReturnsNotFoundAndNeverCallsNode()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.AllocateRewardAsync(userId, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AllocateRewardAsync_ResolvesAvatarGuidAsRouteValue()
    {
        // Arrange — avatarId on the node call must be the recipient's resolved
        // avatar Guid, never a body-supplied value (IDOR-safe; §3).
        var avatarGuid = Guid.NewGuid();
        var user = NewUser(avatarGuid.ToString());
        SetupUser(user);
        SetupNodeSuccess();

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _nodeMock.Verify(n => n.AllocateAsync(
            avatarGuid,
            It.IsAny<AzoaAllocationCommand>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Amount passed through opaquely (§1, §3) ──────────────────────────────────

    [Fact]
    public async Task AllocateRewardAsync_PassesAmountStringThroughOpaquely()
    {
        // Arrange — the exact amount string must reach the port unaltered; the
        // service derives no economics.
        var user = NewUser(Guid.NewGuid().ToString());
        SetupUser(user);
        AzoaAllocationCommand? captured = null;
        _nodeMock.Setup(n => n.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AzoaAllocationCommand, string, CancellationToken>((_, cmd, _, _) => captured = cmd)
            .ReturnsAsync(Result<AzoaAllocationOutcome>.Success(Outcome("reward:task-123")));

        // Act
        await _sut.AllocateRewardAsync(user.id, "task-123", "123.456789000001", "Reward");

        // Assert
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be("123.456789000001");
    }

    [Fact]
    public async Task RefundAsync_IsTransferKind_AndCarriesAssetRecordId()
    {
        // Arrange — a refund moves an existing asset record (Transfer), not a mint.
        var user = NewUser(Guid.NewGuid().ToString());
        SetupUser(user);
        var assetRecordId = Guid.NewGuid();
        AzoaAllocationCommand? captured = null;
        _nodeMock.Setup(n => n.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AzoaAllocationCommand, string, CancellationToken>((_, cmd, _, _) => captured = cmd)
            .ReturnsAsync(Result<AzoaAllocationOutcome>.Success(Outcome("refund:escrow-77")));

        // Act
        await _sut.RefundAsync(user.id, "escrow-77", "50", "Refund", assetRecordId);

        // Assert
        captured.Should().NotBeNull();
        captured!.Kind.Should().Be(AzoaAllocationType.Transfer);
        captured!.AssetRecordId.Should().Be(assetRecordId);
    }

    // ── KYC fail-closed propagation (§6) ─────────────────────────────────────────

    [Fact]
    public async Task AllocateRewardAsync_WhenNodeForbidden_PropagatesForbiddenVerbatim()
    {
        // Arrange — node-side KYC gate (KYC_FORBIDDEN) must survive as Forbidden.
        var user = NewUser(Guid.NewGuid().ToString());
        SetupUser(user);
        _nodeMock.Setup(n => n.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAllocationOutcome>.Forbidden("KYC_FORBIDDEN: approval required"));

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("KYC_FORBIDDEN");
    }

    [Fact]
    public async Task AllocateRewardAsync_WhenNodeFails_PropagatesFailure()
    {
        // Arrange
        var user = NewUser(Guid.NewGuid().ToString());
        SetupUser(user);
        _nodeMock.Setup(n => n.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAllocationOutcome>.Failure("AZOA node unavailable"));

        // Act
        var result = await _sut.AllocateRewardAsync(user.id, "task-123", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    // ── Guard: blank economic-event ids never reach the node ──────────────────────

    [Fact]
    public async Task AllocateRewardAsync_WhenTaskIdBlank_ReturnsValidationErrorAndNeverCallsNode()
    {
        // Act
        var result = await _sut.AllocateRewardAsync("user-1", "  ", "100", "Reward");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AllocateFundingSettlementAsync_WhenPaymentIntentIdBlank_ReturnsValidationError()
    {
        // Act
        var result = await _sut.AllocateFundingSettlementAsync("user-1", "", "100", "Settlement");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        _nodeMock.Verify(n => n.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

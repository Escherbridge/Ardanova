namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using System.Reflection;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

public sealed class AzoaAllocationServiceTests
{
    private readonly Mock<IAzoaValueEligibilityService> _eligibility = new();
    private readonly Mock<IAzoaAllocationNode> _node = new();
    private readonly AzoaAllocationService _sut;

    public AzoaAllocationServiceTests()
    {
        _sut = new AzoaAllocationService(_eligibility.Object, _node.Object);
    }

    [Fact]
    public async Task RefundAsync_UsesTransferAndStableEscrowKey()
    {
        var avatarId = Guid.NewGuid();
        var assetRecordId = Guid.NewGuid();
        AzoaAllocationCommand? captured = null;
        SetupReady("recipient-1", avatarId);
        _node.Setup(node => node.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AzoaAllocationCommand, string, CancellationToken>((_, command, _, _) => captured = command)
            .ReturnsAsync((Guid _, AzoaAllocationCommand _, string key, CancellationToken _) =>
                Result<AzoaAllocationOutcome>.Success(Outcome(key)));

        var result = await _sut.RefundAsync("recipient-1", "escrow-77", "50", "Refund", assetRecordId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdempotencyKey.Should().Be($"{avatarId}:refund:escrow-77");
        captured.Should().NotBeNull();
        captured!.Kind.Should().Be(AzoaAllocationType.Transfer);
        captured.AssetRecordId.Should().Be(assetRecordId);
    }

    /// <summary>
    /// The retirement of the two mint paths is a CONTRACT fact, not a comment: the service
    /// cannot mint because no method that mints exists to call. Asserted by reflection so a
    /// future re-add has to delete this test rather than quietly slip past a doc block.
    /// </summary>
    [Fact]
    public void TheMintingAllocationPathsAreGoneFromTheContract()
    {
        var declared = typeof(IAzoaAllocationService)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();

        declared.Should().NotContain("AllocateRewardAsync");
        declared.Should().NotContain("AllocateFundingSettlementAsync");
        declared.Should().BeEquivalentTo(new[] { nameof(IAzoaAllocationService.RefundAsync) });

        typeof(AzoaAllocationService)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Should().NotContain(new[] { "AllocateRewardAsync", "AllocateFundingSettlementAsync" });
    }

    [Fact]
    public async Task RefundAsync_WhenAccountIsIncomplete_FailsClosedBeforeNodeCall()
    {
        _eligibility.Setup(service => service.RequireReadyAsync(
                "recipient-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden(
                "Complete your ArdaNova account setup before receiving a value allocation."));

        var result = await _sut.RefundAsync("recipient-1", "escrow-77", "50", "Refund", Guid.NewGuid());

        result.Type.Should().Be(ResultType.Forbidden);
        _node.Verify(node => node.AllocateAsync(
            It.IsAny<Guid>(),
            It.IsAny<AzoaAllocationCommand>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefundAsync_DefendsAgainstAnInvalidAvatarReference()
    {
        _eligibility.Setup(service => service.RequireReadyAsync(
                "recipient-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus("not-a-guid")));

        var result = await _sut.RefundAsync("recipient-1", "escrow-77", "50", "Refund", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        _node.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefundAsync_PropagatesProviderForbiddenResult()
    {
        SetupReady("recipient-1", Guid.NewGuid());
        _node.Setup(node => node.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaAllocationOutcome>.Forbidden("KYC_FORBIDDEN: approval required"));

        var result = await _sut.RefundAsync("recipient-1", "escrow-77", "50", "Refund", Guid.NewGuid());

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("KYC_FORBIDDEN");
    }

    [Fact]
    public async Task RefundAsync_WhenEscrowIdIsBlank_DoesNotCheckAccountOrCallNode()
    {
        var result = await _sut.RefundAsync("recipient-1", "  ", "50", "Refund", Guid.NewGuid());

        result.Type.Should().Be(ResultType.ValidationError);
        _eligibility.Verify(service => service.RequireReadyAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _node.VerifyNoOtherCalls();
    }

    private void SetupReady(string userId, Guid avatarId)
    {
        _eligibility.Setup(service => service.RequireReadyAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus(avatarId.ToString())));
    }

    private void SetupNodeSuccess()
    {
        _node.Setup(node => node.AllocateAsync(
                It.IsAny<Guid>(),
                It.IsAny<AzoaAllocationCommand>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, AzoaAllocationCommand _, string key, CancellationToken _) =>
                Result<AzoaAllocationOutcome>.Success(Outcome(key)));
    }

    private static AzoaCustodialAccountStatusDto ReadyStatus(string avatarId)
        => new()
        {
            AvatarId = avatarId,
            WalletId = "wallet-1",
            WalletAddress = "managed-account-address",
            IdentityReady = true,
            KycReady = true,
            KycStatus = AzoaKycStatus.Approved,
            WalletReady = true,
            Ready = true,
        };

    private static AzoaAllocationOutcome Outcome(string idempotencyKey)
        => new(
            AvatarId: Guid.NewGuid(),
            WalletId: Guid.NewGuid(),
            WalletAddress: "managed-account-address",
            WalletProvisioned: true,
            OperationId: Guid.NewGuid(),
            Replayed: false,
            IdempotencyKey: idempotencyKey);
}

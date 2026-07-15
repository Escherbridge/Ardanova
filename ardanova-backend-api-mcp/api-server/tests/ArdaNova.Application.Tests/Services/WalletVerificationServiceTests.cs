namespace ArdaNova.Application.Tests.Services;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class WalletVerificationServiceTests
{
    private readonly Mock<IRepository<Wallet>> _wallets = new();
    private readonly Mock<IWalletVerificationChallengeStore> _challenges = new();
    private readonly Mock<IWalletProofVerifier> _proofs = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly WalletVerificationService _sut;

    public WalletVerificationServiceTests()
    {
        _proofs.SetupGet(item => item.Chain).Returns("algorand");
        _proofs.SetupGet(item => item.Network).Returns("testnet");
        _sut = new WalletVerificationService(_wallets.Object, _challenges.Object, _proofs.Object, _mapper.Object);
    }

    [Fact]
    public async Task IssueAsync_CreatesNonceHashedCanonicalChallenge()
    {
        var wallet = WalletFor("actor-1");
        WalletVerificationChallenge? stored = null;
        _wallets.Setup(item => item.GetByIdAsync(wallet.id, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _proofs.Setup(item => item.TryNormalizeAddress(wallet.address, out It.Ref<string>.IsAny))
            .Callback(new NormalizeAddressCallback((string _, out string normalized) => normalized = wallet.address))
            .Returns(true);
        _challenges.Setup(item => item.CreateAsync(It.IsAny<WalletVerificationChallenge>(), It.IsAny<CancellationToken>()))
            .Callback<WalletVerificationChallenge, CancellationToken>((challenge, _) => stored = challenge)
            .Returns(Task.CompletedTask);

        var result = await _sut.IssueAsync("actor-1", wallet.id);

        result.IsSuccess.Should().BeTrue();
        stored.Should().NotBeNull();
        result.Value!.Message.Should().Contain("actor:actor-1").And.Contain($"wallet-id:{wallet.id}");
        result.Value.Message.Should().Contain($"expires-at:{stored!.expiresAt:O}");
        stored.nonceHash.Should().NotBeEmpty().And.NotBe(result.Value.Message);
        _challenges.Verify(item => item.CreateAsync(It.IsAny<WalletVerificationChallenge>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_ValidProof_AtomicallyConsumesAndReturnsVerifiedWallet()
    {
        var wallet = WalletFor("actor-1");
        var challenge = ChallengeFor(wallet, nonce: "nonce");
        ArrangeOwned(wallet, challenge);
        _proofs.Setup(item => item.Verify(wallet.address, It.IsAny<string>(), "signature")).Returns(true);
        _challenges.Setup(item => item.TryConsumeAsync(
                It.Is<WalletVerificationConsumeRequest>(request => request.ProofVerified && request.WalletId == wallet.id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WalletVerificationConsumeResult.Consumed);
        _mapper.Setup(item => item.Map<WalletDto>(wallet)).Returns(new WalletDto { Id = wallet.id, UserId = wallet.userId, Address = wallet.address });

        var result = await _sut.CompleteAsync("actor-1", wallet.id, Complete(challenge));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_InvalidSignature_ConsumesChallengeWithoutVerifyingWallet()
    {
        var wallet = WalletFor("actor-1");
        var challenge = ChallengeFor(wallet, nonce: "nonce");
        ArrangeOwned(wallet, challenge);
        _proofs.Setup(item => item.Verify(wallet.address, It.IsAny<string>(), "bad-signature")).Returns(false);
        _challenges.Setup(item => item.TryConsumeAsync(
                It.Is<WalletVerificationConsumeRequest>(request => !request.ProofVerified && request.FailureCode == "invalid_signature"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WalletVerificationConsumeResult.Consumed);

        var result = await _sut.CompleteAsync("actor-1", wallet.id, Complete(challenge, "bad-signature"));

        result.Type.Should().Be(ResultType.ValidationError);
        _mapper.Verify(item => item.Map<WalletDto>(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_ExpiredChallenge_DoesNotVerifyOrConsume()
    {
        var wallet = WalletFor("actor-1");
        var challenge = ChallengeFor(wallet, nonce: "nonce", expiresAt: DateTime.UtcNow.AddMinutes(-1));
        ArrangeOwned(wallet, challenge);

        var result = await _sut.CompleteAsync("actor-1", wallet.id, Complete(challenge));

        result.Type.Should().Be(ResultType.ValidationError);
        _challenges.Verify(item => item.TryConsumeAsync(It.IsAny<WalletVerificationConsumeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_ReplayOrConcurrentWinner_ReturnsConflict()
    {
        var wallet = WalletFor("actor-1");
        var challenge = ChallengeFor(wallet, nonce: "nonce");
        ArrangeOwned(wallet, challenge);
        _proofs.Setup(item => item.Verify(wallet.address, It.IsAny<string>(), "signature")).Returns(true);
        _challenges.Setup(item => item.TryConsumeAsync(It.IsAny<WalletVerificationConsumeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WalletVerificationConsumeResult.Replay);

        var result = await _sut.CompleteAsync("actor-1", wallet.id, Complete(challenge));

        result.Type.Should().Be(ResultType.Conflict);
        _mapper.Verify(item => item.Map<WalletDto>(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_ForeignActorOrWallet_IsForbiddenBeforeProofVerification()
    {
        var wallet = WalletFor("other-actor");
        _wallets.Setup(item => item.GetByIdAsync(wallet.id, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

        var result = await _sut.CompleteAsync("actor-1", wallet.id, new CompleteWalletVerificationDto
        {
            ChallengeId = "challenge",
            Nonce = "nonce",
            Signature = "signature"
        });

        result.Type.Should().Be(ResultType.Forbidden);
        _challenges.Verify(item => item.GetForActorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _proofs.Verify(item => item.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_ChallengeForDifferentWallet_IsForbiddenBeforeProofVerification()
    {
        var wallet = WalletFor("actor-1");
        var challenge = ChallengeFor(wallet, nonce: "nonce");
        challenge.walletId = "other-wallet";
        _wallets.Setup(item => item.GetByIdAsync(wallet.id, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _challenges.Setup(item => item.GetForActorAsync(challenge.id, wallet.userId, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);

        var result = await _sut.CompleteAsync("actor-1", wallet.id, Complete(challenge));

        result.Type.Should().Be(ResultType.Forbidden);
        _proofs.Verify(item => item.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _challenges.Verify(item => item.TryConsumeAsync(It.IsAny<WalletVerificationConsumeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void ArrangeOwned(Wallet wallet, WalletVerificationChallenge challenge)
    {
        _wallets.Setup(item => item.GetByIdAsync(wallet.id, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        _challenges.Setup(item => item.GetForActorAsync(challenge.id, wallet.userId, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
    }

    private static Wallet WalletFor(string actorId) => new()
    {
        id = "wallet-1",
        userId = actorId,
        address = "CANONICAL-ALGORAND-ADDRESS",
        provider = WalletProvider.PERA,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    private static WalletVerificationChallenge ChallengeFor(Wallet wallet, string nonce, DateTime? expiresAt = null) => new()
    {
        id = "challenge-1",
        userId = wallet.userId,
        walletId = wallet.id,
        address = wallet.address,
        chain = "algorand",
        network = "testnet",
        nonceHash = Hash(nonce),
        issuedAt = DateTime.UtcNow.AddMinutes(-1),
        expiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(4)
    };

    private static CompleteWalletVerificationDto Complete(WalletVerificationChallenge challenge, string signature = "signature") => new()
    {
        ChallengeId = challenge.id,
        Nonce = "nonce",
        Signature = signature
    };

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private delegate void NormalizeAddressCallback(string address, out string normalized);
}

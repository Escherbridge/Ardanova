namespace ArdaNova.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

public sealed class WalletVerificationService : IWalletVerificationService
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private readonly IRepository<Wallet> _wallets;
    private readonly IWalletVerificationChallengeStore _challenges;
    private readonly IWalletProofVerifier _proofVerifier;
    private readonly IMapper _mapper;

    public WalletVerificationService(
        IRepository<Wallet> wallets,
        IWalletVerificationChallengeStore challenges,
        IWalletProofVerifier proofVerifier,
        IMapper mapper)
    {
        _wallets = wallets;
        _challenges = challenges;
        _proofVerifier = proofVerifier;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<Result<WalletVerificationChallengeDto>> IssueAsync(
        string actorId,
        string walletId,
        CancellationToken ct = default)
    {
        var wallet = await GetActorWalletAsync(actorId, walletId, ct);
        if (wallet is null)
            return Result<WalletVerificationChallengeDto>.Forbidden("Wallet does not belong to the authenticated actor.");
        if (wallet.isVerified)
            return Result<WalletVerificationChallengeDto>.Conflict("Wallet is already verified.");
        if (!_proofVerifier.TryNormalizeAddress(wallet.address, out var address)
            || !string.Equals(wallet.address, address, StringComparison.Ordinal))
        {
            return Result<WalletVerificationChallengeDto>.ValidationError("Wallet address is not a canonical Algorand address.");
        }

        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.Add(ChallengeLifetime);
        var challengeId = Guid.NewGuid().ToString("N");
        var nonce = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = new WalletVerificationChallenge
        {
            id = challengeId,
            userId = actorId,
            walletId = wallet.id,
            address = address,
            chain = _proofVerifier.Chain,
            network = _proofVerifier.Network,
            nonceHash = Hash(nonce),
            issuedAt = issuedAt,
            expiresAt = expiresAt
        };

        await _challenges.CreateAsync(challenge, ct);
        return Result<WalletVerificationChallengeDto>.Success(new WalletVerificationChallengeDto
        {
            ChallengeId = challenge.id,
            Message = BuildMessage(challenge, nonce),
            Chain = challenge.chain,
            Network = challenge.network,
            ExpiresAt = challenge.expiresAt
        });
    }

    /// <inheritdoc/>
    public async Task<Result<WalletDto>> CompleteAsync(
        string actorId,
        string walletId,
        CompleteWalletVerificationDto request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId)
            || string.IsNullOrWhiteSpace(request.Nonce)
            || string.IsNullOrWhiteSpace(request.Signature))
        {
            return Result<WalletDto>.ValidationError("Challenge id, nonce, and signature are required.");
        }

        var wallet = await GetActorWalletAsync(actorId, walletId, ct);
        if (wallet is null)
            return Result<WalletDto>.Forbidden("Wallet does not belong to the authenticated actor.");

        var challenge = await _challenges.GetForActorAsync(request.ChallengeId, actorId, ct);
        if (challenge is null || !string.Equals(challenge.walletId, wallet.id, StringComparison.Ordinal))
            return Result<WalletDto>.Forbidden("Verification challenge does not belong to this wallet.");

        if (challenge.consumedAt is not null)
            return Result<WalletDto>.Conflict("Verification challenge has already been consumed.");
        if (challenge.expiresAt <= DateTime.UtcNow)
            return Result<WalletDto>.ValidationError("Verification challenge has expired.");

        var nonceMatches = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(challenge.nonceHash),
            Convert.FromHexString(Hash(request.Nonce)));
        var proofVerified = nonceMatches
            && _proofVerifier.Verify(challenge.address, BuildMessage(challenge, request.Nonce), request.Signature);
        var consume = await _challenges.TryConsumeAsync(
            new WalletVerificationConsumeRequest(
                challenge.id,
                actorId,
                wallet.id,
                challenge.chain,
                challenge.network,
                proofVerified,
                Hash(request.Signature),
                proofVerified ? null : nonceMatches ? "invalid_signature" : "nonce_mismatch",
                DateTime.UtcNow),
            ct);

        return consume switch
        {
            WalletVerificationConsumeResult.Consumed when proofVerified => Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet) with
            {
                IsVerified = true,
                UpdatedAt = DateTime.UtcNow
            }),
            WalletVerificationConsumeResult.Consumed => Result<WalletDto>.ValidationError("Wallet signature proof is invalid."),
            WalletVerificationConsumeResult.Replay => Result<WalletDto>.Conflict("Verification challenge has already been consumed."),
            WalletVerificationConsumeResult.Expired => Result<WalletDto>.ValidationError("Verification challenge has expired."),
            WalletVerificationConsumeResult.WalletUnavailable => Result<WalletDto>.Conflict("Wallet changed while the verification proof was being completed."),
            _ => Result<WalletDto>.Forbidden("Verification challenge does not belong to this wallet.")
        };
    }

    private async Task<Wallet?> GetActorWalletAsync(string actorId, string walletId, CancellationToken ct)
    {
        var wallet = await _wallets.GetByIdAsync(walletId, ct);
        return wallet is not null && string.Equals(wallet.userId, actorId, StringComparison.Ordinal) ? wallet : null;
    }

    private static string BuildMessage(WalletVerificationChallenge challenge, string nonce)
        => $"ArdaNova Wallet Verification\nversion:1\nchallenge-id:{challenge.id}\nactor:{challenge.userId}\nwallet-id:{challenge.walletId}\naddress:{challenge.address}\nchain:{challenge.chain}\nnetwork:{challenge.network}\nnonce:{nonce}\nexpires-at:{challenge.expiresAt:O}";

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64Url(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

namespace ArdaNova.Infrastructure.Wallets;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public sealed class WalletVerificationChallengeStore : IWalletVerificationChallengeStore
{
    private readonly ArdaNovaDbContext _context;

    public WalletVerificationChallengeStore(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(WalletVerificationChallenge challenge, CancellationToken ct = default)
    {
        await _context.WalletVerificationChallenges.AddAsync(challenge, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<WalletVerificationChallenge?> GetForActorAsync(string challengeId, string actorId, CancellationToken ct = default)
        => _context.WalletVerificationChallenges.AsNoTracking().SingleOrDefaultAsync(
            item => item.id == challengeId && item.userId == actorId,
            ct);

    /// <inheritdoc/>
    public async Task<WalletVerificationConsumeResult> TryConsumeAsync(
        WalletVerificationConsumeRequest request,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var consumed = await _context.WalletVerificationChallenges
            .Where(item => item.id == request.ChallengeId
                && item.userId == request.ActorId
                && item.walletId == request.WalletId
                && item.chain == request.Chain
                && item.network == request.Network
                && item.consumedAt == null
                && item.expiresAt > request.ConsumedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.consumedAt, request.ConsumedAt)
                .SetProperty(item => item.proofVerified, request.ProofVerified)
                .SetProperty(item => item.signatureHash, request.SignatureHash)
                .SetProperty(item => item.failureCode, request.FailureCode), ct);

        if (consumed == 1)
        {
            if (request.ProofVerified)
            {
                var verified = await _context.Wallets
                    .Where(wallet => wallet.id == request.WalletId && wallet.userId == request.ActorId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(wallet => wallet.isVerified, true)
                        .SetProperty(wallet => wallet.verifiedAt, request.ConsumedAt)
                        .SetProperty(wallet => wallet.verificationChain, request.Chain)
                        .SetProperty(wallet => wallet.verificationNetwork, request.Network)
                        .SetProperty(wallet => wallet.verificationChallengeId, request.ChallengeId)
                        .SetProperty(wallet => wallet.updatedAt, request.ConsumedAt), ct);
                if (verified != 1)
                {
                    await transaction.RollbackAsync(ct);
                    return WalletVerificationConsumeResult.WalletUnavailable;
                }
            }

            await transaction.CommitAsync(ct);
            return WalletVerificationConsumeResult.Consumed;
        }

        await transaction.RollbackAsync(ct);
        var existing = await _context.WalletVerificationChallenges.AsNoTracking().SingleOrDefaultAsync(
            item => item.id == request.ChallengeId,
            ct);
        if (existing is null || !string.Equals(existing.userId, request.ActorId, StringComparison.Ordinal)
            || !string.Equals(existing.walletId, request.WalletId, StringComparison.Ordinal))
        {
            return WalletVerificationConsumeResult.NotFoundOrForeign;
        }

        return existing.consumedAt is not null
            ? WalletVerificationConsumeResult.Replay
            : WalletVerificationConsumeResult.Expired;
    }
}

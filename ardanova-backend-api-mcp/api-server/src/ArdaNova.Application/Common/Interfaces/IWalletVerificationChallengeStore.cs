namespace ArdaNova.Application.Common.Interfaces;

using ArdaNova.Domain.Models.Entities;

public interface IWalletVerificationChallengeStore
{
    Task CreateAsync(WalletVerificationChallenge challenge, CancellationToken ct = default);
    Task<WalletVerificationChallenge?> GetForActorAsync(string challengeId, string actorId, CancellationToken ct = default);
    Task<WalletVerificationConsumeResult> TryConsumeAsync(
        WalletVerificationConsumeRequest request,
        CancellationToken ct = default);
}

public sealed record WalletVerificationConsumeRequest(
    string ChallengeId,
    string ActorId,
    string WalletId,
    string Chain,
    string Network,
    bool ProofVerified,
    string? SignatureHash,
    string? FailureCode,
    DateTime ConsumedAt);

public enum WalletVerificationConsumeResult
{
    Consumed,
    Replay,
    Expired,
    NotFoundOrForeign,
    WalletUnavailable
}

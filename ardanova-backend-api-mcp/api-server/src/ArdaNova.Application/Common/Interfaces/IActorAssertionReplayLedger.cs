namespace ArdaNova.Application.Common.Interfaces;

/// <summary>Atomically records each accepted BFF actor assertion identifier.</summary>
public interface IActorAssertionReplayLedger
{
    /// <summary>Consumes the assertion id once and retains it beyond the accepted expiry/skew window.</summary>
    Task<ActorAssertionReplayClaim> TryConsumeAsync(
        ActorAssertionReplayEntry entry,
        CancellationToken ct = default);
}

/// <summary>Immutable evidence retained for a consumed actor assertion.</summary>
public sealed record ActorAssertionReplayEntry(
    string Jti,
    DateTime ExpiresAt,
    DateTime ConsumedAt,
    string Subject,
    string RequestTarget,
    string BodySha256);

/// <summary>Outcome of the single-use assertion claim.</summary>
public enum ActorAssertionReplayClaim
{
    Consumed,
    Replay
}

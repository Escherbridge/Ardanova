namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Common.Results;

/// <summary>
/// Low-level transport to the shared/managed AZOA node. Carries the tenant
/// <c>X-Api-Key</c>, translates the node's <c>AZOAResult&lt;T&gt;</c> envelope into
/// ArdaNova's <c>Result&lt;T&gt;</c>, and maps the fail-closed KYC error
/// (<c>KYC_FORBIDDEN:</c> prefix) to <see cref="ResultType.Forbidden"/> (§6, §11.4).
///
/// This is infrastructure plumbing; domain services (avatar/quest/allocation)
/// sit on top of it. It never computes economics — amounts pass through as
/// opaque strings (§1, §3).
/// </summary>
public interface IAzoaNodeClient
{
    /// <summary>Self-register an end user as a self-sovereign avatar (anonymous; §4, §11.2).</summary>
    Task<Result<AzoaAvatar>> RegisterAvatarAsync(
        AzoaAvatarRegisterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Push an already-decided allocation to the target avatar (route-only, IDOR-safe).
    /// Idempotent on <paramref name="idempotencyKey"/> (stable per economic event).
    /// </summary>
    Task<Result<AzoaAllocationResult>> AllocateAsync(
        Guid avatarId,
        AzoaAllocationRequest request,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Generic authenticated GET against the node, envelope-unwrapped.
    /// Used by quest-state and portfolio read paths.
    /// </summary>
    Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default);

    /// <summary>Generic authenticated POST against the node, envelope-unwrapped.</summary>
    Task<Result<T>> PostAsync<T>(
        string path, object? body, CancellationToken ct = default);
}

namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Common.Results;

/// <summary>
/// Low-level transport to the shared/managed AZOA node. Carries the scoped value
/// <c>X-Api-Key</c>, translates the node's <c>AZOAResult&lt;T&gt;</c> envelope into
/// ArdaNova's <c>Result&lt;T&gt;</c>, and maps the fail-closed KYC error
/// (<c>KYC_FORBIDDEN:</c> prefix) to <see cref="ResultType.Forbidden"/> (§6, §11.4).
///
/// This transport is restricted to value operations. Anonymous registration,
/// tenant custody, and quest automation use separate clients. Amounts pass
/// through as opaque strings (§1, §3).
/// </summary>
public interface IAzoaNodeClient
{
    /// <summary>
    /// Push an already-decided allocation to the target avatar (route-only, IDOR-safe).
    /// Idempotent on <paramref name="idempotencyKey"/> (stable per economic event).
    /// </summary>
    Task<Result<AzoaAllocationResult>> AllocateAsync(
        Guid avatarId,
        AzoaAllocationRequest request,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>Post a fungible-mint request; all other paths are rejected.</summary>
    Task<Result<T>> PostAsync<T>(
        string path, object? body, CancellationToken ct = default);
}

/// <summary>Credential-free transport for AZOA's anonymous avatar registration route.</summary>
public interface IAzoaPublicNodeClient
{
    Task<Result<AzoaAvatar>> RegisterAvatarAsync(
        AzoaAvatarRegisterRequest request,
        CancellationToken ct = default);
}

/// <summary>Transport restricted to tenant custodial-account endpoints.</summary>
public interface IAzoaCustodialNodeClient
{
    Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default);

    Task<Result<T>> PostAsync<T>(
        string path,
        object? body,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<Result<T>> PutAsync<T>(
        string path,
        object? body,
        string idempotencyKey,
        CancellationToken ct = default);
}

/// <summary>Transport restricted to authenticated quest endpoints.</summary>
public interface IAzoaQuestNodeClient
{
    Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default);

    Task<Result<T>> PostAsync<T>(
        string path,
        object? body,
        CancellationToken ct = default);
}

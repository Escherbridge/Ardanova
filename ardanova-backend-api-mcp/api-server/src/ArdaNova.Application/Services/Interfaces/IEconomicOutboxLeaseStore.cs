namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Domain.Models.Enums;

/// <summary>Provides compare-and-set outbox leasing and finalization for economic dispatch.</summary>
public interface IEconomicOutboxLeaseStore
{
    /// <summary>Claims one due outbox row or returns no work when another worker owns it.</summary>
    Task<EconomicOutboxLease?> TryClaimAsync(
        EconomicOutboxClaimKind kind,
        DateTime now,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>Extends a current lease or returns null when its compare-and-set conditions no longer hold.</summary>
    Task<EconomicOutboxLease?> TryRenewAsync(
        EconomicOutboxLease lease,
        DateTime now,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    /// <summary>Atomically records an outcome only while the supplied lease remains current.</summary>
    Task<bool> FinalizeAsync(
        EconomicOutboxLease lease,
        EconomicOutboxFinalization finalization,
        DateTime now,
        CancellationToken ct = default);

    /// <summary>
    /// Releases an AUTHORIZED settlement for dispatch (AUTHORIZED -> PENDING_DISPATCH). False when a
    /// void cancelled it or another request released it first. See Implementations/AGENTS.md §Sponsorship void.
    /// </summary>
    Task<bool> TryReleaseForDispatchAsync(string settlementId, DateTime now, CancellationToken ct = default);

    /// <summary>The settlement's status as the database holds it now, bypassing any tracked copy; null when absent.</summary>
    Task<EconomicSettlementStatus?> ReadSettlementStatusAsync(string settlementId, CancellationToken ct = default);

    /// <summary>Cancels a settlement nothing ever dispatched (AUTHORIZED) and its unleased PENDING outbox, atomically.</summary>
    Task<bool> TryCancelUndispatchedAsync(
        string settlementId,
        string failureCode,
        string? failureDetail,
        DateTime now,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a PENDING_DISPATCH settlement at <paramref name="expectedSettlementVersion"/> only while
    /// its outbox is PENDING and unleased - never under a worker holding the lease.
    /// </summary>
    Task<bool> TryCancelUnleasedAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        DateTime now,
        CancellationToken ct = default);
}

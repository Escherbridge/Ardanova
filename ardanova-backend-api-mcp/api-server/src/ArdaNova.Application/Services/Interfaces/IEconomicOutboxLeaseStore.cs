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
    /// Releases an AUTHORIZED settlement for dispatch (AUTHORIZED -> PENDING_DISPATCH), stamping
    /// <c>submittedAt</c> as the instant the request-path send may begin. False when a void cancelled it
    /// or another request released it first. See Implementations/AGENTS.md §Sponsorship void, §Node proof.
    /// </summary>
    Task<bool> TryReleaseForDispatchAsync(string settlementId, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Marks a settlement CONFIRMED by compare-and-set, only from an open status (PENDING_DISPATCH,
    /// SUBMITTED, AWAITING_RECONCILIATION, CONFIRMED). False when it is closed or absent, so a
    /// confirmation can never overwrite a CANCELLED that may already have been answered <c>voided</c>.
    /// Locks the settlement's outbox row FIRST (the global outbox-then-settlement order); call it inside
    /// the caller's transaction so both locks are held to its commit.
    /// </summary>
    Task<bool> TryConfirmOpenAsync(string settlementId, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// The request path's lost answer: PENDING_DISPATCH at <paramref name="expectedSettlementVersion"/> with
    /// an unleased PENDING outbox -> both AWAITING_RECONCILIATION, atomically. False - nothing written - when
    /// a worker holds the lease or moved the row (it may have SUBMITTED it).
    /// </summary>
    Task<bool> TryParkUnleasedForReconciliationAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        DateTime availableAt,
        DateTime now,
        CancellationToken ct = default);

    /// <summary>
    /// The request path's retryable refusal: records it and defers the unleased PENDING outbox to
    /// <paramref name="availableAt"/>, by the same compare-and-set. False - nothing written - under a worker.
    /// </summary>
    Task<bool> TryLeaveUnleasedToWorkerAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        DateTime availableAt,
        DateTime now,
        CancellationToken ct = default);

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
    /// its outbox is PENDING and unleased - never under a worker holding the lease. The caller must
    /// already hold the proof that nothing was taken; <paramref name="nothingWasSubmitted"/> (proven
    /// in the releasing call itself) also clears <c>submittedAt</c>.
    /// </summary>
    Task<bool> TryCancelUnleasedAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        bool nothingWasSubmitted,
        DateTime now,
        CancellationToken ct = default);
}

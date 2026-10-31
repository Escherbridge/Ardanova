namespace ArdaNova.Infrastructure.Outbox;

using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <inheritdoc/>
public sealed class EconomicOutboxLeaseStore : IEconomicOutboxLeaseStore
{
    private readonly ArdaNovaDbContext _context;

    public EconomicOutboxLeaseStore(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<EconomicOutboxLease?> TryClaimAsync(
        EconomicOutboxClaimKind kind,
        DateTime now,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        var candidate = await EligibleOutboxes(kind, now)
            .AsNoTracking()
            .Include(item => item.Settlement)
            .OrderBy(item => item.availableAt)
            .ThenBy(item => item.createdAt)
            .FirstOrDefaultAsync(ct);
        if (candidate is null)
            return null;

        var candidateSettlement = candidate.Settlement
            ?? throw new InvalidOperationException("An eligible economic outbox is missing its settlement.");
        if (!FixedScaleAmount.TryFromPositiveDecimal(candidateSettlement.amount, candidateSettlement.scale, out _))
            throw new InvalidOperationException("An economic settlement with an invalid fixed-scale amount cannot be claimed.");

        var leaseToken = Guid.NewGuid().ToString("N");
        var expiresAt = now.Add(leaseDuration);
        // Taking over a DISPATCH lease that expired mid-attempt: that attempt may have sent and its answer
        // was never recorded, so its stamp becomes the settlement's unanswered-send stamp (§Node proof).
        var candidateStatus = candidate.status;
        var candidateLastAttemptAt = candidate.lastAttemptAt;
        var unansweredSendAt = kind == EconomicOutboxClaimKind.Dispatch && candidateStatus == EconomicOutboxStatus.LEASED
            ? candidateLastAttemptAt
            : null;

        var claimedInTransaction = await InOneTransactionAsync("economic_outbox_claim", async () =>
        {
            var claimed = await EligibleOutboxes(kind, now)
                .Where(item => item.id == candidate.id
                    && item.status == candidateStatus
                    && item.lastAttemptAt == candidateLastAttemptAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.status, EconomicOutboxStatus.LEASED)
                    .SetProperty(item => item.leaseToken, leaseToken)
                    .SetProperty(item => item.leaseExpiresAt, expiresAt)
                    .SetProperty(item => item.lastAttemptAt, now)
                    .SetProperty(item => item.attemptCount, item => item.attemptCount + 1)
                    .SetProperty(item => item.updatedAt, now), ct);
            if (claimed != 1)
                return false;

            if (unansweredSendAt is { } sentAt)
            {
                // No version bump: this records a fact about the past, it does not move the row.
                await _context.EconomicSettlements
                    .Where(item => item.id == candidateSettlement.id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            item => item.submittedAt,
                            item => item.submittedAt == null || item.submittedAt < sentAt ? sentAt : item.submittedAt), ct);
            }

            return true;
        }, ct);
        if (!claimedInTransaction)
            return null;

        var outbox = await _context.EconomicOutboxes
            .AsNoTracking()
            .Include(item => item.Settlement)
            .ThenInclude(settlement => settlement!.Project)
            .SingleAsync(item => item.id == candidate.id, ct);
        var settlement = outbox.Settlement
            ?? throw new InvalidOperationException("A claimed economic outbox is missing its settlement.");
        if (!FixedScaleAmount.TryFromPositiveDecimal(settlement.amount, settlement.scale, out var amount))
            throw new InvalidOperationException("A claimed economic settlement has an invalid fixed-scale amount.");

        return new EconomicOutboxLease(
            outbox.id,
            settlement.id,
            leaseToken,
            outbox.attemptCount,
            expiresAt,
            settlement.status,
            settlement.version,
            kind,
            new AzoaSettlementRequest(
                settlement.id,
                settlement.idempotencyKey,
                settlement.beneficiaryUserId,
                settlement.assetCode,
                amount,
                settlement.termsSnapshot),
            settlement.kind,
            // One rule, read here and again in the dispatch service: a kind that credits a project
            // needs its owner resolved, or the owner-readiness gate has nothing to check.
            EconomicSettlementKindRules.RequiresProjectOwnerReadiness(settlement.kind)
                ? settlement.Project?.createdById
                : null,
            EconomicOutboxReopenLedger.Read(outbox.failureDetail),
            outbox.reconciliationRequiredAt,
            settlement.submittedAt,
            outbox.lastAttemptAt);
    }

    /// <inheritdoc/>
    public async Task<EconomicOutboxLease?> TryRenewAsync(
        EconomicOutboxLease lease,
        DateTime now,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        if (lease.LeaseExpiresAt <= now)
            return null;

        var expiresAt = now.Add(leaseDuration);
        var renewed = await _context.EconomicOutboxes
            .Where(item => item.id == lease.OutboxId
                && item.status == EconomicOutboxStatus.LEASED
                && item.leaseToken == lease.LeaseToken
                && item.leaseExpiresAt > now
                && item.Settlement != null
                && item.Settlement.id == lease.SettlementId
                && item.Settlement.status == lease.SettlementStatus
                && item.Settlement.version == lease.SettlementVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.leaseExpiresAt, expiresAt)
                .SetProperty(item => item.updatedAt, now), ct);
        return renewed == 1
            ? lease with { LeaseExpiresAt = expiresAt }
            : null;
    }

    /// <inheritdoc/>
    public async Task<bool> FinalizeAsync(
        EconomicOutboxLease lease,
        EconomicOutboxFinalization finalization,
        DateTime now,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        // Two failure codes are decisions, not descriptions. The dispatch ceiling is only ever
        // reached after refusals the gateway called safely retryable, which is its word that no
        // value moved - so it is the one refusal this seam may make terminal. A receipt the node
        // cannot find means the exchange never landed, so the settlement goes back to the dispatch
        // queue rather than looping on a reconciliation that has nothing to read.
        var isDispatchCeiling = finalization.Outcome == EconomicOutboxRecordedOutcome.Retry
            && lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
            && finalization.FailureCode == EconomicSettlementFailureCodes.DispatchAttemptCeiling;
        var isRedispatchable = finalization.Outcome == EconomicOutboxRecordedOutcome.Retry
            && lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
            && finalization.FailureCode == EconomicSettlementFailureCodes.ExchangeNeverLanded;
        // A receipt read that proved (or is waiting to prove) no effect is bookkeeping, not a
        // dispatch, so it hands back the attempt its own claim took (see AGENTS.md, "No-effect reopen").
        var refundsAttempt = finalization.Outcome == EconomicOutboxRecordedOutcome.Retry
            && lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
            && EconomicSettlementFailureCodes.IsNoEffectBookkeeping(finalization.FailureCode);
        var attemptRefund = refundsAttempt ? 1 : 0;
        var outboxFailureDetail = EconomicOutboxReopenLedger.Stamp(
            finalization.FailureDetail,
            lease.NoEffectReopenCount + (isRedispatchable ? 1 : 0));

        var outboxStatus = finalization.Outcome switch
        {
            EconomicOutboxRecordedOutcome.Accepted => EconomicOutboxStatus.SUBMITTED,
            EconomicOutboxRecordedOutcome.Unknown => EconomicOutboxStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry when isDispatchCeiling => EconomicOutboxStatus.FAILED,
            EconomicOutboxRecordedOutcome.Retry when isRedispatchable => EconomicOutboxStatus.PENDING,
            EconomicOutboxRecordedOutcome.Retry when lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
                => EconomicOutboxStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry => EconomicOutboxStatus.PENDING,
            _ => throw new InvalidOperationException("Only gateway outcomes may finalize an economic outbox lease."),
        };
        DateTime? reconciliationAt = finalization.Outcome switch
        {
            EconomicOutboxRecordedOutcome.Accepted => null,
            EconomicOutboxRecordedOutcome.Unknown => now,
            _ when isDispatchCeiling || isRedispatchable => null,
            _ => lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation ? now : null,
        };
        // While a row stays in reconciliation the FIRST ambiguous instant is kept: it is what the
        // no-effect quiet window is measured from, and a poll that rewrote it would defer forever.
        var keepsFirstReconciliationTime = lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
            && reconciliationAt is not null;
        var outboxUpdated = await _context.EconomicOutboxes
            .Where(item => item.id == lease.OutboxId
                && item.status == EconomicOutboxStatus.LEASED
                && item.leaseToken == lease.LeaseToken
                && item.leaseExpiresAt > now
                && item.Settlement != null
                && item.Settlement.id == lease.SettlementId
                && item.Settlement.status == lease.SettlementStatus
                && item.Settlement.version == lease.SettlementVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, outboxStatus)
                .SetProperty(item => item.availableAt, finalization.AvailableAt)
                .SetProperty(item => item.leaseToken, (string?)null)
                .SetProperty(item => item.leaseExpiresAt, (DateTime?)null)
                .SetProperty(item => item.dispatchedAt, item => finalization.Outcome == EconomicOutboxRecordedOutcome.Accepted ? now : item.dispatchedAt)
                .SetProperty(
                    item => item.reconciliationRequiredAt,
                    item => keepsFirstReconciliationTime
                        ? item.reconciliationRequiredAt ?? reconciliationAt
                        : reconciliationAt)
                .SetProperty(item => item.attemptCount, item => item.attemptCount - attemptRefund)
                .SetProperty(item => item.failureCode, finalization.FailureCode)
                .SetProperty(item => item.failureDetail, outboxFailureDetail)
                .SetProperty(item => item.updatedAt, now), ct);
        if (outboxUpdated != 1)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return false;
        }

        var settlementStatus = finalization.Outcome switch
        {
            EconomicOutboxRecordedOutcome.Accepted => EconomicSettlementStatus.SUBMITTED,
            EconomicOutboxRecordedOutcome.Unknown => EconomicSettlementStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry when isDispatchCeiling => EconomicSettlementStatus.FAILED,
            EconomicOutboxRecordedOutcome.Retry when isRedispatchable => EconomicSettlementStatus.PENDING_DISPATCH,
            EconomicOutboxRecordedOutcome.Retry when lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
                => EconomicSettlementStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry => EconomicSettlementStatus.PENDING_DISPATCH,
            _ => throw new InvalidOperationException("Only gateway outcomes may finalize an economic settlement lease."),
        };

        // Gross, fee and net are only knowable from an accepted receipt, and they are written in
        // the same conditional update as the operation id so a settlement can never carry an op
        // whose fee nobody recorded (gotcha 11: a silently shaved node fee).
        var receipt = finalization.Outcome == EconomicOutboxRecordedOutcome.Accepted
            ? EconomicSettlementReceipt.FromJson(finalization.Receipt)
            : null;
        var grossBaseUnits = receipt?.GrossBaseUnits;
        var feeBaseUnits = receipt?.FeeBaseUnits;
        var netBaseUnits = receipt?.NetBaseUnits;
        // A dispatch whose answer was never read (Unknown) may still land: like an expired-lease takeover, its
        // claim stamp becomes the settlement's unanswered-send stamp, so no later close can say "never sent".
        var unansweredSendAt = finalization.Outcome == EconomicOutboxRecordedOutcome.Unknown
            && lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
                ? lease.LastAttemptAt
                : null;

        var settlementUpdated = await _context.EconomicSettlements
            .Where(item => item.id == lease.SettlementId
                && item.status == lease.SettlementStatus
                && item.version == lease.SettlementVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, settlementStatus)
                .SetProperty(item => item.azoaOperationId, finalization.OperationId)
                .SetProperty(item => item.azoaReceipt, finalization.Receipt)
                .SetProperty(item => item.azoaReplayed, finalization.Replayed)
                .SetProperty(item => item.azoaGrossBaseUnits, grossBaseUnits)
                .SetProperty(item => item.azoaFeeBaseUnits, feeBaseUnits)
                .SetProperty(item => item.azoaNetBaseUnits, netBaseUnits)
                .SetProperty(item => item.failureCode, finalization.FailureCode)
                .SetProperty(item => item.failureDetail, finalization.FailureDetail)
                .SetProperty(
                    item => item.submittedAt,
                    item => finalization.Outcome == EconomicOutboxRecordedOutcome.Accepted
                        ? now
                        : unansweredSendAt != null && (item.submittedAt == null || item.submittedAt < unansweredSendAt)
                            ? unansweredSendAt
                            : item.submittedAt)
                .SetProperty(item => item.updatedAt, now)
                .SetProperty(item => item.version, item => item.version + 1), ct);
        if (settlementUpdated != 1)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> TryReleaseForDispatchAsync(string settlementId, DateTime now, CancellationToken ct = default)
    {
        // submittedAt is the durable "a send may start now" stamp, written BEFORE the send exists, so a
        // lost answer still leaves the instant a receipt 404 is measured from (Implementations/AGENTS.md §Node proof).
        var released = await _context.EconomicSettlements
            .Where(item => item.id == settlementId && item.status == EconomicSettlementStatus.AUTHORIZED)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, EconomicSettlementStatus.PENDING_DISPATCH)
                .SetProperty(item => item.submittedAt, now)
                .SetProperty(item => item.updatedAt, now)
                .SetProperty(item => item.version, item => item.version + 1), ct);
        return released == 1;
    }

    /// <inheritdoc/>
    public async Task<bool> TryConfirmOpenAsync(string settlementId, DateTime now, CancellationToken ct = default)
    {
        // Outbox row FIRST - the lock order FinalizeAsync and CancelBothAsync take - so the caller's later
        // outbox write cannot deadlock with a worker finalizing the same settlement (held to the caller's commit).
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM "EconomicOutbox" WHERE "settlementId" = {settlementId} FOR UPDATE""", ct);

        var confirmed = await _context.EconomicSettlements
            .Where(item => item.id == settlementId
                && (item.status == EconomicSettlementStatus.PENDING_DISPATCH
                    || item.status == EconomicSettlementStatus.SUBMITTED
                    || item.status == EconomicSettlementStatus.AWAITING_RECONCILIATION
                    || item.status == EconomicSettlementStatus.CONFIRMED))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, EconomicSettlementStatus.CONFIRMED)
                .SetProperty(item => item.updatedAt, now), ct);
        return confirmed == 1;
    }

    /// <inheritdoc/>
    public async Task<EconomicSettlementStatus?> ReadSettlementStatusAsync(string settlementId, CancellationToken ct = default)
        => await _context.EconomicSettlements
            .AsNoTracking()
            .Where(item => item.id == settlementId)
            .Select(item => (EconomicSettlementStatus?)item.status)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public Task<bool> TryCancelUndispatchedAsync(
        string settlementId,
        string failureCode,
        string? failureDetail,
        DateTime now,
        CancellationToken ct = default)
        => CancelBothAsync(
            settlementId,
            settlement => settlement.status == EconomicSettlementStatus.AUTHORIZED,
            failureCode,
            failureDetail,
            clearSubmission: false,
            now,
            ct);

    /// <inheritdoc/>
    public Task<bool> TryCancelUnleasedAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        bool nothingWasSubmitted,
        DateTime now,
        CancellationToken ct = default)
        => CancelBothAsync(
            settlementId,
            settlement => settlement.status == EconomicSettlementStatus.PENDING_DISPATCH
                && settlement.version == expectedSettlementVersion,
            failureCode,
            failureDetail,
            nothingWasSubmitted,
            now,
            ct);

    /// <inheritdoc/>
    public Task<bool> TryParkUnleasedForReconciliationAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        DateTime availableAt,
        DateTime now,
        CancellationToken ct = default)
        => InOneTransactionAsync("economic_outbox_park", async () =>
        {
            var parkedOutbox = await UnleasedPendingOutbox(settlementId, expectedSettlementVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.status, EconomicOutboxStatus.AWAITING_RECONCILIATION)
                    .SetProperty(item => item.reconciliationRequiredAt, now)
                    .SetProperty(item => item.availableAt, availableAt)
                    .SetProperty(item => item.failureCode, failureCode)
                    .SetProperty(item => item.failureDetail, failureDetail)
                    .SetProperty(item => item.updatedAt, now), ct);

            return parkedOutbox == 1
                && await DispatchableSettlement(settlementId, expectedSettlementVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.status, EconomicSettlementStatus.AWAITING_RECONCILIATION)
                        .SetProperty(item => item.failureCode, failureCode)
                        .SetProperty(item => item.failureDetail, failureDetail)
                        .SetProperty(item => item.submittedAt, item => item.submittedAt ?? now)
                        .SetProperty(item => item.updatedAt, now)
                        .SetProperty(item => item.version, item => item.version + 1), ct) == 1;
        }, ct);

    /// <inheritdoc/>
    public Task<bool> TryLeaveUnleasedToWorkerAsync(
        string settlementId,
        int expectedSettlementVersion,
        string? failureCode,
        string? failureDetail,
        DateTime availableAt,
        DateTime now,
        CancellationToken ct = default)
        => InOneTransactionAsync("economic_outbox_leave", async () =>
        {
            var deferredOutbox = await UnleasedPendingOutbox(settlementId, expectedSettlementVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.availableAt, availableAt)
                    .SetProperty(item => item.failureCode, failureCode)
                    .SetProperty(item => item.failureDetail, failureDetail)
                    .SetProperty(item => item.updatedAt, now), ct);

            return deferredOutbox == 1
                && await DispatchableSettlement(settlementId, expectedSettlementVersion)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.failureCode, failureCode)
                        .SetProperty(item => item.failureDetail, failureDetail)
                        .SetProperty(item => item.updatedAt, now)
                        .SetProperty(item => item.version, item => item.version + 1), ct) == 1;
        }, ct);

    /// <summary>The settlement at exactly the version the caller released or read, still PENDING_DISPATCH.</summary>
    private IQueryable<ArdaNova.Domain.Models.Entities.EconomicSettlement> DispatchableSettlement(
        string settlementId,
        int expectedSettlementVersion)
        => _context.EconomicSettlements.Where(item => item.id == settlementId
            && item.status == EconomicSettlementStatus.PENDING_DISPATCH
            && item.version == expectedSettlementVersion);

    /// <summary>Its outbox, only while PENDING with no lease: a worker holding one may be mid-dispatch.</summary>
    private IQueryable<ArdaNova.Domain.Models.Entities.EconomicOutbox> UnleasedPendingOutbox(
        string settlementId,
        int expectedSettlementVersion)
    {
        var settlement = DispatchableSettlement(settlementId, expectedSettlementVersion);
        return _context.EconomicOutboxes.Where(item => item.settlementId == settlementId
            && item.status == EconomicOutboxStatus.PENDING
            && item.leaseToken == null
            && settlement.Any());
    }

    /// <summary>
    /// Compare-and-set cancel of one outbox and its settlement, outbox row first - the lock order
    /// <see cref="FinalizeAsync"/> takes - so it can neither deadlock with nor overwrite a lease holder.
    /// </summary>
    private Task<bool> CancelBothAsync(
        string settlementId,
        System.Linq.Expressions.Expression<Func<ArdaNova.Domain.Models.Entities.EconomicSettlement, bool>> settlementIsCancellable,
        string? failureCode,
        string? failureDetail,
        bool clearSubmission,
        DateTime now,
        CancellationToken ct)
        => InOneTransactionAsync("economic_outbox_cancel", async () =>
        {
            var cancellableSettlement = _context.EconomicSettlements
                .Where(item => item.id == settlementId)
                .Where(settlementIsCancellable);

            var outboxCancelled = await _context.EconomicOutboxes
                .Where(item => item.settlementId == settlementId
                    && item.status == EconomicOutboxStatus.PENDING
                    && item.leaseToken == null
                    && cancellableSettlement.Any())
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.status, EconomicOutboxStatus.CANCELLED)
                    .SetProperty(item => item.failureCode, failureCode)
                    .SetProperty(item => item.failureDetail, failureDetail)
                    .SetProperty(item => item.updatedAt, now), ct);

            return outboxCancelled == 1
                && await cancellableSettlement
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.status, EconomicSettlementStatus.CANCELLED)
                        .SetProperty(item => item.failureCode, failureCode)
                        .SetProperty(item => item.failureDetail, failureDetail)
                        .SetProperty(item => item.submittedAt, item => clearSubmission ? null : item.submittedAt)
                        .SetProperty(item => item.updatedAt, now)
                        .SetProperty(item => item.version, item => item.version + 1), ct) == 1;
        }, ct);

    /// <summary>
    /// Runs <paramref name="body"/> in its own transaction, or a savepoint inside the caller's, and keeps
    /// its writes only when it returns true - so a half-landed compare-and-set pair never survives.
    /// </summary>
    private async Task<bool> InOneTransactionAsync(string savepoint, Func<Task<bool>> body, CancellationToken ct)
    {
        var ambient = _context.Database.CurrentTransaction;
        var owned = ambient is null ? await _context.Database.BeginTransactionAsync(ct) : null;
        if (ambient is not null)
            await ambient.CreateSavepointAsync(savepoint, ct);

        try
        {
            if (!await body())
            {
                if (owned is not null)
                    await owned.RollbackAsync(CancellationToken.None);
                else
                    await ambient!.RollbackToSavepointAsync(savepoint, CancellationToken.None);
                return false;
            }

            if (owned is not null)
                await owned.CommitAsync(ct);
            return true;
        }
        finally
        {
            if (owned is not null)
                await owned.DisposeAsync();
        }
    }

    private IQueryable<ArdaNova.Domain.Models.Entities.EconomicOutbox> EligibleOutboxes(
        EconomicOutboxClaimKind kind,
        DateTime now)
        => kind == EconomicOutboxClaimKind.Dispatch
            ? _context.EconomicOutboxes.Where(item =>
                item.Settlement != null
                && item.Settlement.status == EconomicSettlementStatus.PENDING_DISPATCH
                && ((item.status == EconomicOutboxStatus.PENDING && item.availableAt <= now)
                    || (item.status == EconomicOutboxStatus.LEASED
                        && item.leaseExpiresAt <= now
                        && item.reconciliationRequiredAt == null)))
            : _context.EconomicOutboxes.Where(item =>
                item.Settlement != null
                && item.Settlement.status == EconomicSettlementStatus.AWAITING_RECONCILIATION
                && ((item.status == EconomicOutboxStatus.AWAITING_RECONCILIATION && item.availableAt <= now)
                    || (item.status == EconomicOutboxStatus.LEASED
                        && item.leaseExpiresAt <= now
                        && item.reconciliationRequiredAt != null)));

}

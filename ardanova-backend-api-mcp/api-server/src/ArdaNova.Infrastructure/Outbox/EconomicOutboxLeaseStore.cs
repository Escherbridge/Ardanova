namespace ArdaNova.Infrastructure.Outbox;

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
        var claimed = await EligibleOutboxes(kind, now)
            .Where(item => item.id == candidate.id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, EconomicOutboxStatus.LEASED)
                .SetProperty(item => item.leaseToken, leaseToken)
                .SetProperty(item => item.leaseExpiresAt, expiresAt)
                .SetProperty(item => item.lastAttemptAt, now)
                .SetProperty(item => item.attemptCount, item => item.attemptCount + 1)
                .SetProperty(item => item.updatedAt, now), ct);
        if (claimed != 1)
            return null;

        var outbox = await _context.EconomicOutboxes
            .AsNoTracking()
            .Include(item => item.Settlement)
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
                settlement.termsSnapshot));
    }

    /// <inheritdoc/>
    public async Task<bool> FinalizeAsync(
        EconomicOutboxLease lease,
        EconomicOutboxFinalization finalization,
        DateTime now,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var outboxStatus = finalization.Outcome switch
        {
            EconomicOutboxRecordedOutcome.Accepted => EconomicOutboxStatus.SUBMITTED,
            EconomicOutboxRecordedOutcome.Unknown => EconomicOutboxStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry when lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
                => EconomicOutboxStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry => EconomicOutboxStatus.PENDING,
            _ => throw new InvalidOperationException("Only gateway outcomes may finalize an economic outbox lease."),
        };
        DateTime? reconciliationAt = finalization.Outcome switch
        {
            EconomicOutboxRecordedOutcome.Accepted => null,
            EconomicOutboxRecordedOutcome.Unknown => now,
            _ => lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation ? now : null,
        };
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
                .SetProperty(item => item.reconciliationRequiredAt, reconciliationAt)
                .SetProperty(item => item.failureCode, finalization.FailureCode)
                .SetProperty(item => item.failureDetail, finalization.FailureDetail)
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
            EconomicOutboxRecordedOutcome.Retry when lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
                => EconomicSettlementStatus.AWAITING_RECONCILIATION,
            EconomicOutboxRecordedOutcome.Retry => EconomicSettlementStatus.PENDING_DISPATCH,
            _ => throw new InvalidOperationException("Only gateway outcomes may finalize an economic settlement lease."),
        };
        var settlementUpdated = await _context.EconomicSettlements
            .Where(item => item.id == lease.SettlementId
                && item.status == lease.SettlementStatus
                && item.version == lease.SettlementVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, settlementStatus)
                .SetProperty(item => item.azoaOperationId, finalization.OperationId)
                .SetProperty(item => item.azoaReceipt, finalization.Receipt)
                .SetProperty(item => item.azoaReplayed, finalization.Replayed)
                .SetProperty(item => item.failureCode, finalization.FailureCode)
                .SetProperty(item => item.failureDetail, finalization.FailureDetail)
                .SetProperty(item => item.submittedAt, item => finalization.Outcome == EconomicOutboxRecordedOutcome.Accepted ? now : item.submittedAt)
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

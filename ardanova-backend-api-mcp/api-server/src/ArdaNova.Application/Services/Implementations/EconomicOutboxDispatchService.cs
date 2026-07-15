namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using System.IO;
using System.Net.Http;

/// <inheritdoc/>
public sealed class EconomicOutboxDispatchService : IEconomicOutboxDispatchService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private readonly IEconomicOutboxLeaseStore _store;
    private readonly IAzoaSettlementGateway _gateway;

    public EconomicOutboxDispatchService(
        IEconomicOutboxLeaseStore store,
        IAzoaSettlementGateway gateway)
    {
        _store = store;
        _gateway = gateway;
    }

    /// <inheritdoc/>
    public Task<EconomicOutboxDispatchRun> DispatchOneAsync(CancellationToken ct = default)
        => RunOneAsync(EconomicOutboxClaimKind.Dispatch, ct);

    /// <inheritdoc/>
    public Task<EconomicOutboxDispatchRun> ReconcileOneAsync(CancellationToken ct = default)
        => RunOneAsync(EconomicOutboxClaimKind.Reconciliation, ct);

    private async Task<EconomicOutboxDispatchRun> RunOneAsync(
        EconomicOutboxClaimKind kind,
        CancellationToken ct)
    {
        var claimTime = DateTime.UtcNow;
        var lease = await _store.TryClaimAsync(kind, claimTime, LeaseDuration, ct);
        if (lease is null)
            return new EconomicOutboxDispatchRun(EconomicOutboxRecordedOutcome.NoWork);
        if (lease.LeaseExpiresAt <= claimTime
            || lease.ClaimKind != kind
            || !HasExpectedSettlementStatus(lease))
        {
            return new EconomicOutboxDispatchRun(
                EconomicOutboxRecordedOutcome.StaleLease,
                lease.OutboxId,
                lease.SettlementId);
        }

        var gatewayResult = await ExecuteGatewayAsync(lease, ct);
        var finalizedAt = DateTime.UtcNow;
        var finalization = ToFinalization(lease, gatewayResult, finalizedAt);
        var finalized = await _store.FinalizeAsync(lease, finalization, finalizedAt, ct);
        return new EconomicOutboxDispatchRun(
            finalized ? finalization.Outcome : EconomicOutboxRecordedOutcome.StaleLease,
            lease.OutboxId,
            lease.SettlementId);
    }

    private async Task<AzoaSettlementGatewayResult> ExecuteGatewayAsync(
        EconomicOutboxLease lease,
        CancellationToken ct)
    {
        try
        {
            return lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
                ? await _gateway.DispatchAsync(lease.Request, ct)
                : await _gateway.ReconcileAsync(lease.Request, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return AmbiguousTransportResult();
        }
        catch (TimeoutException)
        {
            return AmbiguousTransportResult();
        }
        catch (IOException)
        {
            return AmbiguousTransportResult();
        }
        catch (OperationCanceledException)
        {
            return AmbiguousTransportResult();
        }
    }

    private static AzoaSettlementGatewayResult AmbiguousTransportResult()
        => AzoaSettlementGatewayResult.Unknown(
            "AZOA_TRANSPORT_UNCERTAIN",
            "The AZOA transport did not return a durable result; reconciliation is required before another dispatch.");

    private static bool HasExpectedSettlementStatus(EconomicOutboxLease lease)
        => lease.ClaimKind switch
        {
            EconomicOutboxClaimKind.Dispatch => lease.SettlementStatus == EconomicSettlementStatus.PENDING_DISPATCH,
            EconomicOutboxClaimKind.Reconciliation => lease.SettlementStatus == EconomicSettlementStatus.AWAITING_RECONCILIATION,
            _ => false,
        };

    private static EconomicOutboxFinalization ToFinalization(
        EconomicOutboxLease lease,
        AzoaSettlementGatewayResult result,
        DateTime now)
    {
        if (result.Outcome == AzoaSettlementGatewayOutcome.Accepted
            && !string.IsNullOrWhiteSpace(result.OperationId))
        {
            return new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Accepted,
                now,
                result.OperationId,
                result.Receipt,
                result.Replayed,
                null,
                null);
        }

        if (result.Outcome == AzoaSettlementGatewayOutcome.Unknown
            || (result.Outcome == AzoaSettlementGatewayOutcome.Accepted
                && string.IsNullOrWhiteSpace(result.OperationId)))
        {
            return new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Unknown,
                now,
                null,
                null,
                null,
                result.Code ?? "AZOA_AMBIGUOUS_RESULT",
                result.Detail ?? "AZOA accepted neither a durable operation id nor a safely retryable result.");
        }

        return new EconomicOutboxFinalization(
            EconomicOutboxRecordedOutcome.Retry,
            now.Add(RetryDelay(lease.AttemptCount)),
            null,
            null,
            null,
            result.Code ?? "AZOA_RETRY",
            result.Detail);
    }

    private static TimeSpan RetryDelay(int attemptCount)
        => TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, Math.Min(6, Math.Max(0, attemptCount - 1)))));
}

namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using System.IO;
using System.Net.Http;

/// <inheritdoc/>
public sealed class EconomicOutboxDispatchService : IEconomicOutboxDispatchService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultGatewayOperationDeadline = TimeSpan.FromMinutes(4.5);
    private readonly IEconomicOutboxLeaseStore _store;
    private readonly IAzoaSettlementGateway _gateway;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly TimeSpan _gatewayOperationDeadline;

    public EconomicOutboxDispatchService(
        IEconomicOutboxLeaseStore store,
        IAzoaSettlementGateway gateway,
        IAzoaValueEligibilityService valueEligibility,
        TimeSpan? gatewayOperationDeadline = null)
    {
        var deadline = gatewayOperationDeadline ?? DefaultGatewayOperationDeadline;
        if (deadline <= TimeSpan.Zero || deadline >= LeaseDuration)
            throw new ArgumentOutOfRangeException(nameof(gatewayOperationDeadline));

        _store = store;
        _gateway = gateway;
        _valueEligibility = valueEligibility;
        _gatewayOperationDeadline = deadline;
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

        var gatewayResult = lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
            ? await DispatchIfEligibleAsync(lease, ct)
            : await ExecuteGatewayWithRenewalAsync(lease, ct);
        if (gatewayResult is null)
        {
            return new EconomicOutboxDispatchRun(
                EconomicOutboxRecordedOutcome.StaleLease,
                lease.OutboxId,
                lease.SettlementId);
        }
        var finalizedAt = DateTime.UtcNow;
        var finalization = ToFinalization(lease, gatewayResult, finalizedAt);
        var finalized = await _store.FinalizeAsync(lease, finalization, finalizedAt, ct);
        return new EconomicOutboxDispatchRun(
            finalized ? finalization.Outcome : EconomicOutboxRecordedOutcome.StaleLease,
            lease.OutboxId,
            lease.SettlementId);
    }

    private async Task<AzoaSettlementGatewayResult?> ExecuteGatewayWithRenewalAsync(
        EconomicOutboxLease lease,
        CancellationToken ct)
    {
        var renewedLease = await RenewLeaseAsync(lease, ct);
        return renewedLease is null ? null : await ExecuteGatewayAsync(renewedLease, ct);
    }

    private async Task<AzoaSettlementGatewayResult> ExecuteGatewayAsync(
        EconomicOutboxLease lease,
        CancellationToken ct)
    {
        using var gatewayDeadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        gatewayDeadline.CancelAfter(_gatewayOperationDeadline);
        try
        {
            var gatewayCall = lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
                ? _gateway.DispatchAsync(lease.Request, gatewayDeadline.Token)
                : _gateway.ReconcileAsync(lease.Request, gatewayDeadline.Token);
            return await gatewayCall.WaitAsync(gatewayDeadline.Token);
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

    private async Task<AzoaSettlementGatewayResult?> DispatchIfEligibleAsync(
        EconomicOutboxLease lease,
        CancellationToken ct)
    {
        var renewedLease = await RenewLeaseAsync(lease, ct);
        if (renewedLease is null)
            return null;

        var readiness = await _valueEligibility.RequireReadyAsync(
            renewedLease.Request.BeneficiaryUserId,
            "receive settled project value",
            ct);
        if (readiness.IsFailure)
        {
            // Retrying is right either way, but the reason code is what an operator triages
            // on: "not ready" points at the recipient, "unconfirmed" points at the platform.
            return AzoaSettlementGatewayResult.Retry(
                readiness.Type == ResultType.Conflict
                    ? "AZOA_ELIGIBILITY_UNCONFIRMED"
                    : "AZOA_ACCOUNT_NOT_READY",
                readiness.Error ?? "The recipient must complete account setup before settlement can be dispatched.");
        }

        if (renewedLease.SettlementKind == EconomicSettlementKind.FUNDING_ALLOCATION)
        {
            var projectOwnerUserId = renewedLease.ProjectOwnerUserId;
            if (string.IsNullOrWhiteSpace(projectOwnerUserId))
            {
                return AzoaSettlementGatewayResult.Retry(
                    "AZOA_PROJECT_OWNER_UNAVAILABLE",
                    "Funding settlement remains pending until the project owner can be verified.");
            }

            renewedLease = await RenewLeaseAsync(renewedLease, ct);
            if (renewedLease is null)
                return null;

            var ownerReadiness = await _valueEligibility.RequireReadyAsync(
                projectOwnerUserId,
                "receive project funding",
                ct);
            if (ownerReadiness.IsFailure)
            {
                return AzoaSettlementGatewayResult.Retry(
                    "AZOA_PROJECT_OWNER_NOT_READY",
                    "Funding settlement remains pending until the project owner completes account setup.");
            }
        }

        return await ExecuteGatewayWithRenewalAsync(renewedLease, ct);
    }

    private Task<EconomicOutboxLease?> RenewLeaseAsync(
        EconomicOutboxLease lease,
        CancellationToken ct)
        => _store.TryRenewAsync(lease, DateTime.UtcNow, LeaseDuration, ct);

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

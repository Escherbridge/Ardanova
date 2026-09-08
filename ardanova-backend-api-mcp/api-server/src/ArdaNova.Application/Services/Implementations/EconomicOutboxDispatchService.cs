namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Stable codes the dispatch pipeline emits and the persistence seam branches on.</summary>
/// <remarks>
/// These are not log strings: <c>EconomicOutboxLeaseStore</c> reads them back out of
/// <see cref="EconomicOutboxFinalization.FailureCode"/> and they decide the row's next state. The
/// finalization record carries no other channel for that decision, so the code IS the control.
/// </remarks>
public static class EconomicSettlementFailureCodes
{
    /// <summary>Dispatch exhausted its attempt ceiling on definite refusals, so no value ever moved.</summary>
    public const string DispatchAttemptCeiling = "AZOA_SETTLEMENT_DISPATCH_CEILING";

    /// <summary>Reconciliation exhausted its ceiling while the economic effect is still unknown.</summary>
    public const string ReconciliationAttentionRequired = "AZOA_SETTLEMENT_RECONCILIATION_ATTENTION";

    /// <summary>The node holds no operation under this key, so the settlement may be dispatched again.</summary>
    public const string ExchangeNeverLanded = "AZOA_EXCHANGE_NEVER_LANDED";
}

/// <summary>Which settlement kinds credit a project and therefore need its owner ready as well.</summary>
/// <remarks>
/// One definition, read at BOTH seams: the lease store decides whether to resolve a project owner
/// at all, and the dispatch service decides whether to gate on that owner's readiness. A kind added
/// to one list and not the other is how a settlement reaches the node with half a gate.
/// </remarks>
public static class EconomicSettlementKindRules
{
    /// <summary>True when settling this kind credits a project, not only its beneficiary.</summary>
    public static bool RequiresProjectOwnerReadiness(EconomicSettlementKind kind)
        => kind is EconomicSettlementKind.FUNDING_ALLOCATION or EconomicSettlementKind.EXCHANGE;
}

/// <summary>What an accepted settlement recorded at the node boundary, including the fee it revealed.</summary>
/// <remarks>
/// The gateway serialises this into <see cref="EconomicOutboxFinalization.Receipt"/> and the lease
/// store parses it back out to fill the settlement's typed gross/fee/net columns. It exists because
/// the finalization record has exactly one free-form field, and a fee that only lives in a log is a
/// fee nobody can reconcile.
/// </remarks>
public sealed record EconomicSettlementReceipt(
    string? OperationId,
    string? ReceiptReference,
    string? State,
    bool Replayed,
    string? GrossBaseUnits,
    string? FeeBaseUnits,
    string? NetBaseUnits,
    int? Scale,
    string? SettlementReference,
    string? HoldReference,
    DateTimeOffset? HoldReleasesAt)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serialises the receipt for the settlement's jsonb column.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    /// <summary>Reads a persisted receipt back, returning null rather than throwing on anything unreadable.</summary>
    public static EconomicSettlementReceipt? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<EconomicSettlementReceipt>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <inheritdoc/>
public sealed class EconomicOutboxDispatchService : IEconomicOutboxDispatchService
{
    /// <summary>Attempts a settlement gets before a human is told instead of a machine retried.</summary>
    public const int DefaultMaximumAttempts = 10;

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultGatewayOperationDeadline = TimeSpan.FromMinutes(4.5);

    /// <summary>Backoff applied once reconciliation has passed its ceiling; slow, but never terminal.</summary>
    private static readonly TimeSpan CeilingBackoff = TimeSpan.FromHours(1);

    private readonly IEconomicOutboxLeaseStore _store;
    private readonly IAzoaSettlementGateway _gateway;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly TimeSpan _gatewayOperationDeadline;
    private readonly int _maximumAttempts;
    private readonly ILogger<EconomicOutboxDispatchService>? _logger;

    public EconomicOutboxDispatchService(
        IEconomicOutboxLeaseStore store,
        IAzoaSettlementGateway gateway,
        IAzoaValueEligibilityService valueEligibility,
        TimeSpan? gatewayOperationDeadline = null,
        int? maximumAttempts = null,
        ILogger<EconomicOutboxDispatchService>? logger = null)
    {
        var deadline = gatewayOperationDeadline ?? DefaultGatewayOperationDeadline;
        if (deadline <= TimeSpan.Zero || deadline >= LeaseDuration)
            throw new ArgumentOutOfRangeException(nameof(gatewayOperationDeadline));

        var attempts = maximumAttempts ?? DefaultMaximumAttempts;
        if (attempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));

        _store = store;
        _gateway = gateway;
        _valueEligibility = valueEligibility;
        _gatewayOperationDeadline = deadline;
        _maximumAttempts = attempts;
        _logger = logger;
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

        if (EconomicSettlementKindRules.RequiresProjectOwnerReadiness(renewedLease.SettlementKind))
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

    private EconomicOutboxFinalization ToFinalization(
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
            // An ambiguous result past the ceiling is the one case a machine must stop owning:
            // value may have moved, so nothing here may mark it failed. Alert and back off.
            if (lease.AttemptCount >= _maximumAttempts)
                return ReconciliationCeiling(lease, now, result);

            return new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Unknown,
                now,
                null,
                null,
                null,
                result.Code ?? "AZOA_AMBIGUOUS_RESULT",
                result.Detail ?? "AZOA accepted neither a durable operation id nor a safely retryable result.");
        }

        if (lease.AttemptCount >= _maximumAttempts)
        {
            // A Retry is the gateway's word that nothing moved. On the dispatch path that makes
            // the ceiling safely terminal; on the reconciliation path it does not, because the
            // row only reached reconciliation after an outcome nobody could read.
            return lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
                ? DispatchCeiling(lease, now, result)
                : ReconciliationCeiling(lease, now, result);
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

    /// <summary>Parks a never-dispatched settlement and tells an operator; the lease store makes it terminal.</summary>
    private EconomicOutboxFinalization DispatchCeiling(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult result)
    {
        _logger?.LogCritical(
            "Economic settlement {SettlementId} (outbox {OutboxId}) reached the dispatch attempt ceiling of "
            + "{MaximumAttempts} after {AttemptCount} attempts and was parked as failed. Nothing was settled. "
            + "Last gateway code: {GatewayCode}.",
            lease.SettlementId,
            lease.OutboxId,
            _maximumAttempts,
            lease.AttemptCount,
            result.Code ?? "AZOA_RETRY");

        return new EconomicOutboxFinalization(
            EconomicOutboxRecordedOutcome.Retry,
            now,
            null,
            null,
            null,
            EconomicSettlementFailureCodes.DispatchAttemptCeiling,
            "Dispatch was refused on every attempt up to the ceiling. No value was settled; an operator must "
            + "resolve the refusal before this settlement can be reopened.");
    }

    /// <summary>Keeps an ambiguous settlement claimable but slow, and tells an operator every cycle.</summary>
    private EconomicOutboxFinalization ReconciliationCeiling(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult result)
    {
        _logger?.LogCritical(
            "Economic settlement {SettlementId} (outbox {OutboxId}) passed the reconciliation attempt ceiling of "
            + "{MaximumAttempts} after {AttemptCount} attempts and its economic effect is still unknown. It was "
            + "NOT failed. An operator must reconcile it against the node. Last gateway code: {GatewayCode}.",
            lease.SettlementId,
            lease.OutboxId,
            _maximumAttempts,
            lease.AttemptCount,
            result.Code ?? "AZOA_AMBIGUOUS_RESULT");

        return new EconomicOutboxFinalization(
            EconomicOutboxRecordedOutcome.Unknown,
            now.Add(CeilingBackoff),
            null,
            null,
            null,
            EconomicSettlementFailureCodes.ReconciliationAttentionRequired,
            "Reconciliation passed its attempt ceiling without a readable node outcome. This settlement stays "
            + "open on purpose - value may have moved - and needs an operator.");
    }

    private static TimeSpan RetryDelay(int attemptCount)
        => TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, Math.Min(6, Math.Max(0, attemptCount - 1)))));
}

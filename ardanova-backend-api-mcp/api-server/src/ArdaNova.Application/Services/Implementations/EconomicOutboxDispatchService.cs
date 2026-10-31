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

    /// <summary>The node reported no operation before the last dispatch's lease could have died; read again later.</summary>
    public const string ExchangeAbsenceUnsettled = "AZOA_EXCHANGE_ABSENCE_UNSETTLED";

    /// <summary>
    /// This attempt admitted nothing: the node refused it past its <c>Admit-Before</c>, or it was never sent
    /// because the deadline had already passed. A Retry for THIS attempt only - never proof the key is unused.
    /// </summary>
    public const string AdmissionDeadlinePassed = "AZOA_ADMISSION_DEADLINE_PASSED";

    /// <summary>True for the bookkeeping-only reconciliation outcomes that must not spend a dispatch attempt.</summary>
    public static bool IsNoEffectBookkeeping(string? code)
        => code is ExchangeNeverLanded or ExchangeAbsenceUnsettled;
}

/// <summary>The refusals that mean a person's account readiness lapsed, so nothing was ever dispatched.</summary>
/// <remarks>
/// These four codes are the platform's HOLDING marker and they are a wire contract, not log text:
/// <c>FundingIntentService</c> reads the same four off a settlement row to decide that a sponsorship
/// is held rather than failed. Nothing here may rename one, and a settlement carrying one of them
/// must never be given the transport dispatch ceiling - value is waiting on a person, not on a
/// retry. See <c>Services/Implementations/AGENTS.md</c>, "Reward dispatch hygiene".
/// </remarks>
public static class EconomicSettlementReadinessCodes
{
    /// <summary>The recipient's account was read and setup is genuinely unfinished.</summary>
    public const string BeneficiaryNotReady = "AZOA_ACCOUNT_NOT_READY";

    /// <summary>Readiness could not be confirmed at all; this points at the platform, not the person.</summary>
    public const string BeneficiaryUnconfirmed = "AZOA_ELIGIBILITY_UNCONFIRMED";

    /// <summary>A project-crediting settlement has no resolvable project owner to check.</summary>
    public const string ProjectOwnerUnavailable = "AZOA_PROJECT_OWNER_UNAVAILABLE";

    /// <summary>The project owner's account was read and setup is genuinely unfinished.</summary>
    public const string ProjectOwnerNotReady = "AZOA_PROJECT_OWNER_NOT_READY";

    /// <summary>True when a refusal means an account is not ready, so nothing left ArdaNova.</summary>
    public static bool IsReadinessLapse(string? code)
        => code is BeneficiaryNotReady
            or BeneficiaryUnconfirmed
            or ProjectOwnerUnavailable
            or ProjectOwnerNotReady;
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

    /// <summary>Readiness attempt at which a refusal stops being transient and the row is HOLDING.</summary>
    public const int ReadinessHoldingEntryAttempts = 3;

    /// <summary>Holding attempts between operator alerts; loud enough to be seen, quiet enough not to page nightly.</summary>
    public const int ReadinessHoldingAlertInterval = 14;

    /// <summary>Flat cadence a held settlement is re-checked on; long, because only a person can clear it.</summary>
    public static readonly TimeSpan ReadinessHoldingBackoff = TimeSpan.FromHours(12);

    /// <summary>How long captured value may wait on unfinished setup before it is owed back to its source.</summary>
    public static readonly TimeSpan DefaultReadinessHoldingWindow = TimeSpan.FromDays(14);

    private static readonly TimeSpan MaximumReadinessHoldingWindow = TimeSpan.FromDays(365);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultGatewayOperationDeadline = TimeSpan.FromMinutes(4.5);

    /// <summary>Backoff applied once reconciliation has passed its ceiling; slow, but never terminal.</summary>
    private static readonly TimeSpan CeilingBackoff = TimeSpan.FromHours(1);

    /// <summary>Receipt-404 reopens one settlement may take before it parks for an operator instead.</summary>
    public const int MaximumNoEffectReopens = 5;

    /// <summary>How long after a row turned ambiguous a 404 is believed under the DEFAULT admission policy.</summary>
    /// <remarks>
    /// The instance uses its <see cref="ExchangeAdmissionPolicy.ProofWindow"/>; see <c>Infrastructure/Outbox/AGENTS.md</c>,
    /// "No-effect reopen", and <c>Implementations/AGENTS.md</c> §Node proof.
    /// </remarks>
    public static TimeSpan NoEffectQuietWindow => ExchangeAdmissionPolicy.Default.ProofWindow;

    private readonly IEconomicOutboxLeaseStore _store;
    private readonly IAzoaSettlementGateway _gateway;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly IOperatorAlertService _operatorAlerts;
    private readonly TimeSpan _gatewayOperationDeadline;
    private readonly TimeSpan _readinessHoldingWindow;
    private readonly int _maximumAttempts;
    private readonly ExchangeAdmissionPolicy _admission;
    private readonly ILogger<EconomicOutboxDispatchService>? _logger;

    public EconomicOutboxDispatchService(
        IEconomicOutboxLeaseStore store,
        IAzoaSettlementGateway gateway,
        IAzoaValueEligibilityService valueEligibility,
        TimeSpan? gatewayOperationDeadline = null,
        int? maximumAttempts = null,
        ILogger<EconomicOutboxDispatchService>? logger = null,
        IOperatorAlertService? operatorAlerts = null,
        TimeSpan? readinessHoldingWindow = null,
        ExchangeAdmissionPolicy? admission = null)
    {
        var admissionPolicy = admission ?? ExchangeAdmissionPolicy.Default;
        admissionPolicy.Validate();

        var deadline = gatewayOperationDeadline ?? DefaultGatewayOperationDeadline;
        if (deadline <= TimeSpan.Zero || deadline >= LeaseDuration)
            throw new ArgumentOutOfRangeException(nameof(gatewayOperationDeadline));

        var attempts = maximumAttempts ?? DefaultMaximumAttempts;
        if (attempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));

        var holdingWindow = readinessHoldingWindow ?? DefaultReadinessHoldingWindow;
        if (holdingWindow < ReadinessHoldingBackoff || holdingWindow > MaximumReadinessHoldingWindow)
            throw new ArgumentOutOfRangeException(nameof(readinessHoldingWindow));

        _store = store;
        _gateway = gateway;
        _valueEligibility = valueEligibility;
        // Never a silent default: an unregistered alert channel falls back to critical log lines,
        // which every deployment already ships, rather than to a no-op that swallows parked value.
        _operatorAlerts = operatorAlerts ?? LoggingOperatorAlertService.For(logger);
        _gatewayOperationDeadline = deadline;
        _readinessHoldingWindow = holdingWindow;
        _maximumAttempts = attempts;
        _admission = admissionPolicy;
        _logger = logger;
    }

    /// <summary>Attempt at which a held settlement stops waiting and the reserved amount is owed back.</summary>
    /// <remarks>
    /// Expressed in attempts because the lease record carries no first-held timestamp, and safe to
    /// express that way only because the holding cadence is flat. The ordinary dispatch ceiling is
    /// added as an allowance for attempts a row may already have spent before it could be holding,
    /// which makes <see cref="DefaultReadinessHoldingWindow"/> a floor on the real wait, never a cap.
    /// </remarks>
    private int ReadinessRefundAttempts
        => ReadinessHoldingEntryAttempts
            + _maximumAttempts
            + (int)Math.Ceiling(_readinessHoldingWindow / ReadinessHoldingBackoff);

    /// <summary>The holding window in whole days, for copy a person reads.</summary>
    private int ReadinessHoldingWindowDays
        => (int)Math.Round(_readinessHoldingWindow.TotalDays, MidpointRounding.AwayFromZero);

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
        var decision = ToDecision(lease, gatewayResult, finalizedAt);
        if (decision.Closing is { } closing)
        {
            // A terminal "nothing was paid" is the node's to prove, never the lease's (§Node proof).
            var proven = await DecideClosingAsync(lease, closing, ct);
            if (proven is null)
            {
                return new EconomicOutboxDispatchRun(
                    EconomicOutboxRecordedOutcome.StaleLease,
                    lease.OutboxId,
                    lease.SettlementId);
            }

            (lease, decision, finalizedAt) = proven.Value;
        }

        // Deliberately before the write: a duplicate alert costs an operator one glance, while an
        // alert lost because the row was finalized terminally costs somebody their money.
        await RaiseAsync(decision.Alert, ct);
        var finalized = await _store.FinalizeAsync(lease, decision.Finalization, finalizedAt, ct);
        return new EconomicOutboxDispatchRun(
            finalized ? decision.Finalization.Outcome : EconomicOutboxRecordedOutcome.StaleLease,
            lease.OutboxId,
            lease.SettlementId);
    }

    /// <summary>
    /// Before a settlement is closed as "nothing was paid": if any send under its key went unanswered
    /// (<see cref="EconomicOutboxLease.SubmittedAt"/>), the node's receipt decides. Accepted completes it; a
    /// NOT_FOUND closes it only when it is PROOF (<see cref="ExchangeAdmissionPolicy.AbsenceIsBelievable"/>: an
    /// enforcing node, read started past the quiet window); inside the window on an enforcing node it defers;
    /// anything else - including any NOT_FOUND while enforcement is not attested - keeps it OPEN for an operator.
    /// Null when the lease was lost before the read. See Implementations/AGENTS.md §Node proof.
    /// </summary>
    private async Task<(EconomicOutboxLease Lease, EconomicOutboxDecision Decision, DateTime At)?> DecideClosingAsync(
        EconomicOutboxLease lease,
        ClosingDecision closing,
        CancellationToken ct)
    {
        if (lease.SubmittedAt is not { } latestUnansweredSend)
            return (lease, closing.Close(NeverSentBasis), DateTime.UtcNow);

        var renewed = await RenewLeaseAsync(lease, ct);
        if (renewed is null)
            return null;

        // Stamped BEFORE the read: a NOT_FOUND is only as late as the moment the read began, and the read itself
        // may take most of the gateway deadline - far longer than the window's spare margin.
        var readStartedAt = DateTime.UtcNow;
        var receipt = await ExecuteGatewayAsync(deadline => _gateway.ReconcileAsync(renewed.Request, deadline), ct);
        var now = DateTime.UtcNow;
        if (receipt.Outcome == AzoaSettlementGatewayOutcome.Accepted && !string.IsNullOrWhiteSpace(receipt.OperationId))
        {
            _logger?.LogWarning(
                "Economic settlement {SettlementId} (outbox {OutboxId}) was about to be closed as unpaid, but the node "
                + "holds exchange {OperationId} under its key from an earlier send. It is recorded as accepted instead.",
                renewed.SettlementId,
                renewed.OutboxId,
                receipt.OperationId);
            return (renewed, AcceptedDecision(now, receipt), now);
        }

        if (receipt is not { Outcome: AzoaSettlementGatewayOutcome.Retry, Code: EconomicSettlementFailureCodes.ExchangeNeverLanded })
            return (renewed, ClosingUnproven(renewed, now, receipt, ReceiptUnreadable), now);

        if (_admission.AbsenceIsBelievable(latestUnansweredSend, readStartedAt))
            return (renewed, closing.Close(NodeAbsenceBasis(latestUnansweredSend)), now);

        // An enforcing node's NOT_FOUND becomes proof once the window passes; without enforcement it never does.
        return _admission.NodeEnforcesAdmitBefore
            ? (renewed, DeferClosing(closing, _admission.BelievableAt(latestUnansweredSend)), now)
            : (renewed, ClosingUnproven(renewed, now, receipt, AdmissionNotEnforced), now);
    }

    /// <summary>The proof sentence for a settlement no send ever left unanswered.</summary>
    private const string NeverSentBasis =
        "No send under this key ever went unanswered: every attempt was refused before or by the node.";

    /// <summary>The proof sentence for an enforcing node's NOT_FOUND read past the quiet window.</summary>
    private string NodeAbsenceBasis(DateTime latestUnansweredSend)
        => $"The node holds no exchange under this key, read more than {_admission.QuietWindow.TotalMinutes:0} minutes after "
            + $"the last unanswered send ({latestUnansweredSend:O}); the node refuses admission past each send's deadline, "
            + "so none can land later.";

    /// <summary>Why a close was refused: the receipt could not be read at all.</summary>
    private const string ReceiptUnreadable =
        "an earlier send under this key went unanswered and the node's receipt could not be read";

    /// <summary>Why a close was refused: a NOT_FOUND that is no proof, because no node is attested to refuse late admissions.</summary>
    private const string AdmissionNotEnforced =
        "an earlier send under this key went unanswered, and although the node holds no exchange for it now, the node "
        + "is not attested to refuse late admissions (Azoa:Exchange:NodeEnforcesAdmitBefore is false), so that send "
        + "could still land";

    /// <summary>An accepted receipt found on the way to closing: record it like any accepted dispatch.</summary>
    private static EconomicOutboxDecision AcceptedDecision(DateTime now, AzoaSettlementGatewayResult receipt)
        => new(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Accepted,
                now,
                receipt.OperationId,
                receipt.Receipt,
                receipt.Replayed,
                null,
                null),
            null);

    /// <summary>An enforcing node's NOT_FOUND inside the window: stay dispatchable and decide again once it is proof.</summary>
    private static EconomicOutboxDecision DeferClosing(ClosingDecision closing, DateTime believableAt)
        => new(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                believableAt,
                null,
                null,
                null,
                closing.OpenFailureCode,
                "The node holds no exchange under this key yet, but an earlier send may still be landing; it is read "
                + "again before this settlement is closed."),
            null);

    /// <summary>Closing without the node's proof: never terminal, value MAY have moved, and an operator is told why.</summary>
    private EconomicOutboxDecision ClosingUnproven(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult receipt,
        string reason)
    {
        _logger?.LogCritical(
            "Economic settlement {SettlementId} (outbox {OutboxId}) reached a terminal ceiling, but {Reason} "
            + "(receipt {ReceiptCode}). It was NOT closed: value MAY have moved. Reconcile it; do not refund it.",
            lease.SettlementId,
            lease.OutboxId,
            reason,
            receipt.Code ?? "AZOA_AMBIGUOUS_RESULT");

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Unknown,
                now.Add(CeilingBackoff),
                null,
                null,
                null,
                EconomicSettlementFailureCodes.ReconciliationAttentionRequired,
                $"This settlement stays open on purpose - value may have moved: {reason}. It needs an operator."),
            Alert(
                lease,
                EconomicOperatorAlertCodes.ReconciliationAttentionRequired,
                OperatorAlertSeverity.Critical,
                receipt.Code ?? "AZOA_AMBIGUOUS_RESULT",
                $"Value MAY have moved: {reason}. This settlement was NOT closed.",
                "Reconcile this settlement against the node by its idempotency key. Do NOT refund or return anything "
                + "until the node's receipt has been read under an attested admission deadline."));
    }

    /// <summary>Delivers an operator alert without ever letting the alert channel cost a finalization.</summary>
    private async Task RaiseAsync(EconomicOperatorAlert? alert, CancellationToken ct)
    {
        if (alert is null)
            return;

        try
        {
            await _operatorAlerts.RaiseAsync(alert, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError(
                exception,
                "Operator alert {AlertCode} for economic settlement {SettlementId} (outbox {OutboxId}) "
                + "could not be delivered. The settlement outcome is still recorded.",
                alert.AlertCode,
                alert.SettlementId,
                alert.OutboxId);
        }
    }

    private async Task<AzoaSettlementGatewayResult?> ExecuteGatewayWithRenewalAsync(
        EconomicOutboxLease lease,
        CancellationToken ct)
    {
        var renewedLease = await RenewLeaseAsync(lease, ct);
        if (renewedLease is null)
            return null;

        return renewedLease.ClaimKind == EconomicOutboxClaimKind.Dispatch
            ? await ExecuteGatewayAsync(deadline => _gateway.DispatchAsync(WithAdmissionDeadline(renewedLease), deadline), ct)
            : await ExecuteGatewayAsync(deadline => _gateway.ReconcileAsync(renewedLease.Request, deadline), ct);
    }

    /// <summary>
    /// The worker's send carries <c>Admit-Before</c> = this claim's durable <c>lastAttemptAt</c> + W_admit, the
    /// stamp every later NOT_FOUND is measured from (§Node proof). A lease without the stamp sends none.
    /// </summary>
    private AzoaSettlementRequest WithAdmissionDeadline(EconomicOutboxLease lease)
        => lease.LastAttemptAt is { } claimedAt
            ? lease.Request with { AdmitBefore = _admission.AdmitBefore(claimedAt) }
            : lease.Request;

    /// <summary>Runs one gateway call under the deadline shorter than the lease; a local timeout is ambiguous, never a refusal.</summary>
    private async Task<AzoaSettlementGatewayResult> ExecuteGatewayAsync(
        Func<CancellationToken, Task<AzoaSettlementGatewayResult>> call,
        CancellationToken ct)
    {
        using var gatewayDeadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        gatewayDeadline.CancelAfter(_gatewayOperationDeadline);
        try
        {
            return await call(gatewayDeadline.Token).WaitAsync(gatewayDeadline.Token);
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
                    ? EconomicSettlementReadinessCodes.BeneficiaryUnconfirmed
                    : EconomicSettlementReadinessCodes.BeneficiaryNotReady,
                readiness.Error ?? "The recipient must complete account setup before settlement can be dispatched.");
        }

        if (EconomicSettlementKindRules.RequiresProjectOwnerReadiness(renewedLease.SettlementKind))
        {
            var projectOwnerUserId = renewedLease.ProjectOwnerUserId;
            if (string.IsNullOrWhiteSpace(projectOwnerUserId))
            {
                return AzoaSettlementGatewayResult.Retry(
                    EconomicSettlementReadinessCodes.ProjectOwnerUnavailable,
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
                    EconomicSettlementReadinessCodes.ProjectOwnerNotReady,
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

    /// <summary>What one gateway outcome should write, plus the person it should wake, if any.</summary>
    /// <remarks>
    /// The alert travels with the finalization so exactly one place decides "this needs a person"
    /// and exactly one place raises it. A decision that returned only a finalization is how an alert
    /// ends up asserted in prose and absent from the path that actually runs.
    /// </remarks>
    private sealed record EconomicOutboxDecision(
        EconomicOutboxFinalization Finalization,
        EconomicOperatorAlert? Alert,
        ClosingDecision? Closing = null);

    /// <summary>
    /// A decision that would END the settlement as unpaid. It is written only through <see cref="Close"/>,
    /// with the sentence that proves it; <see cref="OpenFailureCode"/> is what a deferred close keeps instead.
    /// </summary>
    private sealed record ClosingDecision(string? OpenFailureCode, Func<string, EconomicOutboxDecision> Close);

    private EconomicOutboxDecision ToDecision(
        EconomicOutboxLease lease,
        AzoaSettlementGatewayResult result,
        DateTime now)
    {
        if (result.Outcome == AzoaSettlementGatewayOutcome.Accepted
            && !string.IsNullOrWhiteSpace(result.OperationId))
        {
            return new EconomicOutboxDecision(
                new EconomicOutboxFinalization(
                    EconomicOutboxRecordedOutcome.Accepted,
                    now,
                    result.OperationId,
                    result.Receipt,
                    result.Replayed,
                    null,
                    null),
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

            return new EconomicOutboxDecision(
                new EconomicOutboxFinalization(
                    EconomicOutboxRecordedOutcome.Unknown,
                    now,
                    null,
                    null,
                    null,
                    result.Code ?? "AZOA_AMBIGUOUS_RESULT",
                    result.Detail ?? "AZOA accepted neither a durable operation id nor a safely retryable result."),
                null);
        }

        if (lease.ClaimKind == EconomicOutboxClaimKind.Reconciliation
            && result.Code == EconomicSettlementFailureCodes.ExchangeNeverLanded)
        {
            return NoEffectReceipt(lease, now, result);
        }

        // A readiness lapse is not a failing dispatch. Nothing was submitted, the reservation is
        // still standing at the node, and only a person can clear it - so it must not be spent
        // against the transport ceiling, which would mark a reward FAILED minutes after accept
        // while its reservation stayed live and unrefunded (runbook gotcha 29).
        if (lease.ClaimKind == EconomicOutboxClaimKind.Dispatch
            && EconomicSettlementReadinessCodes.IsReadinessLapse(result.Code))
        {
            return ReadinessHolding(lease, now, result);
        }

        if (lease.AttemptCount >= _maximumAttempts)
        {
            // A Retry is the gateway's word that nothing moved. On the dispatch path that makes
            // the ceiling safely terminal; on the reconciliation path it does not, because the
            // row only reached reconciliation after an outcome nobody could read.
            if (lease.ClaimKind != EconomicOutboxClaimKind.Dispatch)
                return ReconciliationCeiling(lease, now, result);

            return Closes(
                now,
                result.Code ?? "AZOA_RETRY",
                basis => DispatchCeiling(lease, now, result, basis));
        }

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                now.Add(RetryDelay(lease.AttemptCount)),
                null,
                null,
                null,
                result.Code ?? "AZOA_RETRY",
                result.Detail),
            null);
    }

    /// <summary>Turns a node receipt 404 into a reopen, a deferral, or a parked row; never a spent dispatch attempt.</summary>
    /// <remarks>Owner decision F29; see <c>Infrastructure/Outbox/AGENTS.md</c>, "No-effect reopen".</remarks>
    private EconomicOutboxDecision NoEffectReceipt(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult result)
    {
        // Timing only, never "nothing was taken": a reopen re-sends under the SAME key, which cannot double-pay.
        // Measured at the claim, before the read began, like every NOT_FOUND (§Node proof).
        var believableAt = (lease.ReconciliationRequiredAt ?? now).Add(_admission.ProofWindow);
        if ((lease.LastAttemptAt ?? now) < believableAt)
        {
            return new EconomicOutboxDecision(
                new EconomicOutboxFinalization(
                    EconomicOutboxRecordedOutcome.Retry,
                    believableAt,
                    null,
                    null,
                    null,
                    EconomicSettlementFailureCodes.ExchangeAbsenceUnsettled,
                    "The node holds no operation for this settlement yet, but the last dispatch may still have been "
                    + "in flight. It will be read again before anything is sent."),
                null);
        }

        if (lease.NoEffectReopenCount >= MaximumNoEffectReopens)
            return NoEffectReopenCeiling(lease, now, result);

        // The store refunds this reconcile claim's increment on reopen, so this is dispatches already spent.
        if (lease.AttemptCount - 1 >= _maximumAttempts)
            return ReconciliationCeiling(lease, now, result);

        _logger?.LogWarning(
            "Economic settlement {SettlementId} (outbox {OutboxId}) has no operation on the node under its "
            + "idempotency key; reopening it for dispatch under the same key (no-effect reopen {ReopenNumber} of {MaximumReopens}).",
            lease.SettlementId,
            lease.OutboxId,
            lease.NoEffectReopenCount + 1,
            MaximumNoEffectReopens);

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                now,
                null,
                null,
                null,
                EconomicSettlementFailureCodes.ExchangeNeverLanded,
                result.Detail ?? "The node holds no operation for this settlement, so it may be dispatched again."),
            null);
    }

    /// <summary>Parks a settlement whose receipt kept reading 404 across every allowed reopen.</summary>
    private EconomicOutboxDecision NoEffectReopenCeiling(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult result)
    {
        _logger?.LogCritical(
            "Economic settlement {SettlementId} (outbox {OutboxId}) was reopened {ReopenCount} times on a node "
            + "receipt 404 and the node still holds no operation for it. It was NOT failed and will not be "
            + "redispatched again until an operator looks.",
            lease.SettlementId,
            lease.OutboxId,
            lease.NoEffectReopenCount);

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Unknown,
                now.Add(CeilingBackoff),
                null,
                null,
                null,
                EconomicSettlementFailureCodes.ReconciliationAttentionRequired,
                $"Dispatched and found absent on the node {lease.NoEffectReopenCount} times. This settlement stays "
                + "open on purpose and needs an operator to find out why the node never keeps it."),
            Alert(
                lease,
                EconomicOperatorAlertCodes.ReconciliationAttentionRequired,
                OperatorAlertSeverity.Critical,
                result.Code ?? EconomicSettlementFailureCodes.ExchangeNeverLanded,
                $"Reopened {lease.NoEffectReopenCount} times because the node had no record of it, and it still has none.",
                "Find out why the node never keeps this exchange before reopening it again; nothing indicates value moved."));
    }

    /// <summary>Parks a settlement on somebody's unfinished setup: slow cadence, alerts, definite end.</summary>
    /// <remarks>
    /// The failure code stays the readiness code the gate produced. It is the platform's HOLDING
    /// marker and <c>FundingIntentService</c> reads exactly these four off the settlement row to
    /// keep a sponsorship held rather than failed; a second, prettier code here would fork that.
    /// What makes this state HOLDING rather than a retry loop is the cadence, the exemption from
    /// the transport ceiling, and the refund that ends it.
    /// </remarks>
    private EconomicOutboxDecision ReadinessHolding(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult result)
    {
        var reasonCode = result.Code ?? EconomicSettlementReadinessCodes.BeneficiaryNotReady;
        if (lease.AttemptCount >= ReadinessRefundAttempts)
            return Closes(now, reasonCode, basis => ReadinessRefundDue(lease, now, reasonCode, basis));

        if (lease.AttemptCount < ReadinessHoldingEntryAttempts)
        {
            // Still inside the transient window. A readiness service that blinked deserves a fast
            // retry, and nothing is gained by waiting half a day to find out it has recovered.
            return new EconomicOutboxDecision(
                new EconomicOutboxFinalization(
                    EconomicOutboxRecordedOutcome.Retry,
                    now.Add(RetryDelay(lease.AttemptCount)),
                    null,
                    null,
                    null,
                    reasonCode,
                    result.Detail),
                null);
        }

        var holdingAttempt = lease.AttemptCount - ReadinessHoldingEntryAttempts;
        var alert = holdingAttempt % ReadinessHoldingAlertInterval == 0
            ? Alert(
                lease,
                EconomicOperatorAlertCodes.ReadinessHoldingStalled,
                OperatorAlertSeverity.Warning,
                reasonCode,
                $"Held because {ReadinessSubject(reasonCode)} has not finished account setup. Nothing was "
                + "paid out and the amount is still reserved.",
                $"Help {ReadinessSubject(reasonCode)} finish setup within {ReadinessHoldingWindowDays} days of "
                + "the first hold, or the reserved amount is returned to its source automatically.")
            : null;

        _logger?.LogWarning(
            "Economic settlement {SettlementId} (outbox {OutboxId}) is HOLDING on readiness after "
            + "{AttemptCount} attempts; readiness reason {ReasonCode}. Nothing was dispatched and the "
            + "reservation stands. It will be re-checked every {HoldingBackoffHours} hours and the "
            + "amount is owed back after attempt {RefundAttempt}.",
            lease.SettlementId,
            lease.OutboxId,
            lease.AttemptCount,
            reasonCode,
            ReadinessHoldingBackoff.TotalHours,
            ReadinessRefundAttempts);

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                now.Add(ReadinessHoldingBackoff),
                null,
                null,
                null,
                reasonCode,
                HoldingDetail(reasonCode)),
            alert);
    }

    /// <summary>
    /// A decision that ends the settlement as unpaid, built (and logged) only once its proof is known. Until
    /// then it carries a non-terminal retry, so nothing can write the close without going through the proof.
    /// </summary>
    private static EconomicOutboxDecision Closes(
        DateTime now,
        string? openFailureCode,
        Func<string, EconomicOutboxDecision> close)
        => new(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                now.Add(CeilingBackoff),
                null,
                null,
                null,
                openFailureCode,
                "This settlement is due to be closed; the node's receipt is read first."),
            null,
            new ClosingDecision(openFailureCode, close));

    /// <summary>Ends the hold: no further attempt, and the reserved amount is owed back to its source.</summary>
    /// <remarks>
    /// The finalization carries <see cref="EconomicSettlementFailureCodes.DispatchAttemptCeiling"/>
    /// because that is the only code <c>EconomicOutboxLeaseStore</c> makes terminal on the dispatch
    /// path, and a refund-due row must stop being retried. The refund itself is a node operation
    /// this seam cannot perform, so it is handed over as a critical alert naming the destination -
    /// never as a silent FAILED row that looks settled. Reached only through <see cref="DecideClosingAsync"/>,
    /// whose <paramref name="proof"/> says why nothing was paid.
    /// </remarks>
    private EconomicOutboxDecision ReadinessRefundDue(
        EconomicOutboxLease lease,
        DateTime now,
        string reasonCode,
        string proof)
    {
        _logger?.LogCritical(
            "Economic settlement {SettlementId} (outbox {OutboxId}, kind {SettlementKind}) was held on "
            + "readiness for {AttemptCount} attempts, past its {HoldingWindowDays}-day window, and will "
            + "not be dispatched. Nothing was paid out: {Proof} Required: {RefundAction} "
            + "Readiness reason: {ReasonCode}.",
            lease.SettlementId,
            lease.OutboxId,
            lease.SettlementKind,
            lease.AttemptCount,
            ReadinessHoldingWindowDays,
            proof,
            RefundAction(lease.SettlementKind),
            reasonCode);

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                now,
                null,
                null,
                null,
                EconomicSettlementFailureCodes.DispatchAttemptCeiling,
                $"{ReadinessSubject(reasonCode)} did not finish account setup within "
                + $"{ReadinessHoldingWindowDays} days, so this payment was closed without being made. "
                + "Nothing was paid out and the reserved amount goes back to where it was reserved from."),
            Alert(
                lease,
                EconomicOperatorAlertCodes.ReadinessHoldingRefundDue,
                OperatorAlertSeverity.Critical,
                reasonCode,
                $"The {ReadinessHoldingWindowDays}-day holding window elapsed with {ReadinessSubject(reasonCode)} "
                + $"still not set up. Nothing was paid out: {proof}",
                RefundAction(lease.SettlementKind)));
    }

    /// <summary>Parks a never-landed settlement and tells an operator; the lease store makes it terminal.</summary>
    /// <remarks>Reached only through <see cref="DecideClosingAsync"/>, whose <paramref name="proof"/> says why nothing was settled.</remarks>
    private EconomicOutboxDecision DispatchCeiling(
        EconomicOutboxLease lease,
        DateTime now,
        AzoaSettlementGatewayResult result,
        string proof)
    {
        _logger?.LogCritical(
            "Economic settlement {SettlementId} (outbox {OutboxId}) reached the dispatch attempt ceiling of "
            + "{MaximumAttempts} after {AttemptCount} attempts and was parked as failed. Nothing was settled: {Proof} "
            + "Last gateway code: {GatewayCode}.",
            lease.SettlementId,
            lease.OutboxId,
            _maximumAttempts,
            lease.AttemptCount,
            proof,
            result.Code ?? "AZOA_RETRY");

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Retry,
                now,
                null,
                null,
                null,
                EconomicSettlementFailureCodes.DispatchAttemptCeiling,
                "Dispatch was refused on every attempt up to the ceiling. No value was settled; an operator must "
                + "resolve the refusal before this settlement can be reopened."),
            Alert(
                lease,
                EconomicOperatorAlertCodes.DispatchAttemptCeiling,
                OperatorAlertSeverity.Critical,
                result.Code ?? "AZOA_RETRY",
                $"Dispatch was refused on every attempt up to the ceiling. Nothing was settled: {proof}",
                "Resolve the refusal and reopen the settlement, or close it out: "
                + RefundAction(lease.SettlementKind)));
    }

    /// <summary>Keeps an ambiguous settlement claimable but slow, and tells an operator every cycle.</summary>
    private EconomicOutboxDecision ReconciliationCeiling(
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

        return new EconomicOutboxDecision(
            new EconomicOutboxFinalization(
                EconomicOutboxRecordedOutcome.Unknown,
                now.Add(CeilingBackoff),
                null,
                null,
                null,
                EconomicSettlementFailureCodes.ReconciliationAttentionRequired,
                "Reconciliation passed its attempt ceiling without a readable node outcome. This settlement stays "
                + "open on purpose - value may have moved - and needs an operator."),
            Alert(
                lease,
                EconomicOperatorAlertCodes.ReconciliationAttentionRequired,
                OperatorAlertSeverity.Critical,
                result.Code ?? "AZOA_AMBIGUOUS_RESULT",
                "Reconciliation ran out of attempts and the economic effect is still unknown. This was NOT failed.",
                "Reconcile this settlement against the node by hand before anybody is told it succeeded or failed; "
                + "value may already have moved."));
    }

    private static TimeSpan RetryDelay(int attemptCount)
        => TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, Math.Min(6, Math.Max(0, attemptCount - 1)))));

    /// <summary>Builds an alert from the lease, so an operator never joins back to the row to act.</summary>
    private static EconomicOperatorAlert Alert(
        EconomicOutboxLease lease,
        string alertCode,
        OperatorAlertSeverity severity,
        string? failureCode,
        string summary,
        string requiredAction)
        => new(
            alertCode,
            severity,
            lease.OutboxId,
            lease.SettlementId,
            lease.SettlementKind,
            lease.Request.BeneficiaryUserId,
            lease.Request.AssetCode,
            lease.Request.Amount.BaseUnits,
            lease.Request.Amount.Scale,
            lease.AttemptCount,
            failureCode,
            summary,
            requiredAction);

    /// <summary>Names which side of the settlement is not ready, in words a person can act on.</summary>
    private static string ReadinessSubject(string reasonCode)
        => reasonCode is EconomicSettlementReadinessCodes.ProjectOwnerNotReady
            or EconomicSettlementReadinessCodes.ProjectOwnerUnavailable
            ? "the project owner"
            : "the recipient";

    /// <summary>Where a reservation goes back to when nobody was ever paid, per settlement kind.</summary>
    private static string RefundAction(EconomicSettlementKind kind)
        => kind switch
        {
            EconomicSettlementKind.TASK_REWARD or EconomicSettlementKind.HOLD_RELEASE
                => "Return the reserved amount to the project's contributor pool and tell both sides why; "
                    + "the work reward was never paid out.",
            // An exchange debits the sponsor only when the node admits it, and the node holds none under this
            // key: there is nothing to give back, and "returning" funds would pay the sponsor out of nothing.
            EconomicSettlementKind.EXCHANGE
                => "Tell the sponsor this sponsorship did not go through. They were never debited, so do NOT "
                    + "credit or return anything.",
            EconomicSettlementKind.FUNDING_ALLOCATION
                => "Return the sponsor's funds and tell them why; the project was never credited.",
            EconomicSettlementKind.ESCROW_REFUND
                => "Return the reserved amount to whoever funded it; the refund was never delivered.",
            _ => "Return the reserved amount to where it was reserved from and tell the people involved why; "
                + "nothing was paid out.",
        };

    /// <summary>Consumer-safe copy for a held settlement: no jargon, no blame, no missing money.</summary>
    private string HoldingDetail(string reasonCode)
        => $"This is on hold because {ReadinessSubject(reasonCode)} has not finished account setup. "
            + "Nothing was paid out and nothing was lost - the amount stays reserved while setup is completed. "
            + $"If setup is still unfinished after {ReadinessHoldingWindowDays} days, the reserved amount goes "
            + "back to where it came from.";
}

namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;

/// <summary>How loudly an economic alert asks a person to look at it.</summary>
public enum OperatorAlertSeverity
{
    /// <summary>Value is parked and waiting on a person, but the machine has not given up on it.</summary>
    Warning,

    /// <summary>No further machine attempt will move this value; a person owns it now.</summary>
    Critical,
}

/// <summary>Stable alert codes an operator rule matches on; each names exactly one economic condition.</summary>
/// <remarks>
/// These are the alerting counterpart of <see cref="EconomicSettlementFailureCodes"/>: the failure
/// code decides the outbox row's next state, this code decides who gets woken. They are separate on
/// purpose - a row can sit in one state while crossing several alert thresholds. See
/// <c>Services/Implementations/AGENTS.md</c>, "Reward dispatch hygiene".
/// </remarks>
public static class EconomicOperatorAlertCodes
{
    /// <summary>A settlement is held on someone's incomplete setup past the point ordinary retrying helps.</summary>
    public const string ReadinessHoldingStalled = "AZOA_SETTLEMENT_READINESS_HOLDING_STALLED";

    /// <summary>The holding window elapsed; the reserved amount must go back where it was reserved from.</summary>
    public const string ReadinessHoldingRefundDue = "AZOA_SETTLEMENT_READINESS_REFUND_DUE";

    /// <summary>Dispatch was refused to its ceiling; nothing settled and nothing will without a person.</summary>
    public const string DispatchAttemptCeiling = "AZOA_SETTLEMENT_DISPATCH_CEILING";

    /// <summary>Reconciliation ran out of attempts while the economic effect is still unreadable.</summary>
    public const string ReconciliationAttentionRequired = "AZOA_SETTLEMENT_RECONCILIATION_ATTENTION";
}

/// <summary>One economic condition a person must resolve, carrying everything needed to resolve it.</summary>
/// <remarks>
/// Every field is read off the outbox lease that produced the alert, so an operator never has to
/// join back to the row to know what is stuck, whose it is, how much it is, or what to do about it.
/// <see cref="RequiredAction"/> is mandatory: an alert that reports a condition without naming the
/// action is a log line, and this codebase has already shipped one control that was only a log line.
/// </remarks>
public sealed record EconomicOperatorAlert(
    string AlertCode,
    OperatorAlertSeverity Severity,
    string OutboxId,
    string SettlementId,
    EconomicSettlementKind SettlementKind,
    string BeneficiaryUserId,
    string AssetCode,
    string AmountBaseUnits,
    int AmountScale,
    int AttemptCount,
    string? FailureCode,
    string Summary,
    string RequiredAction);

/// <summary>Delivers economic conditions that no further automated attempt can clear to a person.</summary>
public interface IOperatorAlertService
{
    /// <summary>Raises one alert; implementations never throw into the caller's economic path.</summary>
    Task RaiseAsync(EconomicOperatorAlert alert, CancellationToken ct = default);
}

/// <summary>Default operator channel: one structured critical/warning log line per alert.</summary>
/// <remarks>
/// Logging is the one channel every deployment already ships, so it is the default rather than a
/// no-op - a silently discarded alert about parked value is the failure this class exists to avoid.
/// A deployment with a pager registers its own <see cref="IOperatorAlertService"/> and this one
/// steps aside. The named properties are the alert contract: an alerting rule matches on
/// <c>AlertCode</c>, so renaming one is a breaking change to somebody's pager.
/// </remarks>
public sealed class LoggingOperatorAlertService : IOperatorAlertService
{
    private readonly ILogger? _logger;

    public LoggingOperatorAlertService(ILogger<LoggingOperatorAlertService>? logger = null)
    {
        _logger = logger;
    }

    private LoggingOperatorAlertService(ILogger? logger)
    {
        _logger = logger;
    }

    /// <summary>Builds an alert channel over a logger a caller already holds.</summary>
    public static LoggingOperatorAlertService For(ILogger? logger) => new(logger);

    /// <inheritdoc/>
    public Task RaiseAsync(EconomicOperatorAlert alert, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var level = alert.Severity == OperatorAlertSeverity.Critical
            ? LogLevel.Critical
            : LogLevel.Warning;
        _logger?.Log(
            level,
            "OPERATOR ALERT {AlertCode}: {Summary} Settlement {SettlementId} (outbox {OutboxId}, kind "
            + "{SettlementKind}) for beneficiary {BeneficiaryUserId}, {AmountBaseUnits} base units of "
            + "{AssetCode} at scale {AmountScale}, after {AttemptCount} attempts, last failure code "
            + "{FailureCode}. Required action: {RequiredAction}",
            alert.AlertCode,
            alert.Summary,
            alert.SettlementId,
            alert.OutboxId,
            alert.SettlementKind,
            alert.BeneficiaryUserId,
            alert.AmountBaseUnits,
            alert.AssetCode,
            alert.AmountScale,
            alert.AttemptCount,
            alert.FailureCode ?? "none",
            alert.RequiredAction);

        return Task.CompletedTask;
    }
}

namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;

public enum EconomicOutboxClaimKind
{
    Dispatch,
    Reconciliation,
}

public enum AzoaSettlementGatewayOutcome
{
    Accepted,
    Unknown,
    Retry,
}

public enum EconomicOutboxRecordedOutcome
{
    NoWork,
    Accepted,
    Unknown,
    Retry,
    StaleLease,
}

/// <summary>Immutable local settlement data delivered at the AZOA boundary.</summary>
public sealed record AzoaSettlementRequest(
    string SettlementId,
    string IdempotencyKey,
    string BeneficiaryUserId,
    string AssetCode,
    FixedScaleAmount Amount,
    string? TermsSnapshot);

/// <summary>One CAS lease plus the immutable settlement request it protects.</summary>
public sealed record EconomicOutboxLease(
    string OutboxId,
    string SettlementId,
    string LeaseToken,
    int AttemptCount,
    DateTime LeaseExpiresAt,
    EconomicSettlementStatus SettlementStatus,
    int SettlementVersion,
    EconomicOutboxClaimKind ClaimKind,
    AzoaSettlementRequest Request);

/// <summary>Typed response that distinguishes accepted, ambiguous, and safely retryable gateway outcomes.</summary>
public sealed record AzoaSettlementGatewayResult(
    AzoaSettlementGatewayOutcome Outcome,
    string? OperationId,
    string? Receipt,
    bool Replayed,
    string? Code,
    string? Detail)
{
    public static AzoaSettlementGatewayResult Accepted(string operationId, string? receipt = null, bool replayed = false)
        => new(AzoaSettlementGatewayOutcome.Accepted, operationId, receipt, replayed, null, null);

    public static AzoaSettlementGatewayResult Unknown(string code, string? detail = null)
        => new(AzoaSettlementGatewayOutcome.Unknown, null, null, false, code, detail);

    public static AzoaSettlementGatewayResult Retry(string code, string? detail = null)
        => new(AzoaSettlementGatewayOutcome.Retry, null, null, false, code, detail);
}

/// <summary>State to write after a current lease receives a typed gateway outcome.</summary>
public sealed record EconomicOutboxFinalization(
    EconomicOutboxRecordedOutcome Outcome,
    DateTime AvailableAt,
    string? OperationId,
    string? Receipt,
    bool? Replayed,
    string? FailureCode,
    string? FailureDetail);

/// <summary>Observable result of a single worker attempt; it never represents completed economic value.</summary>
public sealed record EconomicOutboxDispatchRun(
    EconomicOutboxRecordedOutcome Outcome,
    string? OutboxId = null,
    string? SettlementId = null);

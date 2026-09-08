namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record TaskEscrowDto
{
    public string Id { get; init; }
    public string TaskId { get; init; }
    public string FunderId { get; init; }
    public string ShareId { get; init; }
    public decimal Amount { get; init; }
    public EscrowStatus Status { get; init; }
    public string? DisputeReason { get; init; }
    public string? DisputeDescription { get; init; }
    public string? DisputedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? FundedAt { get; init; }
    public DateTime? ReleasedAt { get; init; }
    public DateTime? RefundedAt { get; init; }
    public DateTime? DisputedAt { get; init; }
}

public record CreateTaskEscrowDto
{
    public required string TaskId { get; init; }
    public required string FunderId { get; init; }
    public required string ShareId { get; init; }
    public required decimal Amount { get; init; }
}

/// <summary>
/// Nothing the funder types in: release evidence is the hold's own state on the node, read
/// back after the call, never a chain hash a form field asks a human to transcribe.
/// </summary>
public record ReleaseEscrowDto
{
    public required string ReleasedByUserId { get; init; }
}

public record DisputeEscrowDto
{
    public required string Reason { get; init; }
    public required string Description { get; init; }
    public required string DisputedByUserId { get; init; }
}

/// <summary>A free-text reason is an audit note, not a chain artifact, so it stays optional.</summary>
public record RefundEscrowDto
{
    public required string RefundedByUserId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// An administrator's adjudication of a disputed escrow. <see cref="Outcome"/> is
/// <c>"REFUND"</c> (return the held units to the contributor partition) or <c>"REJECT"</c>
/// (dismiss the dispute and restore FUNDED).
/// </summary>
public record ResolveDisputeDto
{
    public required string Outcome { get; init; }
    public required string ResolvedByUserId { get; init; }
}

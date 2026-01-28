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
    public string? TxHashFund { get; init; }
    public string? TxHashRelease { get; init; }
    public string? TxHashRefund { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? FundedAt { get; init; }
    public DateTime? ReleasedAt { get; init; }
    public DateTime? RefundedAt { get; init; }
}

public record CreateTaskEscrowDto
{
    public required string TaskId { get; init; }
    public required string FunderId { get; init; }
    public required string ShareId { get; init; }
    public required decimal Amount { get; init; }
    public string? TxHashFund { get; init; }
}

public record ReleaseEscrowDto
{
    public string? TxHash { get; init; }
}

public record RefundEscrowDto
{
    public string? TxHash { get; init; }
}

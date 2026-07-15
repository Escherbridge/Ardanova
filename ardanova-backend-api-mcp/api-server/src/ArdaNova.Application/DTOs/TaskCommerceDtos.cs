namespace ArdaNova.Application.DTOs;

/// <summary>Server-owned result of accepting a bid into the local commerce workflow.</summary>
public record TaskCommerceAcceptanceDto
{
    public required string BidId { get; init; }
    public required string TaskId { get; init; }
    public required string AgreementId { get; init; }
    public required string CommerceUrl { get; init; }
}

/// <summary>Actor-authorized, read-only local state for a task commerce agreement.</summary>
public record TaskCommerceViewDto
{
    public required string TaskId { get; init; }
    public required string AgreementId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string AssetCode { get; init; }
    public required decimal AwardAmount { get; init; }
    public required int Scale { get; init; }
    public required string AgreementStatus { get; init; }
    public required string EscrowStatus { get; init; }
}

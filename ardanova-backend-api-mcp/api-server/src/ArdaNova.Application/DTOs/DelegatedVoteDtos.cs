namespace ArdaNova.Application.DTOs;

public record DelegatedVoteDto
{
    public string Id { get; init; }
    public string ProjectId { get; init; }
    public string DelegatorId { get; init; }
    public string DelegateeId { get; init; }
    public string ShareId { get; init; }
    public decimal Amount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public record CreateDelegatedVoteDto
{
    public required string ProjectId { get; init; }
    public required string DelegatorId { get; init; }
    public required string DelegateeId { get; init; }
    public required string ShareId { get; init; }
    public required decimal Amount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record UpdateDelegatedVoteDto
{
    public decimal? Amount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

namespace ArdaNova.Application.DTOs;

public record DelegatedVoteDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid DelegatorId { get; init; }
    public Guid DelegateeId { get; init; }
    public Guid TokenId { get; init; }
    public decimal Amount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public record CreateDelegatedVoteDto
{
    public required Guid ProjectId { get; init; }
    public required Guid DelegatorId { get; init; }
    public required Guid DelegateeId { get; init; }
    public required Guid TokenId { get; init; }
    public required decimal Amount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record UpdateDelegatedVoteDto
{
    public decimal? Amount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

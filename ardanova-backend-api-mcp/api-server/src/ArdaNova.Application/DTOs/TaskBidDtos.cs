namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record TaskBidDto
{
    public string Id { get; init; } = null!;
    public string TaskId { get; init; } = null!;
    public string GuildId { get; init; } = null!;
    public decimal ProposedAmount { get; init; }
    public string Proposal { get; init; } = null!;
    public int EstimatedHours { get; init; }
    public BidStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public GuildSummaryDto? Guild { get; init; }
    public TaskSummaryDto? Task { get; init; }
}

public record CreateTaskBidDto
{
    public required string TaskId { get; init; }
    public required string GuildId { get; init; }
    public required decimal ProposedAmount { get; init; }
    public required string Proposal { get; init; }
    public required int EstimatedHours { get; init; }
}

public record UpdateTaskBidDto
{
    public decimal? ProposedAmount { get; init; }
    public string? Proposal { get; init; }
    public int? EstimatedHours { get; init; }
}

public record ReviewTaskBidDto
{
    public required BidStatus Status { get; init; }
}

public record GuildSummaryDto
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Image { get; init; }
}

public record TaskSummaryDto
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
}

namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record OpportunityBidDto
{
    public string Id { get; init; } = null!;
    public string OpportunityId { get; init; } = null!;
    public string BidderId { get; init; } = null!;
    public string? GuildId { get; init; }
    public decimal? ProposedAmount { get; init; }
    public string Proposal { get; init; } = null!;
    public int? EstimatedHours { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
    public BidStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public OpportunityBidBidderDto? Bidder { get; init; }
    public OpportunityBidGuildDto? Guild { get; init; }
}

public record OpportunityBidBidderDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
    public string? Email { get; init; }
}

public record OpportunityBidGuildDto
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Logo { get; init; }
    public string Slug { get; init; } = null!;
}

public record CreateOpportunityBidDto
{
    public required string OpportunityId { get; init; }
    public required string BidderId { get; init; }
    public string? GuildId { get; init; }
    public decimal? ProposedAmount { get; init; }
    public required string Proposal { get; init; }
    public int? EstimatedHours { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
}

public record UpdateOpportunityBidDto
{
    public decimal? ProposedAmount { get; init; }
    public string? Proposal { get; init; }
    public int? EstimatedHours { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
}

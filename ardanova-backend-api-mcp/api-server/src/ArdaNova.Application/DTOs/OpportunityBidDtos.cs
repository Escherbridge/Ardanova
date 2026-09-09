namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record OpportunityBidDto
{
    public string Id { get; init; } = null!;
    public string OpportunityId { get; init; } = null!;
    public string BidderId { get; init; } = null!;
    public string? GuildId { get; init; }
    public decimal? ProposedAmount { get; init; }

    /// <summary>
    /// The guild's share of <see cref="ProposedAmount"/>, in whole units; the remainder is the
    /// bidder's own. Null on every bid that is not a guild bid.
    /// </summary>
    public decimal? GuildAwardSplit { get; init; }

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

    /// <summary>
    /// How much of <see cref="ProposedAmount"/> the guild is owed, in whole units. Required on a
    /// PAID guild bid: an award is set aside per beneficiary, so a bid that does not say how it
    /// splits cannot be held for anyone. Refused unless it names a guild and leaves the bidder a
    /// whole positive share of their own.
    /// </summary>
    public decimal? GuildAwardSplit { get; init; }

    public required string Proposal { get; init; }
    public int? EstimatedHours { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
}

public record UpdateOpportunityBidDto
{
    public decimal? ProposedAmount { get; init; }

    /// <summary>
    /// A revised guild share. Null leaves the bid's existing split alone; the revised bid is
    /// re-checked against the same rule it was created under.
    /// </summary>
    public decimal? GuildAwardSplit { get; init; }

    public string? Proposal { get; init; }
    public int? EstimatedHours { get; init; }
    public string? Timeline { get; init; }
    public string? Deliverables { get; init; }
}

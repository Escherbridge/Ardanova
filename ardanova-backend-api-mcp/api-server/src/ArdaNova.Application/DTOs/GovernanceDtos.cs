namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ProposalDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string CreatorId { get; init; } = null!;
    public ProposalType Type { get; init; }
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Options { get; init; } = null!;
    public int Quorum { get; init; }
    public int Threshold { get; init; }
    public ProposalStatus Status { get; init; }
    public DateTime? VotingStart { get; init; }
    public DateTime? VotingEnd { get; init; }
    public int? ExecutionDelay { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public ProposalCreatorDto? Creator { get; init; }
    public ProposalProjectDto? Project { get; init; }
    public int VotesCount { get; init; }
    public decimal TotalVotingPower { get; init; }
    /// <summary>Confirmed members of the project who may vote - the electorate quorum is measured against.</summary>
    public int EligibleMembers { get; init; }
    /// <summary>Ballots needed for quorum, as the tally computes it (never fewer than two).</summary>
    public int VotesNeededForQuorum { get; init; }
    public bool QuorumReached { get; init; }
    /// <summary>Ballots cast per declared choice number.</summary>
    public Dictionary<int, int> VotesByChoice { get; init; } = new();
}

public record ProposalCreatorDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record ProposalProjectDto
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
}

public record CreateProposalDto
{
    public required string ProjectId { get; init; }
    public required string CreatorId { get; init; }
    public required ProposalType Type { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Options { get; init; }
    public int Quorum { get; init; } = 50;
    public int Threshold { get; init; } = 51;
    public DateTime? VotingStart { get; init; }
    public DateTime? VotingEnd { get; init; }
    public int? ExecutionDelay { get; init; }
}

public record UpdateProposalDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Options { get; init; }
    public int? Quorum { get; init; }
    public int? Threshold { get; init; }
    public DateTime? VotingStart { get; init; }
    public DateTime? VotingEnd { get; init; }
    public int? ExecutionDelay { get; init; }
}

public record VoteDto
{
    public string Id { get; init; } = null!;
    public string ProposalId { get; init; } = null!;
    public string VoterId { get; init; } = null!;
    public int Choice { get; init; }
    public decimal Weight { get; init; }
    public string? Reason { get; init; }
    public string? TxHash { get; init; }
    public DateTime CreatedAt { get; init; }
    public VoteUserDto? Voter { get; init; }
}

public record VoteUserDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record CastVoteDto
{
    public required string VoterId { get; init; }
    public required int Choice { get; init; }
    public decimal? Weight { get; init; }
    public string? Reason { get; init; }
}

public record ProposalVoteSummaryDto
{
    public string ProposalId { get; init; } = null!;
    public int TotalVotes { get; init; }
    public decimal TotalVotingPower { get; init; }
    public Dictionary<int, VoteOptionSummary> OptionSummaries { get; init; } = new();
    public bool QuorumReached { get; init; }
    public int? WinningOption { get; init; }
    /// <summary>Confirmed members who may vote on this proposal.</summary>
    public int EligibleMembers { get; init; }
    /// <summary>Ballots needed for quorum, computed by the same rule that resolves the proposal.</summary>
    public int VotesNeededForQuorum { get; init; }
}

/// <summary>One member's standing on one proposal, so the client offers a ballot only to someone the server will accept it from.</summary>
public record BallotStatusDto
{
    public bool VotingOpen { get; init; }
    public bool CanVote { get; init; }
    /// <summary>Why <see cref="CanVote"/> is false, in the words CastVote would refuse with.</summary>
    public string? Reason { get; init; }
    /// <summary>The choice this member already cast, if any.</summary>
    public int? Choice { get; init; }
    public DateTime? VotedAt { get; init; }
}

public record VoteOptionSummary
{
    public int Choice { get; init; }
    public int VoteCount { get; init; }
    public decimal VotingPower { get; init; }
    public decimal Percentage { get; init; }
}

public record ProposalCommentDto
{
    public string Id { get; init; } = null!;
    public string ProposalId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? ParentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public ProposalCommentUserDto? User { get; init; }
    public List<ProposalCommentDto>? Replies { get; init; }
}

public record ProposalCommentUserDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

/// <summary>A comment request body. ProposalId and UserId come from the route and the actor, never the body, so a body may omit them; see Services/AGENTS.md §Governance.</summary>
public record CreateProposalCommentDto
{
    public string ProposalId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public required string Content { get; init; }
    public string? ParentId { get; init; }
}

public record UpdateProposalCommentDto
{
    public required string Content { get; init; }
}

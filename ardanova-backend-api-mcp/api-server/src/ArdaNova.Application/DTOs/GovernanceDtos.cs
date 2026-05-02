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

public record CreateProposalCommentDto
{
    public required string ProposalId { get; init; }
    public required string UserId { get; init; }
    public required string Content { get; init; }
    public string? ParentId { get; init; }
}

public record UpdateProposalCommentDto
{
    public required string Content { get; init; }
}

namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// Leaderboard DTOs
public record LeaderboardDto
{
    public string Id { get; init; } = string.Empty;
    public LeaderboardPeriod Period { get; init; }
    public LeaderboardCategory Category { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateLeaderboardDto
{
    public required LeaderboardPeriod Period { get; init; }
    public required LeaderboardCategory Category { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
}

public record LeaderboardEntryDto
{
    public string Id { get; init; } = string.Empty;
    public string LeaderboardId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public int Rank { get; init; }
    public int Score { get; init; }
    public string? Metadata { get; init; }
}

public record CreateLeaderboardEntryDto
{
    public required string LeaderboardId { get; init; }
    public required string UserId { get; init; }
    public required int Score { get; init; }
    public string? Metadata { get; init; }
}

public record UpdateLeaderboardEntryDto
{
    public int? Score { get; init; }
    public int? Rank { get; init; }
    public string? Metadata { get; init; }
}

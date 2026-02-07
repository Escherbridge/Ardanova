namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// XPEvent DTOs
public record XPEventDto
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public XPEventType EventType { get; init; }
    public int Amount { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? SourceId { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AwardXPDto
{
    public required string UserId { get; init; }
    public required XPEventType EventType { get; init; }
    public required int Amount { get; init; }
    public required string Source { get; init; }
    public string? SourceId { get; init; }
    public string? Metadata { get; init; }
}

public record XPSummaryDto
{
    public string UserId { get; init; } = string.Empty;
    public int TotalXP { get; init; }
    public int Level { get; init; }
    public UserTier Tier { get; init; }
    public int XPForCurrentLevel { get; init; }
    public int XPForNextLevel { get; init; }
    public double ProgressPercent { get; init; }
}

public record LevelInfoDto
{
    public int Level { get; init; }
    public int XPRequired { get; init; }
}

public record XPRewardsConfigDto
{
    public Dictionary<string, int> Rewards { get; init; } = new();
}

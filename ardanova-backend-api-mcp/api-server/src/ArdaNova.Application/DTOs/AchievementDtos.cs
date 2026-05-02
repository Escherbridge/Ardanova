namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// Achievement DTOs
public record AchievementDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AchievementCategory Category { get; init; }
    public string Criteria { get; init; } = string.Empty;
    public int XpReward { get; init; }
    public decimal? EquityReward { get; init; }
    public AchievementRarity Rarity { get; init; }
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateAchievementDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public AchievementCategory Category { get; init; }
    public required string Criteria { get; init; }
    public int XpReward { get; init; }
    public decimal? EquityReward { get; init; }
    public AchievementRarity Rarity { get; init; } = AchievementRarity.COMMON;
    public string? Icon { get; init; }
}

public record UpdateAchievementDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Criteria { get; init; }
    public int? XpReward { get; init; }
    public decimal? EquityReward { get; init; }
    public AchievementRarity? Rarity { get; init; }
    public string? Icon { get; init; }
    public bool? IsActive { get; init; }
}

public record UserAchievementDto
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string AchievementId { get; init; } = string.Empty;
    public int Progress { get; init; }
    public DateTime? EarnedAt { get; init; }
}

public record UpdateProgressDto
{
    public required int Progress { get; init; }
}

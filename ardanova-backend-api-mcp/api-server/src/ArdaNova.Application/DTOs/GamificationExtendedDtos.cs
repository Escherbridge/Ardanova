namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// UserStreak DTOs
public record UserStreakDto
{
    public string Id { get; init; }
    public string UserId { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public DateTime? LastActivityDate { get; init; }
    public StreakType StreakType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateUserStreakDto
{
    public required string UserId { get; init; }
    public StreakType StreakType { get; init; } = StreakType.DAILY_LOGIN;
}

// Referral DTOs
public record ReferralDto
{
    public string Id { get; init; }
    public string ReferrerId { get; init; }
    public string ReferredId { get; init; }
    public string? ReferralCode { get; init; }
    public ReferralStatus Status { get; init; }
    public bool RewardClaimed { get; init; }
    public int? XpRewarded { get; init; }
    public decimal? EquityRewarded { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CreateReferralDto
{
    public required string ReferrerId { get; init; }
    public required string ReferredId { get; init; }
    public string? ReferralCode { get; init; }
}

public record ClaimReferralRewardDto
{
    public required int XpAmount { get; init; }
    public decimal? EquityAmount { get; init; }
}

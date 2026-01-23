namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// UserStreak DTOs
public record UserStreakDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public DateTime? LastActivityDate { get; init; }
    public StreakType StreakType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateUserStreakDto
{
    public required Guid UserId { get; init; }
    public StreakType StreakType { get; init; } = StreakType.DAILY_LOGIN;
}

// Referral DTOs
public record ReferralDto
{
    public Guid Id { get; init; }
    public Guid ReferrerId { get; init; }
    public Guid ReferredId { get; init; }
    public string? ReferralCode { get; init; }
    public ReferralStatus Status { get; init; }
    public bool RewardClaimed { get; init; }
    public int? XpRewarded { get; init; }
    public decimal? TokenRewarded { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CreateReferralDto
{
    public required Guid ReferrerId { get; init; }
    public required Guid ReferredId { get; init; }
    public string? ReferralCode { get; init; }
}

public record ClaimReferralRewardDto
{
    public required int XpAmount { get; init; }
    public decimal? TokenAmount { get; init; }
}

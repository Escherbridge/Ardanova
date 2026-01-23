using System.Text.Json.Serialization;

namespace ArdaNova.Domain.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserTier
{
    BRONZE,
    SILVER,
    GOLD,
    PLATINUM,
    DIAMOND
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VerificationLevel
{
    ANONYMOUS,
    VERIFIED,
    PRO,
    EXPERT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum XPEventType
{
    TASK_COMPLETED,
    PROPOSAL_CREATED,
    PROPOSAL_PASSED,
    VOTE_CAST,
    PROJECT_FUNDED,
    MEMBER_REFERRED,
    ACHIEVEMENT_EARNED,
    STREAK_MAINTAINED,
    LEVEL_UP,
    REVIEW_GIVEN,
    CONTRIBUTION_MADE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AchievementCategory
{
    CONTRIBUTOR,
    COLLABORATOR,
    INVESTOR,
    GOVERNANCE,
    COMMUNITY,
    STREAK,
    MILESTONE,
    GAMING
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AchievementRarity
{
    COMMON,
    UNCOMMON,
    RARE,
    EPIC,
    LEGENDARY
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LeaderboardPeriod
{
    DAILY,
    WEEKLY,
    MONTHLY,
    QUARTERLY,
    ALL_TIME
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LeaderboardCategory
{
    XP,
    TASKS_COMPLETED,
    TOKENS_EARNED,
    PROPOSALS_CREATED,
    VOTES_CAST,
    PROJECTS_FUNDED
}

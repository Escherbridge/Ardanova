namespace ArdaNova.Domain.Models.Enums;

public enum NotificationType
{
    // Task notifications
    TASK_ASSIGNED,
    TASK_COMPLETED,
    TASK_REVIEWED,
    TASK_REVISION_REQUESTED,
    // Proposal/governance notifications
    PROPOSAL_CREATED,
    PROPOSAL_VOTED,
    PROPOSAL_PASSED,
    PROPOSAL_REJECTED,
    PROPOSAL_EXECUTED,
    // Achievement/gamification notifications
    ACHIEVEMENT_EARNED,
    LEVEL_UP,
    STREAK_MILESTONE,
    // Token notifications
    TOKEN_RECEIVED,
    TOKEN_VESTED,
    ESCROW_FUNDED,
    ESCROW_RELEASED,
    // Project notifications
    PROJECT_INVITATION,
    PROJECT_UPDATE,
    PROJECT_FUNDED,
    // Social notifications
    COMMENT_REPLY,
    MENTION,
    FOLLOWER_NEW,
    // System notifications
    SYSTEM_ANNOUNCEMENT,
    SECURITY_ALERT
}

public enum ActivityType
{
    CREATED,
    UPDATED,
    DELETED,
    COMPLETED,
    JOINED,
    LEFT,
    COMMENTED,
    VOTED,
    SUBMITTED,
    REVIEWED,
    FUNDED,
    TRANSFERRED,
    SWAPPED
}

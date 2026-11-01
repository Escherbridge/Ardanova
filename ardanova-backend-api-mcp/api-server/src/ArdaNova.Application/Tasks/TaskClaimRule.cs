using ArdaNova.Domain.Models.Enums;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

namespace ArdaNova.Application.Tasks;

/// <summary>The facts about a task that decide whether it can be claimed.</summary>
public sealed record TaskClaimFacts(
    string? AssignedToId,
    TaskStatus Status,
    EscrowStatus EscrowStatus,
    bool HasAgreement,
    decimal? EquityReward);

/// <summary>Why a viewer can or cannot claim a task, most blocking reason first.</summary>
public enum TaskClaimVerdict
{
    Claimable,
    PaidNeedsReadiness,
    NotAMember,
    AlreadyTaken,
    Completed,
    UnderAgreement,
}

/// <summary>The one claim rule shared by the claim write and the feed's canClaim. See Tasks/AGENTS.md.</summary>
public static class TaskClaimRule
{
    /// <summary>Evaluates whether a viewer may claim the task described by <paramref name="task"/>.</summary>
    public static TaskClaimVerdict Evaluate(TaskClaimFacts task, bool viewerIsProjectMember)
    {
        if (!viewerIsProjectMember) return TaskClaimVerdict.NotAMember;
        if (task.AssignedToId is not null) return TaskClaimVerdict.AlreadyTaken;
        if (task.Status == TaskStatus.COMPLETED) return TaskClaimVerdict.Completed;
        if (task.HasAgreement || task.EscrowStatus != EscrowStatus.NONE) return TaskClaimVerdict.UnderAgreement;
        if (task.EquityReward is > 0m) return TaskClaimVerdict.PaidNeedsReadiness;
        return TaskClaimVerdict.Claimable;
    }

    /// <summary>True only when the card may offer a one-tap claim; paid tasks route through the task page instead.</summary>
    public static bool CanClaimInPlace(TaskClaimFacts task, bool viewerIsProjectMember)
        => Evaluate(task, viewerIsProjectMember) == TaskClaimVerdict.Claimable;
}

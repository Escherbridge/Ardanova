namespace ArdaNova.Application.DTOs;

using ArdaNova.Application.Tasks;
using ArdaNova.Domain.Models.Enums;

public record TaskDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string? PbiId { get; init; }
    public string? FeatureId { get; init; }
    public string? SprintId { get; init; }
    public string? EpicId { get; init; }
    public string? MilestoneId { get; init; }
    public string? GuildId { get; init; }
    public string? OpportunityId { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskType TaskType { get; init; }
    public int? EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public decimal? EquityReward { get; init; }
    public EscrowStatus EscrowStatus { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? AssignedToId { get; init; }
    public TaskUserDto? AssignedTo { get; init; }
    public TaskProjectDto? Project { get; init; }

    /// <summary>An accepted bid's agreement exists for this task; one of <see cref="TaskClaimFacts"/>.</summary>
    public bool HasAgreement { get; init; }

    /// <summary>The task's live HELP_WANTED ask when its audience is everyone; null otherwise. See Implementations/AGENTS.md §Help wanted.</summary>
    public TaskHelpWantedDto? HelpWanted { get; init; }

    /// <summary>The facts <see cref="TaskClaimRule.Evaluate"/> reads, straight off this task.</summary>
    public TaskClaimFacts ToClaimFacts() => new(AssignedToId, Status, EscrowStatus, HasAgreement, EquityReward);
}

/// <summary>Who asked for help on a task, so the task page can offer "Message" without another route.</summary>
public record TaskHelpWantedDto
{
    public string PostId { get; init; } = null!;
    public string AskerId { get; init; } = null!;
    public string? AskerName { get; init; }
}

public record TaskUserDto
{
    public string Id { get; init; } = null!;
    public string? Name { get; init; }
    public string? Image { get; init; }
}

public record TaskProjectDto
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
}

public record CreateTaskDto
{
    public required string ProjectId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.MEDIUM;
    public TaskType TaskType { get; init; } = TaskType.FEATURE;
    public int? EstimatedHours { get; init; }
    public decimal? EquityReward { get; init; }
    public DateTime? DueDate { get; init; }
    public string? AssignedToId { get; init; }
    public string? PbiId { get; init; }
    public string? FeatureId { get; init; }
    public string? SprintId { get; init; }
    public string? EpicId { get; init; }
    public string? MilestoneId { get; init; }
    public string? GuildId { get; init; }
}

public record UpdateTaskDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public TaskType? TaskType { get; init; }
    public int? EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public decimal? EquityReward { get; init; }
    public DateTime? DueDate { get; init; }
}

public record UpdateTaskStatusDto
{
    public required TaskStatus Status { get; init; }
}

/// <summary>Body of PUT /api/tasks/{id}/assign: a project member's user id assigns, null unassigns.</summary>
public record AssignTaskDto
{
    public string? UserId { get; init; }

    /// <summary>The one-tap "Pick up": take this task only while it is open, never over its holder. See Implementations/AGENTS.md §Task assignment.</summary>
    public bool Claim { get; init; }
}

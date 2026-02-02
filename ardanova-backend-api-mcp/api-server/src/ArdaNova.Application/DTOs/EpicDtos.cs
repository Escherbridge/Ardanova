namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record EpicDto
{
    public string Id { get; init; } = null!;
    public string MilestoneId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public EpicStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public decimal? EquityBudget { get; init; }
    public decimal Progress { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? TargetDate { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<ProductBacklogItemDto>? PBIs { get; init; }
}

public record CreateEpicDto
{
    public required string MilestoneId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.MEDIUM;
    public decimal? EquityBudget { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? TargetDate { get; init; }
    public string? AssigneeId { get; init; }
}

public record UpdateEpicDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public EpicStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public decimal? EquityBudget { get; init; }
    public decimal? Progress { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? TargetDate { get; init; }
    public string? AssigneeId { get; init; }
}

public record UpdateEpicPriorityDto
{
    public TaskPriority Priority { get; init; }
}

public record ReorderEpicsDto
{
    public required IReadOnlyList<string> EpicIds { get; init; }
}

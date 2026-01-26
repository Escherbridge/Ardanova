namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record TaskDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string? BacklogItemId { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskType TaskType { get; init; }
    public int? EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public decimal? TokenReward { get; init; }
    public EscrowStatus EscrowStatus { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? AssignedToId { get; init; }
    public TaskUserDto? AssignedTo { get; init; }
    public TaskProjectDto? Project { get; init; }
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
    public decimal? TokenReward { get; init; }
    public DateTime? DueDate { get; init; }
    public string? AssignedToId { get; init; }
    public string? BacklogItemId { get; init; }
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
    public decimal? TokenReward { get; init; }
    public DateTime? DueDate { get; init; }
    public string? AssignedToId { get; init; }
}

public record UpdateTaskStatusDto
{
    public required TaskStatus Status { get; init; }
}

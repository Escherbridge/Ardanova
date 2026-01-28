namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ProductBacklogItemDto
{
    public string Id { get; init; } = null!;
    public string EpicId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public PBIType Type { get; init; }
    public int? StoryPoints { get; init; }
    public PBIStatus Status { get; init; }
    public string? AcceptanceCriteria { get; init; }
    public TaskPriority Priority { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<BacklogItemDto>? BacklogItems { get; init; }
}

public record CreateProductBacklogItemDto
{
    public required string EpicId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public PBIType Type { get; init; } = PBIType.FEATURE;
    public int? StoryPoints { get; init; }
    public string? AcceptanceCriteria { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.MEDIUM;
    public string? AssigneeId { get; init; }
}

public record UpdateProductBacklogItemDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public PBIType? Type { get; init; }
    public int? StoryPoints { get; init; }
    public PBIStatus? Status { get; init; }
    public string? AcceptanceCriteria { get; init; }
    public TaskPriority? Priority { get; init; }
    public string? AssigneeId { get; init; }
}

public record BacklogItemDto
{
    public string Id { get; init; } = null!;
    public string PbiId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public BacklogItemType Type { get; init; }
    public BacklogStatus Status { get; init; }
    public int? Estimate { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<ProjectTaskDto>? Tasks { get; init; }
}

public record CreateBacklogItemDto
{
    public required string PbiId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public BacklogItemType Type { get; init; } = BacklogItemType.TASK;
    public int? Estimate { get; init; }
    public string? AssigneeId { get; init; }
}

public record UpdateBacklogItemDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public BacklogItemType? Type { get; init; }
    public BacklogStatus? Status { get; init; }
    public int? Estimate { get; init; }
    public string? AssigneeId { get; init; }
}

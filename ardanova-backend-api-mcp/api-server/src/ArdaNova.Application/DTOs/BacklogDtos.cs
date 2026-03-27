namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ProductBacklogItemDto
{
    public string Id { get; init; } = null!;
    public string FeatureId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public PBIType Type { get; init; }
    public int? StoryPoints { get; init; }
    public PBIStatus Status { get; init; }
    public string? AcceptanceCriteria { get; init; }
    public Priority Priority { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateProductBacklogItemDto
{
    public required string FeatureId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public PBIType Type { get; init; } = PBIType.FEATURE;
    public int? StoryPoints { get; init; }
    public string? AcceptanceCriteria { get; init; }
    public Priority Priority { get; init; } = Priority.MEDIUM;
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
    public Priority? Priority { get; init; }
    public string? AssigneeId { get; init; }
}

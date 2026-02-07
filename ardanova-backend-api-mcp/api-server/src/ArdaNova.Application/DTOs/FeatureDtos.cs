namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record FeatureDto
{
    public string Id { get; init; } = null!;
    public string SprintId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public FeatureStatus Status { get; init; }
    public Priority Priority { get; init; }
    public int Order { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateFeatureDto
{
    public required string SprintId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public Priority Priority { get; init; } = Priority.MEDIUM;
    public int Order { get; init; }
}

public record UpdateFeatureDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public FeatureStatus? Status { get; init; }
    public Priority? Priority { get; init; }
    public int? Order { get; init; }
    public string? AssigneeId { get; init; }
}

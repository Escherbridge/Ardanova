namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record RoadmapDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Vision { get; init; }
    public RoadmapStatus Status { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<RoadmapPhaseDto>? Phases { get; init; }
}

public record CreateRoadmapDto
{
    public required string ProjectId { get; init; }
    public required string Title { get; init; }
    public string? Vision { get; init; }
    public string? AssigneeId { get; init; }
}

public record UpdateRoadmapDto
{
    public string? Title { get; init; }
    public string? Vision { get; init; }
    public RoadmapStatus? Status { get; init; }
    public string? AssigneeId { get; init; }
}

public record RoadmapPhaseDto
{
    public string Id { get; init; } = null!;
    public string RoadmapId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public PhaseStatus Status { get; init; }
    public int Order { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateRoadmapPhaseDto
{
    public required string RoadmapId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; } = 0;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record UpdateRoadmapPhaseDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public PhaseStatus? Status { get; init; }
    public int? Order { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

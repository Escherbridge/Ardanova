namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record SprintDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Goal { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? EquityBudget { get; init; }
    public decimal? Velocity { get; init; }
    public SprintStatus Status { get; init; }
    public string? AssigneeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<SprintItemDto>? Items { get; init; }
}

public record CreateSprintDto
{
    public required string ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Goal { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? EquityBudget { get; init; }
    public string? AssigneeId { get; init; }
}

public record UpdateSprintDto
{
    public string? Name { get; init; }
    public string? Goal { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? EquityBudget { get; init; }
    public decimal? Velocity { get; init; }
    public SprintStatus? Status { get; init; }
    public string? AssigneeId { get; init; }
}

public record SprintItemDto
{
    public string Id { get; init; } = null!;
    public string SprintId { get; init; } = null!;
    public string TaskId { get; init; } = null!;
    public int Order { get; init; }
    public DateTime AddedAt { get; init; }
    public ProjectTaskDto? Task { get; init; }
}

public record AddSprintItemDto
{
    public required string SprintId { get; init; }
    public required string TaskId { get; init; }
    public int Order { get; init; } = 0;
}

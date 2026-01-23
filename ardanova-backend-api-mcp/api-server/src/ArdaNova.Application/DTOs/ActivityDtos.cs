namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ActivityDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid? ProjectId { get; init; }
    public ActivityType Type { get; init; }
    public string EntityType { get; init; } = null!;
    public string EntityId { get; init; } = null!;
    public string Action { get; init; } = null!;
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateActivityDto
{
    public required Guid UserId { get; init; }
    public Guid? ProjectId { get; init; }
    public required ActivityType Type { get; init; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required string Action { get; init; }
    public string? Metadata { get; init; }
}

namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Activity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ProjectId { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActivityType Type { get; private set; }

    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string? Metadata { get; private set; } // JSON
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;
    public Project? Project { get; private set; }

    private Activity() { }

    public static Activity Create(
        Guid userId,
        ActivityType type,
        string entityType,
        string entityId,
        string action,
        Guid? projectId = null,
        string? metadata = null)
    {
        return new Activity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };
    }
}

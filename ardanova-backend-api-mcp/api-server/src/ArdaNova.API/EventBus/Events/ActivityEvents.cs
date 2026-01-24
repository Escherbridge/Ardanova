using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when an activity is logged.
/// </summary>
public sealed record ActivityLoggedEvent(
    Guid ActivityId,
    Guid? UserId,
    string ActivityType,
    string Description,
    Guid? EntityId,
    string? EntityType
) : DomainEvent
{
    public override string EventType => "activity.logged";
}

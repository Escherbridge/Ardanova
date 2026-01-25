using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when an activity is logged.
/// </summary>
public sealed record ActivityLoggedEvent(
    string ActivityId,
    string? UserId,
    string ActivityType,
    string Description,
    string? EntityId,
    string? EntityType
) : DomainEvent
{
    public override string EventType => "activity.logged";
}

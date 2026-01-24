using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when a notification is created for a user.
/// </summary>
public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    Guid UserId,
    string Type,
    string Title,
    string Message,
    string? ActionUrl
) : DomainEvent
{
    public override string EventType => "notification.created";
}

/// <summary>
/// Event raised when a notification is marked as read.
/// </summary>
public sealed record NotificationReadEvent(
    Guid NotificationId,
    Guid UserId
) : DomainEvent
{
    public override string EventType => "notification.read";
}

/// <summary>
/// Event raised when all notifications for a user are marked as read.
/// </summary>
public sealed record NotificationsMarkedAllReadEvent(
    Guid UserId
) : DomainEvent
{
    public override string EventType => "notification.all_read";
}

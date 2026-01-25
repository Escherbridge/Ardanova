using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when a notification is created for a user.
/// </summary>
public sealed record NotificationCreatedEvent(
    string NotificationId,
    string UserId,
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
    string NotificationId,
    string UserId
) : DomainEvent
{
    public override string EventType => "notification.read";
}

/// <summary>
/// Event raised when all notifications for a user are marked as read.
/// </summary>
public sealed record NotificationsMarkedAllReadEvent(
    string UserId
) : DomainEvent
{
    public override string EventType => "notification.all_read";
}

using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ArdaNova.API.WebSocket.Handlers;

/// <summary>
/// Bridges notification domain events to SignalR clients.
/// </summary>
public class NotificationHubHandler :
    IEventHandler<NotificationCreatedEvent>,
    IEventHandler<NotificationReadEvent>,
    IEventHandler<NotificationsMarkedAllReadEvent>
{
    private readonly IHubContext<ArdaNovaHub, IArdaNovaHubClient> _hubContext;
    private readonly ILogger<NotificationHubHandler> _logger;

    public NotificationHubHandler(
        IHubContext<ArdaNovaHub, IArdaNovaHubClient> hubContext,
        ILogger<NotificationHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(NotificationCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting notification.created event for user {UserId}, notification {NotificationId}",
            @event.UserId,
            @event.NotificationId);

        var payload = new
        {
            notificationId = @event.NotificationId,
            userId = @event.UserId,
            type = @event.Type,
            title = @event.Title,
            message = @event.Message,
            actionUrl = @event.ActionUrl,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the specific user
        await _hubContext.Clients.Group($"user:{@event.UserId}").NotificationReceived(payload);
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(NotificationReadEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting notification.read event for user {UserId}, notification {NotificationId}",
            @event.UserId,
            @event.NotificationId);

        var payload = new
        {
            notificationId = @event.NotificationId,
            userId = @event.UserId,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the specific user (for syncing across devices/tabs)
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(NotificationsMarkedAllReadEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting notification.all_read event for user {UserId}", @event.UserId);

        var payload = new
        {
            userId = @event.UserId,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the specific user (for syncing across devices/tabs)
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }
}

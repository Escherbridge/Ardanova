using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ArdaNova.API.WebSocket.Handlers;

/// <summary>
/// Bridges activity domain events to SignalR clients.
/// </summary>
public class ActivityHubHandler : IEventHandler<ActivityLoggedEvent>
{
    private readonly IHubContext<ArdaNovaHub, IArdaNovaHubClient> _hubContext;
    private readonly ILogger<ActivityHubHandler> _logger;

    public ActivityHubHandler(
        IHubContext<ArdaNovaHub, IArdaNovaHubClient> hubContext,
        ILogger<ActivityHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(ActivityLoggedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting activity.logged event: {ActivityType} for entity {EntityType}:{EntityId}",
            @event.ActivityType,
            @event.EntityType,
            @event.EntityId);

        var payload = new
        {
            activityId = @event.ActivityId,
            userId = @event.UserId,
            activityType = @event.ActivityType,
            description = @event.Description,
            entityId = @event.EntityId,
            entityType = @event.EntityType,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the user who performed the activity (if known)
        if (@event.UserId.HasValue)
        {
            await _hubContext.Clients.Group($"user:{@event.UserId}").ActivityLogged(payload);
        }

        // Broadcast to all for activity feeds
        await _hubContext.Clients.Group("all").ActivityLogged(payload);
        await _hubContext.Clients.Group("all").ReceiveEvent(@event.EventType, payload);
    }
}

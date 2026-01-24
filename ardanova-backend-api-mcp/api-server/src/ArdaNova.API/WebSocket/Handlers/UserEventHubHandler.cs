using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ArdaNova.API.WebSocket.Handlers;

/// <summary>
/// Bridges user domain events to SignalR clients.
/// </summary>
public class UserEventHubHandler :
    IEventHandler<UserCreatedEvent>,
    IEventHandler<UserUpdatedEvent>,
    IEventHandler<UserVerifiedEvent>,
    IEventHandler<UserDeletedEvent>
{
    private readonly IHubContext<ArdaNovaHub, IArdaNovaHubClient> _hubContext;
    private readonly ILogger<UserEventHubHandler> _logger;

    public UserEventHubHandler(
        IHubContext<ArdaNovaHub, IArdaNovaHubClient> hubContext,
        ILogger<UserEventHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting user.created event for user {UserId}", @event.UserId);

        var payload = new
        {
            userId = @event.UserId,
            email = @event.Email,
            name = @event.Name,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Broadcast to all connected clients (for admin dashboards, etc.)
        await _hubContext.Clients.Group("all").UserCreated(payload);
        await _hubContext.Clients.Group("all").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(UserUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting user.updated event for user {UserId}", @event.UserId);

        var payload = new
        {
            userId = @event.UserId,
            name = @event.Name,
            email = @event.Email,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the specific user
        await _hubContext.Clients.Group($"user:{@event.UserId}").UserUpdated(payload);
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(UserVerifiedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting user.verified event for user {UserId}", @event.UserId);

        var payload = new
        {
            userId = @event.UserId,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the specific user
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(UserDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting user.deleted event for user {UserId}", @event.UserId);

        var payload = new
        {
            userId = @event.UserId,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Broadcast to admins
        await _hubContext.Clients.Group("all").ReceiveEvent(@event.EventType, payload);
    }
}

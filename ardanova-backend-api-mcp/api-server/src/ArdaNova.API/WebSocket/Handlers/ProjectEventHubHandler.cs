using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ArdaNova.API.WebSocket.Handlers;

/// <summary>
/// Bridges project domain events to SignalR clients.
/// </summary>
public class ProjectEventHubHandler :
    IEventHandler<ProjectCreatedEvent>,
    IEventHandler<ProjectUpdatedEvent>,
    IEventHandler<ProjectStatusChangedEvent>,
    IEventHandler<ProjectDeletedEvent>,
    IEventHandler<ProjectTaskCompletedEvent>,
    IEventHandler<ProjectMemberAddedEvent>,
    IEventHandler<ProjectMemberRemovedEvent>
{
    private readonly IHubContext<ArdaNovaHub, IArdaNovaHubClient> _hubContext;
    private readonly ILogger<ProjectEventHubHandler> _logger;

    public ProjectEventHubHandler(
        IHubContext<ArdaNovaHub, IArdaNovaHubClient> hubContext,
        ILogger<ProjectEventHubHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(ProjectCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting project.created event for project {ProjectId}", @event.ProjectId);

        var payload = new
        {
            projectId = @event.ProjectId,
            ownerId = @event.OwnerId,
            title = @event.Title,
            slug = @event.Slug,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify the owner
        await _hubContext.Clients.Group($"user:{@event.OwnerId}").ProjectCreated(payload);
        await _hubContext.Clients.Group($"user:{@event.OwnerId}").ReceiveEvent(@event.EventType, payload);

        // Also broadcast to "all" for discovery feeds
        await _hubContext.Clients.Group("all").ProjectCreated(payload);
    }

    public async Task HandleAsync(ProjectUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting project.updated event for project {ProjectId}", @event.ProjectId);

        var payload = new
        {
            projectId = @event.ProjectId,
            title = @event.Title,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify all subscribers of this project
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ProjectUpdated(payload);
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(ProjectStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting project.status_changed event for project {ProjectId}: {OldStatus} -> {NewStatus}",
            @event.ProjectId,
            @event.OldStatus,
            @event.NewStatus);

        var payload = new
        {
            projectId = @event.ProjectId,
            oldStatus = @event.OldStatus,
            newStatus = @event.NewStatus,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify all subscribers of this project
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ProjectStatusChanged(payload);
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(ProjectDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Broadcasting project.deleted event for project {ProjectId}", @event.ProjectId);

        var payload = new
        {
            projectId = @event.ProjectId,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify all subscribers of this project
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(ProjectTaskCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting project.task_completed event for project {ProjectId}, task {TaskId}",
            @event.ProjectId,
            @event.TaskId);

        var payload = new
        {
            projectId = @event.ProjectId,
            taskId = @event.TaskId,
            assigneeId = @event.AssigneeId,
            taskTitle = @event.TaskTitle,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify all subscribers of this project
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").TaskCompleted(payload);
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ReceiveEvent(@event.EventType, payload);

        // Also notify the assignee if present
        if (!string.IsNullOrEmpty(@event.AssigneeId))
        {
            await _hubContext.Clients.Group($"user:{@event.AssigneeId}").TaskCompleted(payload);
        }
    }

    public async Task HandleAsync(ProjectMemberAddedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting project.member_added event for project {ProjectId}, user {UserId}",
            @event.ProjectId,
            @event.UserId);

        var payload = new
        {
            projectId = @event.ProjectId,
            userId = @event.UserId,
            role = @event.Role,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify project subscribers
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ReceiveEvent(@event.EventType, payload);

        // Notify the new member
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }

    public async Task HandleAsync(ProjectMemberRemovedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Broadcasting project.member_removed event for project {ProjectId}, user {UserId}",
            @event.ProjectId,
            @event.UserId);

        var payload = new
        {
            projectId = @event.ProjectId,
            userId = @event.UserId,
            eventId = @event.EventId,
            occurredAt = @event.OccurredAt
        };

        // Notify project subscribers
        await _hubContext.Clients.Group($"project:{@event.ProjectId}").ReceiveEvent(@event.EventType, payload);

        // Notify the removed member
        await _hubContext.Clients.Group($"user:{@event.UserId}").ReceiveEvent(@event.EventType, payload);
    }
}

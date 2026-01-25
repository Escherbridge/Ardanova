using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when a new project is created.
/// </summary>
public sealed record ProjectCreatedEvent(
    string ProjectId,
    string OwnerId,
    string Title,
    string? Slug
) : DomainEvent
{
    public override string EventType => "project.created";
}

/// <summary>
/// Event raised when a project is updated.
/// </summary>
public sealed record ProjectUpdatedEvent(
    string ProjectId,
    string Title
) : DomainEvent
{
    public override string EventType => "project.updated";
}

/// <summary>
/// Event raised when a project's status changes.
/// </summary>
public sealed record ProjectStatusChangedEvent(
    string ProjectId,
    string OldStatus,
    string NewStatus
) : DomainEvent
{
    public override string EventType => "project.status_changed";
}

/// <summary>
/// Event raised when a project is deleted.
/// </summary>
public sealed record ProjectDeletedEvent(
    string ProjectId
) : DomainEvent
{
    public override string EventType => "project.deleted";
}

/// <summary>
/// Event raised when a task within a project is completed.
/// </summary>
public sealed record ProjectTaskCompletedEvent(
    string ProjectId,
    string TaskId,
    string? AssigneeId,
    string TaskTitle
) : DomainEvent
{
    public override string EventType => "project.task_completed";
}

/// <summary>
/// Event raised when a new member joins a project.
/// </summary>
public sealed record ProjectMemberAddedEvent(
    string ProjectId,
    string UserId,
    string Role
) : DomainEvent
{
    public override string EventType => "project.member_added";
}

/// <summary>
/// Event raised when a member leaves a project.
/// </summary>
public sealed record ProjectMemberRemovedEvent(
    string ProjectId,
    string UserId
) : DomainEvent
{
    public override string EventType => "project.member_removed";
}

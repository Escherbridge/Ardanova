using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when a new user is created.
/// </summary>
public sealed record UserCreatedEvent(
    Guid UserId,
    string Email,
    string? Name
) : DomainEvent
{
    public override string EventType => "user.created";
}

/// <summary>
/// Event raised when a user's profile is updated.
/// </summary>
public sealed record UserUpdatedEvent(
    Guid UserId,
    string? Name,
    string? Email
) : DomainEvent
{
    public override string EventType => "user.updated";
}

/// <summary>
/// Event raised when a user is verified.
/// </summary>
public sealed record UserVerifiedEvent(
    Guid UserId
) : DomainEvent
{
    public override string EventType => "user.verified";
}

/// <summary>
/// Event raised when a user is deleted.
/// </summary>
public sealed record UserDeletedEvent(
    Guid UserId
) : DomainEvent
{
    public override string EventType => "user.deleted";
}

namespace ArdaNova.API.EventBus.Abstractions;

/// <summary>
/// Base interface for all domain events in the system.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// The type of event (e.g., "user.created", "project.updated").
    /// Used for routing and filtering events.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base record for domain events providing default implementations.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}

namespace ArdaNova.API.EventBus.Abstractions;

/// <summary>
/// In-process event bus for publishing and subscribing to domain events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers asynchronously.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Subscribes a handler to receive events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to subscribe to.</typeparam>
    /// <param name="handler">The handler action to invoke when events are published.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Subscribes a synchronous handler to receive events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to subscribe to.</typeparam>
    /// <param name="handler">The handler action to invoke when events are published.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent;
}

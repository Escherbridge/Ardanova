namespace ArdaNova.API.EventBus.Abstractions;

/// <summary>
/// Interface for handling specific domain events.
/// Handlers are resolved from DI and invoked when events are published.
/// </summary>
/// <typeparam name="TEvent">The type of domain event this handler processes.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

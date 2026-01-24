using System.Collections.Concurrent;
using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Implementation;

/// <summary>
/// Thread-safe in-process event bus implementation using ConcurrentDictionary.
/// Supports both DI-registered handlers and runtime subscriptions.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _subscriptions = new();

    public InMemoryEventBus(
        IServiceProvider serviceProvider,
        ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        _logger.LogDebug(
            "Publishing event {EventType} with ID {EventId}",
            @event.EventType,
            @event.EventId);

        var eventType = typeof(TEvent);
        var tasks = new List<Task>();

        // Invoke DI-registered handlers
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                tasks.Add(SafeInvokeHandler(handler, @event, cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving handlers for event {EventType}", @event.EventType);
        }

        // Invoke runtime subscriptions
        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            foreach (var subscription in subscriptions)
            {
                switch (subscription)
                {
                    case Func<TEvent, CancellationToken, Task> asyncHandler:
                        tasks.Add(SafeInvokeAsync(asyncHandler, @event, cancellationToken));
                        break;
                    case Action<TEvent> syncHandler:
                        tasks.Add(Task.Run(() => SafeInvokeSync(syncHandler, @event), cancellationToken));
                        break;
                }
            }
        }

        await Task.WhenAll(tasks);

        _logger.LogDebug(
            "Completed publishing event {EventType} to {HandlerCount} handlers",
            @event.EventType,
            tasks.Count);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        var subscriptions = _subscriptions.GetOrAdd(eventType, _ => new ConcurrentBag<object>());
        subscriptions.Add(handler);

        _logger.LogDebug("Added async subscription for event type {EventType}", eventType.Name);

        return new Subscription<TEvent>(this, handler);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        var subscriptions = _subscriptions.GetOrAdd(eventType, _ => new ConcurrentBag<object>());
        subscriptions.Add(handler);

        _logger.LogDebug("Added sync subscription for event type {EventType}", eventType.Name);

        return new Subscription<TEvent>(this, handler);
    }

    private void Unsubscribe<TEvent>(object handler) where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            // ConcurrentBag doesn't support removal, so we rebuild without the handler
            var newBag = new ConcurrentBag<object>(subscriptions.Where(s => !ReferenceEquals(s, handler)));
            _subscriptions.TryUpdate(eventType, newBag, subscriptions);
        }
    }

    private async Task SafeInvokeHandler<TEvent>(IEventHandler<TEvent> handler, TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        try
        {
            await handler.HandleAsync(@event, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in handler {HandlerType} for event {EventType}",
                handler.GetType().Name,
                @event.EventType);
        }
    }

    private async Task SafeInvokeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        try
        {
            await handler(@event, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in async subscription for event {EventType}", @event.EventType);
        }
    }

    private void SafeInvokeSync<TEvent>(Action<TEvent> handler, TEvent @event)
        where TEvent : IDomainEvent
    {
        try
        {
            handler(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sync subscription for event {EventType}", @event.EventType);
        }
    }

    private sealed class Subscription<TEvent> : IDisposable where TEvent : IDomainEvent
    {
        private readonly InMemoryEventBus _eventBus;
        private readonly object _handler;
        private bool _disposed;

        public Subscription(InMemoryEventBus eventBus, object handler)
        {
            _eventBus = eventBus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _eventBus.Unsubscribe<TEvent>(_handler);
        }
    }
}

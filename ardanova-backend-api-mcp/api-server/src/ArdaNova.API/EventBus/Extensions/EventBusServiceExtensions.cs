using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Implementation;

namespace ArdaNova.API.EventBus.Extensions;

/// <summary>
/// Extension methods for registering EventBus services with dependency injection.
/// </summary>
public static class EventBusServiceExtensions
{
    /// <summary>
    /// Adds the EventBus services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        // Register the event bus as a singleton (shared across all requests)
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        return services;
    }

    /// <summary>
    /// Registers an event handler with the DI container.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<IEventHandler<TEvent>, THandler>();
        return services;
    }
}

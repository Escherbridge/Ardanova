using ArdaNova.API.EventBus.Abstractions;
using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Handlers;
using ArdaNova.API.WebSocket.Hubs;

namespace ArdaNova.API.WebSocket.Extensions;

/// <summary>
/// Extension methods for registering WebSocket/SignalR services with dependency injection.
/// </summary>
public static class WebSocketServiceExtensions
{
    /// <summary>
    /// Adds SignalR and WebSocket services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddArdaNovaWebSocket(this IServiceCollection services)
    {
        // Add SignalR with configuration
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
        });

        // Register event handlers that bridge to SignalR
        // User events
        services.AddScoped<IEventHandler<UserCreatedEvent>, UserEventHubHandler>();
        services.AddScoped<IEventHandler<UserUpdatedEvent>, UserEventHubHandler>();
        services.AddScoped<IEventHandler<UserVerifiedEvent>, UserEventHubHandler>();
        services.AddScoped<IEventHandler<UserDeletedEvent>, UserEventHubHandler>();

        // Project events
        services.AddScoped<IEventHandler<ProjectCreatedEvent>, ProjectEventHubHandler>();
        services.AddScoped<IEventHandler<ProjectUpdatedEvent>, ProjectEventHubHandler>();
        services.AddScoped<IEventHandler<ProjectStatusChangedEvent>, ProjectEventHubHandler>();
        services.AddScoped<IEventHandler<ProjectDeletedEvent>, ProjectEventHubHandler>();
        services.AddScoped<IEventHandler<ProjectTaskCompletedEvent>, ProjectEventHubHandler>();
        services.AddScoped<IEventHandler<ProjectMemberAddedEvent>, ProjectEventHubHandler>();
        services.AddScoped<IEventHandler<ProjectMemberRemovedEvent>, ProjectEventHubHandler>();

        // Notification events
        services.AddScoped<IEventHandler<NotificationCreatedEvent>, NotificationHubHandler>();
        services.AddScoped<IEventHandler<NotificationReadEvent>, NotificationHubHandler>();
        services.AddScoped<IEventHandler<NotificationsMarkedAllReadEvent>, NotificationHubHandler>();

        // Activity events
        services.AddScoped<IEventHandler<ActivityLoggedEvent>, ActivityHubHandler>();

        return services;
    }

    /// <summary>
    /// Maps the ArdaNova SignalR hub endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapArdaNovaHubs(this WebApplication app)
    {
        app.MapHub<ArdaNovaHub>("/hubs/ardanova");

        return app;
    }
}

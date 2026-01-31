namespace ArdaNova.API.WebSocket.Clients;

/// <summary>
/// Strongly-typed client interface for the ArdaNova SignalR hub.
/// Defines methods that the server can call on connected clients.
/// </summary>
public interface IArdaNovaHubClient
{
    /// <summary>
    /// Receives a generic event with type and payload.
    /// </summary>
    /// <param name="eventType">The event type identifier (e.g., "project.created").</param>
    /// <param name="payload">The event payload data.</param>
    Task ReceiveEvent(string eventType, object payload);

    /// <summary>
    /// Receives a user-related event.
    /// </summary>
    Task UserCreated(object user);

    /// <summary>
    /// Receives a user updated event.
    /// </summary>
    Task UserUpdated(object user);

    /// <summary>
    /// Receives a project created event.
    /// </summary>
    Task ProjectCreated(object project);

    /// <summary>
    /// Receives a project updated event.
    /// </summary>
    Task ProjectUpdated(object project);

    /// <summary>
    /// Receives a project status changed event.
    /// </summary>
    Task ProjectStatusChanged(object data);

    /// <summary>
    /// Receives a task completed event.
    /// </summary>
    Task TaskCompleted(object task);

    /// <summary>
    /// Receives a notification.
    /// </summary>
    Task NotificationReceived(object notification);

    /// <summary>
    /// Receives an activity event.
    /// </summary>
    Task ActivityLogged(object activity);

    // ========== Chat Events ==========

    /// <summary>
    /// Receives a new chat message.
    /// </summary>
    Task MessageReceived(object message);

    /// <summary>
    /// Receives message status update (delivered/read).
    /// </summary>
    Task MessageStatusUpdated(object statusUpdate);

    /// <summary>
    /// Receives typing indicator.
    /// </summary>
    Task TypingIndicatorReceived(object typingIndicator);

    /// <summary>
    /// Receives conversation created/updated event.
    /// </summary>
    Task ConversationUpdated(object conversation);

    /// <summary>
    /// Receives notification when added to a conversation.
    /// </summary>
    Task AddedToConversation(object conversation);

    /// <summary>
    /// Receives notification when removed from a conversation.
    /// </summary>
    Task RemovedFromConversation(object data);
}

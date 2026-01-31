using ArdaNova.API.WebSocket.Clients;
using Microsoft.AspNetCore.SignalR;

namespace ArdaNova.API.WebSocket.Hubs;

/// <summary>
/// Main SignalR hub for real-time communication with ArdaNova clients.
/// Supports subscription to user-specific, project-specific, and guild-specific events.
/// </summary>
public class ArdaNovaHub : Hub<IArdaNovaHubClient>
{
    private readonly ILogger<ArdaNovaHub> _logger;
    private readonly IConfiguration _configuration;

    public ArdaNovaHub(ILogger<ArdaNovaHub> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the user ID from the X-User-Id header or userId query parameter.
    /// </summary>
    private string? GetUserId() =>
        Context.GetHttpContext()?.Request.Headers["X-User-Id"].FirstOrDefault()
        ?? Context.GetHttpContext()?.Request.Query["userId"].FirstOrDefault();

    /// <summary>
    /// Validates the API key from the request.
    /// </summary>
    private bool ValidateApiKey()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null) return false;

        var expectedApiKey = _configuration["API_KEY"] ?? Environment.GetEnvironmentVariable("API_KEY");
        if (string.IsNullOrEmpty(expectedApiKey)) return false;

        var providedApiKey = httpContext.Request.Headers["X-Api-Key"].FirstOrDefault()
                          ?? httpContext.Request.Query["api_key"].FirstOrDefault();

        return !string.IsNullOrEmpty(providedApiKey) && providedApiKey == expectedApiKey;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var httpContext = Context.GetHttpContext();

        // Validate API key
        if (!ValidateApiKey())
        {
            _logger.LogWarning(
                "Hub connection rejected: Invalid or missing API key from {RemoteIp}",
                httpContext?.Connection.RemoteIpAddress);
            Context.Abort();
            return;
        }

        var userId = GetUserId();

        _logger.LogInformation(
            "Client connected: {ConnectionId}, User: {UserId}",
            connectionId,
            userId ?? "anonymous");

        // Automatically add user to their personal group for targeted notifications
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(connectionId, $"user:{userId}");
            _logger.LogDebug("Added connection {ConnectionId} to group user:{UserId}", connectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation(
            "Client disconnected: {ConnectionId}, User: {UserId}, Exception: {Exception}",
            connectionId,
            userId ?? "anonymous",
            exception?.Message ?? "none");

        // Remove from user group
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(connectionId, $"user:{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes the client to receive events for a specific project.
    /// </summary>
    /// <param name="projectId">The project ID to subscribe to.</param>
    public async Task SubscribeToProject(string projectId)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;
        var groupName = $"project:{projectId}";

        // TODO: Optionally validate that the user has access to this project
        // by injecting IProjectService and checking membership

        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} (User: {UserId}) subscribed to project {ProjectId}",
            connectionId,
            userId ?? "anonymous",
            projectId);
    }

    /// <summary>
    /// Unsubscribes the client from a specific project's events.
    /// </summary>
    /// <param name="projectId">The project ID to unsubscribe from.</param>
    public async Task UnsubscribeFromProject(string projectId)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"project:{projectId}";

        await Groups.RemoveFromGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from project {ProjectId}",
            connectionId,
            projectId);
    }

    /// <summary>
    /// Subscribes the client to receive events for a specific guild.
    /// </summary>
    /// <param name="guildId">The guild ID to subscribe to.</param>
    public async Task SubscribeToGuild(string guildId)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;
        var groupName = $"guild:{guildId}";

        // TODO: Optionally validate that the user is a member of this guild

        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} (User: {UserId}) subscribed to guild {GuildId}",
            connectionId,
            userId ?? "anonymous",
            guildId);
    }

    /// <summary>
    /// Unsubscribes the client from a specific guild's events.
    /// </summary>
    /// <param name="guildId">The guild ID to unsubscribe from.</param>
    public async Task UnsubscribeFromGuild(string guildId)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"guild:{guildId}";

        await Groups.RemoveFromGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from guild {GuildId}",
            connectionId,
            guildId);
    }

    /// <summary>
    /// Subscribes to a specific user's events (typically for viewing another user's public activity).
    /// Only allows subscribing to own user events by default.
    /// </summary>
    /// <param name="targetUserId">The user ID to subscribe to.</param>
    public async Task SubscribeToUser(string targetUserId)
    {
        var currentUserId = GetUserId();
        var connectionId = Context.ConnectionId;

        // For security, only allow subscribing to own user events
        // Modify this logic if public user feeds are needed
        if (currentUserId != targetUserId.ToString())
        {
            _logger.LogWarning(
                "Client {ConnectionId} attempted to subscribe to another user's events: {TargetUserId}",
                connectionId,
                targetUserId);
            return;
        }

        var groupName = $"user:{targetUserId}";
        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to user {UserId}",
            connectionId,
            targetUserId);
    }

    /// <summary>
    /// Unsubscribes from a user's events.
    /// </summary>
    /// <param name="targetUserId">The user ID to unsubscribe from.</param>
    public async Task UnsubscribeFromUser(string targetUserId)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"user:{targetUserId}";

        await Groups.RemoveFromGroupAsync(connectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from user {UserId}",
            connectionId,
            targetUserId);
    }

    /// <summary>
    /// Subscribes to all events (broadcast channel).
    /// Use with caution as this can be noisy.
    /// </summary>
    public async Task SubscribeToAll()
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, "all");

        _logger.LogDebug("Client {ConnectionId} subscribed to all events", connectionId);
    }

    /// <summary>
    /// Unsubscribes from all events.
    /// </summary>
    public async Task UnsubscribeFromAll()
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, "all");

        _logger.LogDebug("Client {ConnectionId} unsubscribed from all events", connectionId);
    }

    // ========== Chat Methods ==========

    /// <summary>
    /// Subscribes to a conversation for real-time updates.
    /// </summary>
    /// <param name="conversationId">The conversation ID to subscribe to.</param>
    public async Task SubscribeToConversation(string conversationId)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        // Require authenticated user
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "Unauthenticated subscription attempt to conversation {ConversationId}",
                conversationId);
            return;
        }

        var groupName = $"conversation:{conversationId}";

        // NOTE: Full membership validation requires injecting IChatService
        // For now, we rely on the API-level membership checks in ChatService
        // The client can only call this after successfully fetching the conversation,
        // which validates membership

        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} (User: {UserId}) subscribed to conversation {ConversationId}",
            connectionId,
            userId,
            conversationId);
    }

    /// <summary>
    /// Unsubscribes from a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to unsubscribe from.</param>
    public async Task UnsubscribeFromConversation(string conversationId)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"conversation:{conversationId}";

        await Groups.RemoveFromGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from conversation {ConversationId}",
            connectionId,
            conversationId);
    }

    /// <summary>
    /// Sends typing indicator to other conversation members.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="isTyping">Whether the user is typing.</param>
    public async Task SendTypingIndicator(string conversationId, bool isTyping)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        // Input validation: prevent abuse with excessively long conversation IDs
        if (conversationId.Length > 100)
        {
            _logger.LogWarning(
                "User {UserId} attempted to send typing indicator with invalid conversation ID length",
                userId);
            return;
        }

        // TODO: Add rate limiting to prevent typing indicator spam
        // Consider implementing a sliding window (e.g., max 10 indicators per 5 seconds)

        var groupName = $"conversation:{conversationId}";

        // Broadcast to all members except sender
        await Clients.OthersInGroup(groupName).TypingIndicatorReceived(new
        {
            conversationId,
            userId,
            isTyping,
            timestamp = DateTime.UtcNow
        });

        _logger.LogDebug(
            "User {UserId} typing indicator ({IsTyping}) in conversation {ConversationId}",
            userId,
            isTyping,
            conversationId);
    }

    /// <summary>
    /// Marks messages as read and broadcasts to conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="lastReadMessageId">The ID of the last read message.</param>
    public async Task MarkAsRead(string conversationId, string lastReadMessageId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        var groupName = $"conversation:{conversationId}";

        await Clients.OthersInGroup(groupName).MessageStatusUpdated(new
        {
            conversationId,
            userId,
            lastReadMessageId,
            status = "READ",
            timestamp = DateTime.UtcNow
        });
    }
}

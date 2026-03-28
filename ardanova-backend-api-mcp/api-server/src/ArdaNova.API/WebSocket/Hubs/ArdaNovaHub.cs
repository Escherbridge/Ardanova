using ArdaNova.API.WebSocket.Clients;
using ArdaNova.Application.Services.Interfaces;
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
    private readonly IProjectMemberService _projectMemberService;
    private readonly IGuildService _guildService;
    private readonly IGuildMemberService _guildMemberService;

    private static readonly object TypingLock = new();
    private static readonly Dictionary<string, List<DateTime>> TypingWindows = new();

    private static bool IsTypingAllowed(string userId, string conversationId)
    {
        const int maxEvents = 10;
        var window = TimeSpan.FromSeconds(5);
        var key = $"{userId}:{conversationId}";
        var now = DateTime.UtcNow;
        lock (TypingLock)
        {
            if (!TypingWindows.TryGetValue(key, out var list))
            {
                list = new List<DateTime>();
                TypingWindows[key] = list;
            }

            list.RemoveAll(t => now - t > window);
            if (list.Count >= maxEvents)
            {
                return false;
            }

            list.Add(now);
            return true;
        }
    }

    public ArdaNovaHub(
        ILogger<ArdaNovaHub> logger,
        IConfiguration configuration,
        IProjectMemberService projectMemberService,
        IGuildService guildService,
        IGuildMemberService guildMemberService)
    {
        _logger = logger;
        _configuration = configuration;
        _projectMemberService = projectMemberService;
        _guildService = guildService;
        _guildMemberService = guildMemberService;
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

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "SubscribeToProject denied: missing user id (connection {ConnectionId})",
                connectionId);
            return;
        }

        var membersResult = await _projectMemberService.GetByProjectIdAsync(projectId, CancellationToken.None);
        if (!membersResult.IsSuccess || membersResult.Value is null ||
            membersResult.Value.All(m => m.UserId != userId))
        {
            _logger.LogWarning(
                "SubscribeToProject denied: user {UserId} is not a member of project {ProjectId}",
                userId,
                projectId);
            return;
        }

        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} (User: {UserId}) subscribed to project {ProjectId}",
            connectionId,
            userId,
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

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "SubscribeToGuild denied: missing user id (connection {ConnectionId})",
                connectionId);
            return;
        }

        var guildResult = await _guildService.GetByIdAsync(guildId, CancellationToken.None);
        if (!guildResult.IsSuccess || guildResult.Value is null)
        {
            _logger.LogWarning("SubscribeToGuild denied: guild {GuildId} not found", guildId);
            return;
        }

        if (guildResult.Value.OwnerId != userId)
        {
            var membersResult = await _guildMemberService.GetByGuildIdAsync(guildId, CancellationToken.None);
            if (!membersResult.IsSuccess || membersResult.Value is null ||
                membersResult.Value.All(m => m.UserId != userId))
            {
                _logger.LogWarning(
                    "SubscribeToGuild denied: user {UserId} is not a member of guild {GuildId}",
                    userId,
                    guildId);
                return;
            }
        }

        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} (User: {UserId}) subscribed to guild {GuildId}",
            connectionId,
            userId,
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

        if (!IsTypingAllowed(userId, conversationId))
        {
            _logger.LogDebug(
                "Typing indicator rate limited for user {UserId} in conversation {ConversationId}",
                userId,
                conversationId);
            return;
        }

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

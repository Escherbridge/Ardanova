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
    /// Gets the user ID from the X-User-Id header (set by Next.js proxy).
    /// </summary>
    private string? GetUserId() =>
        Context.GetHttpContext()?.Request.Headers["X-User-Id"].FirstOrDefault();

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
}

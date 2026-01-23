namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ModelContextProtocol.Server;

[McpServerToolType]
public class NotificationTools
{
    private readonly INotificationService _notificationService;

    public NotificationTools(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [McpServerTool(Name = "notification_get_by_id")]
    [Description("Retrieves a notification by its unique identifier")]
    public async Task<NotificationDto?> GetNotificationById(
        [Description("The unique identifier of the notification")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "notification_get_by_user_id")]
    [Description("Retrieves all notifications for a user")]
    public async Task<IReadOnlyList<NotificationDto>?> GetNotificationsByUserId(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "notification_get_unread")]
    [Description("Retrieves all unread notifications for a user")]
    public async Task<IReadOnlyList<NotificationDto>?> GetUnreadNotifications(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetUnreadByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "notification_get_summary")]
    [Description("Gets notification summary (total and unread count) for a user")]
    public async Task<NotificationSummaryDto?> GetNotificationSummary(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetSummaryAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "notification_create")]
    [Description("Creates a new notification for a user")]
    public async Task<NotificationDto?> CreateNotification(
        [Description("The user ID to notify")] Guid userId,
        [Description("The notification type")] NotificationType type,
        [Description("The notification title")] string title,
        [Description("The notification message")] string message,
        [Description("Optional action URL")] string? actionUrl = null,
        CancellationToken ct = default)
    {
        var dto = new CreateNotificationDto
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl
        };
        var result = await _notificationService.CreateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "notification_mark_read")]
    [Description("Marks a notification as read")]
    public async Task<NotificationDto?> MarkNotificationAsRead(
        [Description("The notification ID")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _notificationService.MarkAsReadAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "notification_mark_all_read")]
    [Description("Marks all notifications as read for a user")]
    public async Task<bool> MarkAllNotificationsAsRead(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return result.IsSuccess && result.Value;
    }
}

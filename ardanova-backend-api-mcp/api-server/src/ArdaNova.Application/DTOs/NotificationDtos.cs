namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = null!;
    public string Message { get; init; } = null!;
    public string? Data { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateNotificationDto
{
    public required Guid UserId { get; init; }
    public required NotificationType Type { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? Data { get; init; }
    public string? ActionUrl { get; init; }
}

public record NotificationSummaryDto
{
    public int TotalCount { get; init; }
    public int UnreadCount { get; init; }
}

namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? Data { get; private set; } // JSON data
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private Notification() { }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? data = null,
        string? actionUrl = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Data = data,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
    }
}

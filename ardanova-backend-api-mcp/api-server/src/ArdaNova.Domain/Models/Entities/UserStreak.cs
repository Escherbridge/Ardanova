namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class UserStreak
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastActivityDate { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StreakType StreakType { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private UserStreak() { }

    public static UserStreak Create(Guid userId, StreakType streakType = StreakType.DAILY_LOGIN)
    {
        return new UserStreak
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStreak = 0,
            LongestStreak = 0,
            StreakType = streakType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void IncrementStreak()
    {
        CurrentStreak++;
        if (CurrentStreak > LongestStreak)
            LongestStreak = CurrentStreak;
        LastActivityDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetStreak()
    {
        CurrentStreak = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordActivity()
    {
        var today = DateTime.UtcNow.Date;
        var lastActivity = LastActivityDate?.Date;

        if (lastActivity == today)
            return; // Already recorded today

        if (lastActivity == today.AddDays(-1))
        {
            // Consecutive day
            IncrementStreak();
        }
        else
        {
            // Streak broken, start fresh
            ResetStreak();
            IncrementStreak();
        }
    }
}

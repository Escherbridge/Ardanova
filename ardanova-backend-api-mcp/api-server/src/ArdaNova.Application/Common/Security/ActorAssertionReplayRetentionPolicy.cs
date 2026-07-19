namespace ArdaNova.Application.Common.Security;

/// <summary>Defines the shared validity and cleanup windows for actor assertion replay evidence.</summary>
public static class ActorAssertionReplayRetentionPolicy
{
    public const int AllowedClockSkewSeconds = 30;
    public const int RetentionAfterExpirySeconds = 5 * 60;
    public const int CleanupIntervalSeconds = 60;
    public const int CleanupTimeBudgetSeconds = 5;
    public const int CleanupBatchSize = 500;

    public static DateTime PurgeBefore(DateTimeOffset now)
        => now.UtcDateTime.AddSeconds(-RetentionAfterExpirySeconds);
}

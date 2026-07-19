namespace ArdaNova.Application.Common.Interfaces;

/// <summary>Deletes one bounded batch of actor assertion replay records past the retention cutoff.</summary>
public interface IActorAssertionReplayCleanupStore
{
    Task<int> PurgeBatchAsync(
        DateTime expiresBefore,
        int batchSize,
        CancellationToken ct = default);
}

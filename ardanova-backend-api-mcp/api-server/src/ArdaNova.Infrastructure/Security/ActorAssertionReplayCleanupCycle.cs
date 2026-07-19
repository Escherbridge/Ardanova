namespace ArdaNova.Infrastructure.Security;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Security;

/// <summary>Runs one time- and batch-bounded replay cleanup cycle.</summary>
public sealed class ActorAssertionReplayCleanupCycle
{
    private readonly IActorAssertionReplayCleanupStore _store;
    private readonly TimeProvider _timeProvider;

    public ActorAssertionReplayCleanupCycle(
        IActorAssertionReplayCleanupStore store,
        TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task<ActorAssertionReplayCleanupResult> RunAsync(CancellationToken ct)
    {
        var startedAt = _timeProvider.GetTimestamp();
        var timeBudget = TimeSpan.FromSeconds(
            ActorAssertionReplayRetentionPolicy.CleanupTimeBudgetSeconds);
        var purgeBefore = ActorAssertionReplayRetentionPolicy.PurgeBefore(
            _timeProvider.GetUtcNow());
        var removed = 0;
        var batches = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var remaining = timeBudget - _timeProvider.GetElapsedTime(startedAt);
            if (remaining <= TimeSpan.Zero)
                return new ActorAssertionReplayCleanupResult(removed, batches, true);

            using var budgetCancellation = new CancellationTokenSource(
                remaining,
                _timeProvider);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                budgetCancellation.Token);

            int batchCount;
            try
            {
                batchCount = await _store.PurgeBatchAsync(
                    purgeBefore,
                    ActorAssertionReplayRetentionPolicy.CleanupBatchSize,
                    linkedCancellation.Token);
            }
            catch (OperationCanceledException) when (
                budgetCancellation.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                return new ActorAssertionReplayCleanupResult(removed, batches, true);
            }

            removed += batchCount;
            batches++;
            if (batchCount < ActorAssertionReplayRetentionPolicy.CleanupBatchSize)
                return new ActorAssertionReplayCleanupResult(removed, batches, false);
        }
    }
}

public sealed record ActorAssertionReplayCleanupResult(
    int Removed,
    int Batches,
    bool TimeBudgetReached);

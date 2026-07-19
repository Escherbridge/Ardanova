namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Security;
using ArdaNova.Infrastructure.Security;
using FluentAssertions;

public class ActorAssertionReplayCleanupCycleTests
{
    [Fact]
    public async Task RunAsync_UsesOneCutoffAndStopsAfterTheFirstShortBatch()
    {
        var now = new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);
        var time = new ManualTimeProvider(now);
        var store = new RecordingCleanupStore(
            ActorAssertionReplayRetentionPolicy.CleanupBatchSize,
            17);
        var cycle = new ActorAssertionReplayCleanupCycle(store, time);

        var result = await cycle.RunAsync(CancellationToken.None);

        result.Should().Be(new ActorAssertionReplayCleanupResult(
            ActorAssertionReplayRetentionPolicy.CleanupBatchSize + 17,
            2,
            false));
        store.Cutoffs.Should().OnlyContain(
            cutoff => cutoff == now.UtcDateTime.AddMinutes(-5));
        store.BatchSizes.Should().OnlyContain(
            size => size == ActorAssertionReplayRetentionPolicy.CleanupBatchSize);
    }

    [Fact]
    public async Task RunAsync_StopsBeforeStartingWorkBeyondTheTimeBudget()
    {
        var time = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        var store = new RecordingCleanupStore(
            ActorAssertionReplayRetentionPolicy.CleanupBatchSize)
        {
            AfterPurge = () => time.Advance(TimeSpan.FromSeconds(
                ActorAssertionReplayRetentionPolicy.CleanupTimeBudgetSeconds))
        };
        var cycle = new ActorAssertionReplayCleanupCycle(store, time);

        var result = await cycle.RunAsync(CancellationToken.None);

        result.Should().Be(new ActorAssertionReplayCleanupResult(
            ActorAssertionReplayRetentionPolicy.CleanupBatchSize,
            1,
            true));
        store.Cutoffs.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsync_CancelsAnInFlightBatchWhenTheTimeBudgetExpires()
    {
        var time = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        var store = new CancellationAwaitingCleanupStore();
        var cycle = new ActorAssertionReplayCleanupCycle(store, time);

        var run = cycle.RunAsync(CancellationToken.None);
        await store.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(
            ActorAssertionReplayRetentionPolicy.CleanupTimeBudgetSeconds));

        var result = await run.WaitAsync(TimeSpan.FromSeconds(1));

        result.Should().Be(new ActorAssertionReplayCleanupResult(0, 0, true));
        store.ObservedCancellation.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_PropagatesCallerCancellationDuringAnInFlightBatch()
    {
        var time = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));
        var store = new CancellationAwaitingCleanupStore();
        var cycle = new ActorAssertionReplayCleanupCycle(store, time);
        using var cancellation = new CancellationTokenSource();

        var run = cycle.RunAsync(cancellation.Token);
        await store.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        cancellation.Cancel();

        await FluentActions.Awaiting(() => run).Should().ThrowAsync<OperationCanceledException>();
        store.ObservedCancellation.Should().BeTrue();
    }

    private sealed class RecordingCleanupStore(params int[] results)
        : IActorAssertionReplayCleanupStore
    {
        private readonly Queue<int> _results = new(results);

        public Action? AfterPurge { get; init; }
        public List<DateTime> Cutoffs { get; } = [];
        public List<int> BatchSizes { get; } = [];

        public Task<int> PurgeBatchAsync(
            DateTime expiresBefore,
            int batchSize,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Cutoffs.Add(expiresBefore);
            BatchSizes.Add(batchSize);
            var result = _results.Count > 0 ? _results.Dequeue() : 0;
            AfterPurge?.Invoke();
            return Task.FromResult(result);
        }
    }

    private sealed class CancellationAwaitingCleanupStore
        : IActorAssertionReplayCleanupStore
    {
        private int _observedCancellation;

        public TaskCompletionSource<bool> Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ObservedCancellation => Volatile.Read(ref _observedCancellation) == 1;

        public async Task<int> PurgeBatchAsync(
            DateTime expiresBefore,
            int batchSize,
            CancellationToken ct = default)
        {
            Started.TrySetResult(true);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return 0;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Interlocked.Exchange(ref _observedCancellation, 1);
                throw;
            }
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset initialNow) : TimeProvider
    {
        private readonly object _gate = new();
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _now = initialNow;
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override DateTimeOffset GetUtcNow()
        {
            lock (_gate)
                return _now;
        }

        public override long GetTimestamp()
        {
            lock (_gate)
                return _timestamp;
        }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state, dueTime, period);
            lock (_gate)
                _timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan amount)
        {
            ManualTimer[] timers;
            long timestamp;
            lock (_gate)
            {
                _now = _now.Add(amount);
                _timestamp += amount.Ticks;
                timestamp = _timestamp;
                timers = [.. _timers];
            }

            foreach (var timer in timers)
                timer.FireIfDue(timestamp);
        }

        private void Remove(ManualTimer timer)
        {
            lock (_gate)
                _timers.Remove(timer);
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _provider;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private readonly object _gate = new();
            private long _dueAt;
            private TimeSpan _period;
            private bool _disposed;

            public ManualTimer(
                ManualTimeProvider provider,
                TimerCallback callback,
                object? state,
                TimeSpan dueTime,
                TimeSpan period)
            {
                _provider = provider;
                _callback = callback;
                _state = state;
                _period = period;
                _dueAt = DueAt(provider.GetTimestamp(), dueTime);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_gate)
                {
                    if (_disposed)
                        return false;

                    _period = period;
                    _dueAt = DueAt(_provider.GetTimestamp(), dueTime);
                    return true;
                }
            }

            public void Dispose()
            {
                lock (_gate)
                {
                    if (_disposed)
                        return;
                    _disposed = true;
                }

                _provider.Remove(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public void FireIfDue(long timestamp)
            {
                lock (_gate)
                {
                    if (_disposed || timestamp < _dueAt)
                        return;

                    _dueAt = _period == Timeout.InfiniteTimeSpan
                        ? long.MaxValue
                        : DueAt(timestamp, _period);
                }

                _callback(_state);
            }

            private static long DueAt(long timestamp, TimeSpan dueTime)
                => dueTime == Timeout.InfiniteTimeSpan
                    ? long.MaxValue
                    : timestamp + Math.Max(0, dueTime.Ticks);
        }
    }
}

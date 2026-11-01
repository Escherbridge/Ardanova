namespace ArdaNova.Application.Feed;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>What one quality pass did.</summary>
public sealed record FeedQualityRun(FeedProjectorOutcome Outcome, int Examined, int Updated, int Failed)
{
    public static FeedQualityRun RunnerBusy { get; } = new(FeedProjectorOutcome.RunnerBusy, 0, 0, 0);
}

/// <summary>
/// Recomputes every live post's stored quality (Post.trendingScore) under the projector's runner lock, in
/// sweep-sized batches, writing only rows whose score moved. See Feed/AGENTS.md §Quality score.
/// </summary>
public sealed class FeedQualityPass
{
    private readonly IFeedReadStore _reads;
    private readonly IFeedEnvelopeStore _store;
    private readonly FeedProjectorOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<FeedQualityPass> _logger;

    public FeedQualityPass(
        IFeedReadStore reads,
        IFeedEnvelopeStore store,
        FeedProjectorOptions options,
        TimeProvider clock,
        ILogger<FeedQualityPass>? logger = null)
    {
        _reads = reads;
        _store = store;
        _options = options;
        _clock = clock;
        _logger = logger ?? NullLogger<FeedQualityPass>.Instance;
    }

    public async Task<FeedQualityRun> RunAsync(CancellationToken ct = default)
    {
        await using var runnerLock = await _store.TryAcquireRunnerLockAsync(ct);
        if (runnerLock is null)
            return FeedQualityRun.RunnerBusy;

        var now = _clock.GetUtcNow().UtcDateTime;
        var examined = 0;
        var updated = 0;
        var failed = 0;
        var ids = await _reads.ReadScorablePostIdsAsync(ct);
        foreach (var batch in ids.Chunk(Math.Max(1, _options.SweepBatchSize)))
        {
            foreach (var row in await _reads.ReadQualitySignalsAsync(batch, ct))
            {
                examined++;
                var quality = FeedQualityScore.Compute(row.Signals, now);
                if (quality == row.StoredQuality)
                    continue;
                try
                {
                    updated += await _store.SetQualityAsync(row.PostId, quality, now, ct);
                }
                catch (Exception failure) when (!ct.IsCancellationRequested)
                {
                    failed++;
                    _logger.LogError(failure, "Feed quality: scoring post {PostId} failed; skipped, the rest of the pass went ahead.", row.PostId);
                }
            }
        }

        return new FeedQualityRun(FeedProjectorOutcome.Completed, examined, updated, failed);
    }
}

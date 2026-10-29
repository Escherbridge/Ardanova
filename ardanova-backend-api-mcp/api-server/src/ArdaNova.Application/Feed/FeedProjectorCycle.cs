namespace ArdaNova.Application.Feed;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Pull projector: reads committed source rows and makes the feed envelopes match them. Plan, then apply,
/// so a dry run is the same code with the writes left off. See Feed/AGENTS.md.
/// </summary>
public sealed class FeedProjectorCycle
{
    private readonly IFeedSourceReader _sources;
    private readonly IFeedEnvelopeStore _envelopes;
    private readonly IRepository<Post> _posts;
    private readonly FeedProjectorOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<FeedProjectorCycle> _logger;

    public FeedProjectorCycle(
        IFeedSourceReader sources,
        IFeedEnvelopeStore envelopes,
        IRepository<Post> posts,
        FeedProjectorOptions options,
        TimeProvider clock,
        ILogger<FeedProjectorCycle>? logger = null)
    {
        _sources = sources;
        _envelopes = envelopes;
        _posts = posts;
        _options = options;
        _clock = clock;
        _logger = logger ?? NullLogger<FeedProjectorCycle>.Instance;
    }

    /// <summary>
    /// One timed cycle: each kind read from its <paramref name="watermark"/> (stretched back over any stall), plus
    /// a full envelope reconciliation when a sweep is due. The watermark advances only for kinds read and applied.
    /// </summary>
    public Task<FeedProjectorRun> RunAsync(bool sweep, FeedProjectorWatermark watermark, CancellationToken ct = default)
        => ExecuteAsync(new Pass(Sweep: sweep, DryRun: false, Watermark: watermark), ct);

    /// <summary>The same pass with no window: every source row ever written, and every envelope re-checked.</summary>
    public Task<FeedProjectorRun> BackfillAsync(bool dryRun, CancellationToken ct = default)
        => ExecuteAsync(new Pass(Sweep: true, DryRun: dryRun, Watermark: null), ct);

    private async Task<FeedProjectorRun> ExecuteAsync(Pass pass, CancellationToken ct)
    {
        if (pass.DryRun)
        {
            var preview = await PlanAsync(pass, Now(), ct);
            return new FeedProjectorRun(
                FeedProjectorOutcome.Completed, true, pass.Sweep, preview.SourcesExamined, preview.Inserts.Count,
                preview.AlreadyProjected, 0, preview.Removals.Count, preview.Resyncs.Count, preview.Inserts,
                preview.SourceReadsFailed, preview.CardsFailed);
        }

        await using var runnerLock = await _envelopes.TryAcquireRunnerLockAsync(ct);
        if (runnerLock is null)
            return FeedProjectorRun.RunnerBusy;

        var startedAt = Now();
        var plan = await PlanAsync(pass, startedAt, ct);
        var run = await ApplyAsync(pass, plan, ct);

        // Only after the apply: a cycle that throws part-way leaves the mark where it was, so it is re-read.
        if (pass.Watermark is { } watermark)
        {
            foreach (var kind in plan.KindsRead)
                watermark.Advance(kind, startedAt);
        }

        return run;
    }

    private async Task<Plan> PlanAsync(Pass pass, DateTime startedAt, CancellationToken ct)
    {
        var plan = new Plan();
        var sources = new List<FeedSource>();
        foreach (var kind in Enum.GetValues<FeedSourceType>())
        {
            DateTime? since = pass.Watermark is { } watermark
                ? watermark.SinceFor(kind, startedAt, _options.LookBack)
                : null;
            try
            {
                sources.AddRange(await _sources.ReadChangedSinceAsync(kind, since, ct));
                plan.KindsRead.Add(kind);
            }
            catch (Exception failure) when (!ct.IsCancellationRequested)
            {
                plan.SourceReadsFailed++;
                _logger.LogError(
                    failure,
                    "Feed projector: reading {SourceKind} failed; its rows wait for a later cycle, every other kind went ahead.",
                    kind);
            }
        }

        plan.SourcesExamined = sources.Count;
        await PlanSourcesAsync(sources, plan, ct);
        if (pass.Sweep)
            await PlanSweepAsync(plan, ct);
        return plan;
    }

    /// <summary>Every source the window saw: insert its missing envelope, and re-judge the one it already has.</summary>
    private async Task PlanSourcesAsync(IReadOnlyList<FeedSource> sources, Plan plan, CancellationToken ct)
    {
        var wanted = new List<(FeedSource Source, FeedEnvelope? Envelope)>(sources.Count);
        foreach (var source in sources)
        {
            try
            {
                wanted.Add((source, FeedProjectionRules.Enter(source)));
            }
            catch (Exception failure)
            {
                CardFailed(plan, failure, "judging", source.Kind, source.Id);
            }
        }

        var keys = new HashSet<FeedSourceKey>();
        foreach (var (source, envelope) in wanted)
        {
            if (envelope is not null)
                keys.Add(envelope.Key);
            if (source.Kind != FeedSourceType.PROJECT_TOKEN_CONFIG)
                keys.Add(new FeedSourceKey(source.Kind, source.Id));
        }

        var existing = await FindStoredAsync(keys, ct);
        foreach (var (source, envelope) in wanted)
        {
            try
            {
                // A funding mark, once reached, stays reached; every other source can lose or move its envelope.
                if (source.Kind != FeedSourceType.PROJECT_TOKEN_CONFIG
                    && existing.TryGetValue(new FeedSourceKey(source.Kind, source.Id), out var stored))
                {
                    if (plan.Judge(stored, source) && envelope is not null)
                        plan.AlreadyProjected++;
                    continue;
                }

                if (envelope is null)
                    continue;
                if (existing.ContainsKey(envelope.Key))
                    plan.AlreadyProjected++;
                else
                    plan.Insert(envelope);
            }
            catch (Exception failure)
            {
                CardFailed(plan, failure, "judging", source.Kind, source.Id);
            }
        }
    }

    /// <summary>
    /// A deleted source leaves no row for the window to see, so it is found from the envelope side - and unlike
    /// <see cref="PlanSourcesAsync"/>'s window, this walks EVERY envelope, whatever its age: a time-filtered sweep
    /// never reconsiders a card whose source outlives the filter and only later loses its target or eligibility.
    /// See Feed/AGENTS.md §Sweep for the lock/batching trade-off.
    /// </summary>
    private async Task PlanSweepAsync(Plan plan, CancellationToken ct)
    {
        var orphans = await _posts.Query()
            .Where(post => post.sourceType != null)
            .Where(FeedProjectionRules.HasLostItsTarget)
            .Select(post => post.id)
            .ToListAsync(ct);
        foreach (var orphan in orphans)
            plan.Remove(orphan);

        // One id per envelope is cheap to hold for the whole table; batching it in memory (not via a SQL
        // "WHERE id > cursor", which would need a translatable string ordering) keeps every wide read and
        // every per-kind source lookup below bounded to SweepBatchSize rows.
        var allIds = await _posts.Query()
            .Where(post => post.sourceType != null)
            .OrderBy(post => post.id)
            .Select(post => post.id)
            .ToListAsync(ct);

        foreach (var batchIds in allIds.Chunk(Math.Max(1, _options.SweepBatchSize)))
        {
            var pending = batchIds.Where(id => !plan.Removals.Contains(id)).ToArray();
            if (pending.Length == 0)
                continue;

            var stored = await ReadStoredAsync(_posts.Query().Where(post => pending.Contains(post.id)), ct);
            foreach (var kind in stored.GroupBy(envelope => envelope.Key.SourceType))
            {
                var rowIds = kind.Select(envelope => FeedProjectionRules.RowIdOf(envelope.Key)).Distinct().ToList();
                Dictionary<string, FeedSource> current;
                try
                {
                    current = (await _sources.ReadByIdsAsync(kind.Key, rowIds, ct)).ToDictionary(source => source.Id);
                }
                catch (Exception failure) when (!ct.IsCancellationRequested)
                {
                    // An unreadable source is not a deleted one: these cards stay until a sweep can read it.
                    plan.SourceReadsFailed++;
                    _logger.LogError(
                        failure,
                        "Feed projector sweep: reading {SourceKind} failed; its {Count} cards were left as they are.",
                        kind.Key,
                        kind.Count());
                    continue;
                }

                foreach (var envelope in kind)
                {
                    try
                    {
                        if (current.TryGetValue(FeedProjectionRules.RowIdOf(envelope.Key), out var source))
                            plan.Judge(envelope, source);
                        else
                            plan.Remove(envelope.Id);
                    }
                    catch (Exception failure)
                    {
                        CardFailed(plan, failure, "judging", envelope.Key.SourceType, envelope.Key.SourceId);
                    }
                }
            }
        }
    }

    /// <summary>Removals first, so a card replaced for having moved never collides with its own replacement.</summary>
    private async Task<FeedProjectorRun> ApplyAsync(Pass pass, Plan plan, CancellationToken ct)
    {
        var now = Now();
        // Deleted in the same bounded batches the plan was read in: one sweep's worth of removals never
        // becomes a single unbounded DELETE/transaction, however many stale envelopes it found.
        var removed = 0;
        foreach (var batch in plan.Removals.Chunk(Math.Max(1, _options.SweepBatchSize)))
        {
            try
            {
                removed += await _envelopes.RemoveAsync(batch, ct);
            }
            catch (Exception failure) when (!ct.IsCancellationRequested)
            {
                // One bad envelope must not keep the rest of its batch on the feed: retry them one at a time.
                _logger.LogWarning(failure, "Feed projector: removing a batch of {Count} cards failed; retrying one by one.", batch.Length);
                foreach (var envelopeId in batch)
                {
                    try
                    {
                        removed += await _envelopes.RemoveAsync([envelopeId], ct);
                    }
                    catch (Exception single) when (!ct.IsCancellationRequested)
                    {
                        CardFailed(plan, single, "removing", null, envelopeId);
                    }
                }
            }
        }

        var resynced = 0;
        foreach (var (envelopeId, visibility) in plan.Resyncs)
        {
            try
            {
                resynced += await _envelopes.SetVisibilityAsync(envelopeId, visibility, now, ct);
            }
            catch (Exception failure) when (!ct.IsCancellationRequested)
            {
                CardFailed(plan, failure, "re-audiencing", null, envelopeId);
            }
        }

        var projected = 0;
        var alreadyProjected = plan.AlreadyProjected;
        var targetsGone = 0;
        foreach (var envelope in plan.Inserts)
        {
            try
            {
                switch (await _envelopes.InsertAsync(envelope, now, ct))
                {
                    case FeedInsertOutcome.Inserted:
                        projected++;
                        break;
                    case FeedInsertOutcome.AlreadyProjected:
                        alreadyProjected++;
                        break;
                    case FeedInsertOutcome.TargetGone:
                        targetsGone++;
                        break;
                }
            }
            catch (Exception failure) when (!ct.IsCancellationRequested)
            {
                CardFailed(plan, failure, "inserting", envelope.Key.SourceType, envelope.Key.SourceId);
            }
        }

        return new FeedProjectorRun(
            FeedProjectorOutcome.Completed, false, pass.Sweep, plan.SourcesExamined, projected, alreadyProjected,
            targetsGone, removed, resynced, [], plan.SourceReadsFailed, plan.CardsFailed);
    }

    /// <summary>The stored envelopes for these keys, one query per kind so each is a seek on (sourceType, sourceId).</summary>
    private async Task<Dictionary<FeedSourceKey, StoredFeedEnvelope>> FindStoredAsync(
        IReadOnlyCollection<FeedSourceKey> keys,
        CancellationToken ct)
    {
        var stored = new Dictionary<FeedSourceKey, StoredFeedEnvelope>();
        foreach (var kind in keys.GroupBy(key => key.SourceType))
        {
            var sourceIds = kind.Select(key => key.SourceId).Distinct().ToArray();
            var rows = await ReadStoredAsync(
                _posts.Query().Where(FeedProjectionRules.IsEnvelopeOf(kind.Key, sourceIds)), ct);
            foreach (var envelope in rows)
                stored[envelope.Key] = envelope;
        }

        return stored;
    }

    private static async Task<List<StoredFeedEnvelope>> ReadStoredAsync(IQueryable<Post> envelopes, CancellationToken ct)
        => (await envelopes
                .Select(post => new
                {
                    post.id,
                    post.sourceType,
                    post.sourceId,
                    post.visibility,
                    post.projectId,
                    post.guildId,
                    post.opportunityId,
                    post.eventId,
                })
                .ToListAsync(ct))
            .Select(row => new StoredFeedEnvelope(
                row.id,
                new FeedSourceKey(row.sourceType!.Value, row.sourceId!),
                row.visibility,
                new FeedEnvelopeTargets(row.projectId, row.guildId, row.opportunityId, row.eventId)))
            .ToList();

    private void CardFailed(Plan plan, Exception failure, string step, FeedSourceType? kind, string id)
    {
        plan.CardsFailed++;
        _logger.LogError(
            failure,
            "Feed projector: {Step} the card for {SourceKind} {Id} failed; skipped, the rest of the pass went ahead.",
            step,
            kind?.ToString() ?? "envelope",
            id);
    }

    private DateTime Now() => _clock.GetUtcNow().UtcDateTime;

    /// <summary>A null watermark is the backfill: no window at all.</summary>
    private sealed record Pass(bool Sweep, bool DryRun, FeedProjectorWatermark? Watermark);

    private sealed class Plan
    {
        private readonly HashSet<FeedSourceKey> _insertKeys = new();

        public int SourcesExamined { get; set; }
        public int AlreadyProjected { get; set; }
        public int SourceReadsFailed { get; set; }
        public int CardsFailed { get; set; }
        public HashSet<FeedSourceType> KindsRead { get; } = new();
        public List<FeedEnvelope> Inserts { get; } = [];
        public HashSet<string> Removals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, PostVisibility> Resyncs { get; } = new(StringComparer.Ordinal);

        public void Insert(FeedEnvelope envelope)
        {
            if (_insertKeys.Add(envelope.Key))
                Inserts.Add(envelope);
        }

        public void Remove(string envelopeId)
        {
            Removals.Add(envelopeId);
            Resyncs.Remove(envelopeId);
        }

        /// <summary>
        /// Re-judges a stored card against its source as it is now. True when the card stays (possibly moved to a
        /// new audience); false when it goes, or is replaced because the rows it points at have changed.
        /// </summary>
        public bool Judge(StoredFeedEnvelope stored, FeedSource source)
        {
            var retained = FeedProjectionRules.Retain(source);
            var targets = FeedProjectionRules.TargetsOf(source);
            if (retained is not { } visibility)
            {
                Remove(stored.Id);
                return false;
            }

            if (stored.Targets != targets)
            {
                // The targets gate who may see the card (a draft project hides its cards), so a card pointing at
                // the old rows is wrong about its audience: replace it, unless the source no longer earns entry.
                var replacement = FeedProjectionRules.Enter(source);
                Remove(stored.Id);
                if (replacement is not null && replacement.Key == stored.Key)
                    Insert(replacement);
                return false;
            }

            if (visibility != stored.Visibility && !Removals.Contains(stored.Id))
                Resyncs[stored.Id] = visibility;
            return true;
        }
    }
}

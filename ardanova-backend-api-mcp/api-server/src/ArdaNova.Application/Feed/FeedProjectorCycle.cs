namespace ArdaNova.Application.Feed;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

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

    public FeedProjectorCycle(
        IFeedSourceReader sources,
        IFeedEnvelopeStore envelopes,
        IRepository<Post> posts,
        FeedProjectorOptions options,
        TimeProvider clock)
    {
        _sources = sources;
        _envelopes = envelopes;
        _posts = posts;
        _options = options;
        _clock = clock;
    }

    /// <summary>One timed cycle: the look-back window, plus a full envelope reconciliation when a sweep is due.</summary>
    public Task<FeedProjectorRun> RunAsync(bool sweep, CancellationToken ct = default)
        => ExecuteAsync(new Pass(Since: Now() - _options.LookBack, Sweep: sweep, DryRun: false), ct);

    /// <summary>The same pass with no window: every source row ever written, and every envelope re-checked.</summary>
    public Task<FeedProjectorRun> BackfillAsync(bool dryRun, CancellationToken ct = default)
        => ExecuteAsync(new Pass(Since: null, Sweep: true, DryRun: dryRun), ct);

    private async Task<FeedProjectorRun> ExecuteAsync(Pass pass, CancellationToken ct)
    {
        if (pass.DryRun)
        {
            var preview = await PlanAsync(pass, ct);
            return new FeedProjectorRun(
                FeedProjectorOutcome.Completed, true, pass.Sweep, preview.SourcesExamined, preview.Inserts.Count,
                preview.AlreadyProjected, 0, preview.Removals.Count, preview.Resyncs.Count, preview.Inserts);
        }

        await using var runnerLock = await _envelopes.TryAcquireRunnerLockAsync(ct);
        if (runnerLock is null)
            return FeedProjectorRun.RunnerBusy;

        var plan = await PlanAsync(pass, ct);
        return await ApplyAsync(pass, plan, ct);
    }

    private async Task<Plan> PlanAsync(Pass pass, CancellationToken ct)
    {
        var plan = new Plan();
        var sources = await _sources.ReadChangedSinceAsync(pass.Since, ct);
        plan.SourcesExamined = sources.Count;
        await PlanSourcesAsync(sources, plan, ct);
        if (pass.Sweep)
            await PlanSweepAsync(plan, ct);
        return plan;
    }

    /// <summary>Every source the window saw: insert its missing envelope, and re-judge the one it already has.</summary>
    private async Task PlanSourcesAsync(IReadOnlyList<FeedSource> sources, Plan plan, CancellationToken ct)
    {
        var wanted = sources.Select(source => (Source: source, Envelope: FeedProjectionRules.Enter(source))).ToList();
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
            if (envelope is not null)
            {
                if (existing.ContainsKey(envelope.Key))
                    plan.AlreadyProjected++;
                else
                    plan.Inserts.Add(envelope);
            }

            // A funding mark, once reached, stays reached; every other source can lose its envelope.
            if (source.Kind != FeedSourceType.PROJECT_TOKEN_CONFIG
                && existing.TryGetValue(new FeedSourceKey(source.Kind, source.Id), out var stored))
            {
                plan.Judge(stored, FeedProjectionRules.Retain(source));
            }
        }
    }

    /// <summary>
    /// A deleted source leaves no row for the look-back window to see, so it is found from the envelope side -
    /// and unlike <see cref="PlanSourcesAsync"/>'s 10-minute look-back, this walks EVERY envelope, whatever its
    /// age: a security review found that a time-filtered sweep (the old "last 48 h" window) never reconsiders a
    /// card whose source outlives that window and only later loses its target or its eligibility (a deleted
    /// comment, an opportunity re-drafted, an event made INVITE_ONLY, long after projection). See Feed/AGENTS.md
    /// §Sweep for the lock/batching trade-off this makes.
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

            var stored = (await _posts.Query()
                    .Where(post => pending.Contains(post.id))
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

            foreach (var kind in stored.GroupBy(envelope => envelope.Key.SourceType))
            {
                var rowIds = kind.Select(envelope => FeedProjectionRules.RowIdOf(envelope.Key)).Distinct().ToList();
                var current = (await _sources.ReadByIdsAsync(kind.Key, rowIds, ct)).ToDictionary(source => source.Id);
                foreach (var envelope in kind)
                {
                    if (current.TryGetValue(FeedProjectionRules.RowIdOf(envelope.Key), out var source))
                        plan.Judge(envelope, FeedProjectionRules.Retain(source));
                    else
                        plan.Remove(envelope.Id);
                }
            }
        }
    }

    private async Task<FeedProjectorRun> ApplyAsync(Pass pass, Plan plan, CancellationToken ct)
    {
        var now = Now();
        // Deleted in the same bounded batches the plan was read in: one sweep's worth of removals never
        // becomes a single unbounded DELETE/transaction, however many stale envelopes it found.
        var removed = 0;
        foreach (var batch in plan.Removals.Chunk(Math.Max(1, _options.SweepBatchSize)))
            removed += await _envelopes.RemoveAsync(batch, ct);

        var resynced = 0;
        foreach (var (envelopeId, visibility) in plan.Resyncs)
            resynced += await _envelopes.SetVisibilityAsync(envelopeId, visibility, now, ct);

        var projected = 0;
        var alreadyProjected = plan.AlreadyProjected;
        var targetsGone = 0;
        foreach (var envelope in plan.Inserts)
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

        return new FeedProjectorRun(
            FeedProjectorOutcome.Completed, false, pass.Sweep, plan.SourcesExamined, projected, alreadyProjected,
            targetsGone, removed, resynced, []);
    }

    private async Task<Dictionary<FeedSourceKey, StoredFeedEnvelope>> FindStoredAsync(
        IReadOnlyCollection<FeedSourceKey> keys,
        CancellationToken ct)
    {
        if (keys.Count == 0)
            return new Dictionary<FeedSourceKey, StoredFeedEnvelope>();

        var sourceIds = keys.Select(key => key.SourceId).Distinct().ToArray();
        var rows = await _posts.Query()
            .Where(post => post.sourceType != null && sourceIds.Contains(post.sourceId!))
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
            .ToListAsync(ct);

        return rows
            .Select(row => new StoredFeedEnvelope(
                row.id,
                new FeedSourceKey(row.sourceType!.Value, row.sourceId!),
                row.visibility,
                new FeedEnvelopeTargets(row.projectId, row.guildId, row.opportunityId, row.eventId)))
            .Where(envelope => keys.Contains(envelope.Key))
            .ToDictionary(envelope => envelope.Key);
    }

    private DateTime Now() => _clock.GetUtcNow().UtcDateTime;

    private sealed record Pass(DateTime? Since, bool Sweep, bool DryRun);

    private sealed class Plan
    {
        public int SourcesExamined { get; set; }
        public int AlreadyProjected { get; set; }
        public List<FeedEnvelope> Inserts { get; } = [];
        public HashSet<string> Removals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, PostVisibility> Resyncs { get; } = new(StringComparer.Ordinal);

        public void Remove(string envelopeId)
        {
            Removals.Add(envelopeId);
            Resyncs.Remove(envelopeId);
        }

        /// <summary>Null means the source no longer earns an envelope; a different audience means it moved.</summary>
        public void Judge(StoredFeedEnvelope stored, PostVisibility? retained)
        {
            if (retained is not { } visibility)
                Remove(stored.Id);
            else if (visibility != stored.Visibility && !Removals.Contains(stored.Id))
                Resyncs[stored.Id] = visibility;
        }
    }
}

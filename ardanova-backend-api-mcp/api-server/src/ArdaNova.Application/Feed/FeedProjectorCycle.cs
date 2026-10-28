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

    /// <summary>One timed cycle: the look-back window, plus the bounded sweep when one is due.</summary>
    public Task<FeedProjectorRun> RunAsync(bool sweep, CancellationToken ct = default)
    {
        var now = Now();
        return ExecuteAsync(
            new Pass(Since: now - _options.LookBack, SweepSince: now - _options.SweepWindow, Sweep: sweep, DryRun: false),
            ct);
    }

    /// <summary>The same pass with no window: every source row ever written, and every envelope re-checked.</summary>
    public Task<FeedProjectorRun> BackfillAsync(bool dryRun, CancellationToken ct = default)
        => ExecuteAsync(new Pass(Since: null, SweepSince: null, Sweep: true, DryRun: dryRun), ct);

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
            await PlanSweepAsync(pass.SweepSince, plan, ct);
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

    /// <summary>Envelopes whose source was deleted leave no row for the window to see, so they are found from the envelope side.</summary>
    private async Task PlanSweepAsync(DateTime? since, Plan plan, CancellationToken ct)
    {
        var orphans = await _posts.Query()
            .Where(post => post.sourceType != null)
            .Where(FeedProjectionRules.HasLostItsTarget)
            .Select(post => post.id)
            .ToListAsync(ct);
        foreach (var orphan in orphans)
            plan.Remove(orphan);

        var recent = _posts.Query().Where(post => post.sourceType != null);
        if (since is { } sweepSince)
            recent = recent.Where(post => post.createdAt >= sweepSince);
        var stored = (await recent
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
            .Where(row => !plan.Removals.Contains(row.id))
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

    private async Task<FeedProjectorRun> ApplyAsync(Pass pass, Plan plan, CancellationToken ct)
    {
        var now = Now();
        var removed = await _envelopes.RemoveAsync(plan.Removals, ct);

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

    private sealed record Pass(DateTime? Since, DateTime? SweepSince, bool Sweep, bool DryRun);

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

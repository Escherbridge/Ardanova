namespace ArdaNova.Application.Feed;

using ArdaNova.Domain.Models.Enums;

/// <summary>Reads COMMITTED feedable source rows; the rules, not the reader, decide what qualifies.</summary>
public interface IFeedSourceReader
{
    /// <summary>
    /// The rows of one kind with a relevant timestamp at or after <paramref name="since"/>; null reads every row.
    /// One kind per call so the cycle can isolate a kind whose read fails.
    /// </summary>
    Task<IReadOnlyList<FeedSource>> ReadChangedSinceAsync(
        FeedSourceType kind,
        DateTime? since,
        CancellationToken ct = default);

    /// <summary>The current state of the named rows of one kind; a row that no longer exists is absent.</summary>
    Task<IReadOnlyList<FeedSource>> ReadByIdsAsync(
        FeedSourceType kind,
        IReadOnlyCollection<string> rowIds,
        CancellationToken ct = default);
}

/// <summary>Envelope persistence and the single-runner lock; see Infrastructure/Feed/AGENTS.md.</summary>
public interface IFeedEnvelopeStore
{
    /// <summary>Takes the projector's cross-process lock, or returns null while another runner holds it.</summary>
    Task<IAsyncDisposable?> TryAcquireRunnerLockAsync(CancellationToken ct = default);

    /// <summary>Inserts one envelope; a row already holding its (sourceType, sourceId) is "already projected".</summary>
    Task<FeedInsertOutcome> InsertAsync(FeedEnvelope envelope, DateTime projectedAt, CancellationToken ct = default);

    /// <summary>Deletes envelopes and their likes, replies, bookmarks, shares and media; never a person's post.</summary>
    Task<int> RemoveAsync(IReadOnlyCollection<string> envelopeIds, CancellationToken ct = default);

    /// <summary>Moves an envelope to the audience its source now has.</summary>
    Task<int> SetVisibilityAsync(
        string envelopeId,
        PostVisibility visibility,
        DateTime changedAt,
        CancellationToken ct = default);
}

/// <summary>What one envelope insert did.</summary>
public enum FeedInsertOutcome
{
    Inserted,

    /// <summary>The (sourceType, sourceId) unique index already holds this envelope: idempotent, not an error.</summary>
    AlreadyProjected,

    /// <summary>The author or a target row was deleted between the read and the insert.</summary>
    TargetGone,
}

public enum FeedProjectorOutcome
{
    Completed,

    /// <summary>Another runner holds the advisory lock; this pass wrote nothing.</summary>
    RunnerBusy,
}

/// <summary>What one projector pass did (or, for a dry run, would do).</summary>
/// <param name="SourceReadsFailed">Reads of one source kind that threw; those rows were skipped, never treated as deleted.</param>
/// <param name="CardsFailed">Single envelopes whose rule, insert, removal or re-audience threw; the rest went ahead.</param>
public sealed record FeedProjectorRun(
    FeedProjectorOutcome Outcome,
    bool DryRun,
    bool Swept,
    int SourcesExamined,
    int Projected,
    int AlreadyProjected,
    int TargetsGone,
    int Removed,
    int Resynced,
    IReadOnlyList<FeedEnvelope> PendingEnvelopes,
    int SourceReadsFailed = 0,
    int CardsFailed = 0)
{
    public static FeedProjectorRun RunnerBusy { get; } = new(
        FeedProjectorOutcome.RunnerBusy, false, false, 0, 0, 0, 0, 0, 0, []);

    /// <summary>True when the pass changed any row.</summary>
    public bool ChangedAnything => !DryRun && (Projected > 0 || Removed > 0 || Resynced > 0);

    /// <summary>True when part of the pass was skipped because something threw.</summary>
    public bool HadFailures => SourceReadsFailed > 0 || CardsFailed > 0;
}

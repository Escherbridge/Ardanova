namespace ArdaNova.Infrastructure.Feed;

using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

/// <inheritdoc/>
public sealed class FeedEnvelopeStore : IFeedEnvelopeStore
{
    /// <summary>Session advisory-lock key owned by the feed projector ("ARDNFEED" in ASCII).</summary>
    public const long RunnerLockKey = 0x4152_444E_4645_4544;

    private readonly ArdaNovaDbContext _context;
    private readonly ILogger<FeedEnvelopeStore> _logger;

    public FeedEnvelopeStore(ArdaNovaDbContext context, ILogger<FeedEnvelopeStore>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<FeedEnvelopeStore>.Instance;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A SESSION lock, so the connection is opened here and held until the handle is disposed: EF would otherwise
    /// hand each command a pooled connection and the lock would sit on whichever one ran it.
    /// </remarks>
    public async Task<IAsyncDisposable?> TryAcquireRunnerLockAsync(CancellationToken ct = default)
    {
        await _context.Database.OpenConnectionAsync(ct);
        try
        {
            var acquired = await _context.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_lock({RunnerLockKey}) AS \"Value\"")
                .SingleAsync(ct);
            if (acquired)
                return new RunnerLock(_context, _logger);
        }
        catch
        {
            await _context.Database.CloseConnectionAsync();
            throw;
        }

        await _context.Database.CloseConnectionAsync();
        return null;
    }

    /// <inheritdoc/>
    public async Task<FeedInsertOutcome> InsertAsync(
        FeedEnvelope envelope,
        DateTime projectedAt,
        CancellationToken ct = default)
    {
        var post = FeedProjectionRules.ToPost(envelope, projectedAt);
        _context.Posts.Add(post);
        try
        {
            await _context.SaveChangesAsync(ct);
            return FeedInsertOutcome.Inserted;
        }
        catch (DbUpdateException failure) when (HasSqlState(failure, PostgresErrorCodes.UniqueViolation))
        {
            // Another runner (or an earlier pass) already wrote this (sourceType, sourceId).
            return FeedInsertOutcome.AlreadyProjected;
        }
        catch (DbUpdateException failure) when (HasSqlState(failure, PostgresErrorCodes.ForeignKeyViolation))
        {
            return FeedInsertOutcome.TargetGone;
        }
        finally
        {
            _context.Entry(post).State = EntityState.Detached;
        }
    }

    /// <inheritdoc/>
    public async Task<int> RemoveAsync(IReadOnlyCollection<string> envelopeIds, CancellationToken ct = default)
    {
        if (envelopeIds.Count == 0)
            return 0;

        var ids = envelopeIds.ToArray();
        // The sourceType guard is the invariant: whatever id reaches this method, a person's post is never touched.
        var envelopes = _context.Posts.Where(post => ids.Contains(post.id) && post.sourceType != null);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        await _context.PostComments
            .Where(comment => envelopes.Any(post => post.id == comment.postId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(comment => comment.parentId, (string?)null), ct);
        await _context.PostComments.Where(row => envelopes.Any(post => post.id == row.postId)).ExecuteDeleteAsync(ct);
        await _context.PostLikes.Where(row => envelopes.Any(post => post.id == row.postId)).ExecuteDeleteAsync(ct);
        await _context.PostBookmarks.Where(row => envelopes.Any(post => post.id == row.postId)).ExecuteDeleteAsync(ct);
        await _context.PostShares.Where(row => envelopes.Any(post => post.id == row.postId)).ExecuteDeleteAsync(ct);
        await _context.PostMedias.Where(row => envelopes.Any(post => post.id == row.postId)).ExecuteDeleteAsync(ct);
        // The migration cascades Notification.postId; EF models it client-set-null, so an EF-built database would refuse.
        await _context.Notifications.Where(row => envelopes.Any(post => post.id == row.postId)).ExecuteDeleteAsync(ct);
        var removed = await envelopes.ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return removed;
    }

    /// <inheritdoc/>
    public Task<int> SetVisibilityAsync(
        string envelopeId,
        PostVisibility visibility,
        DateTime changedAt,
        CancellationToken ct = default)
        => _context.Posts
            .Where(post => post.id == envelopeId && post.sourceType != null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(post => post.visibility, visibility)
                .SetProperty(post => post.updatedAt, changedAt), ct);

    /// <inheritdoc/>
    /// <remarks>createdAt is never touched, so a card that comes back is not bumped as new.</remarks>
    public Task<int> RetractAsync(IReadOnlyCollection<string> envelopeIds, DateTime retractedAt, CancellationToken ct = default)
    {
        if (envelopeIds.Count == 0)
            return Task.FromResult(0);

        var ids = envelopeIds.ToArray();
        return _context.Posts
            .Where(post => ids.Contains(post.id) && post.sourceType != null && post.retractedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(post => post.retractedAt, retractedAt)
                .SetProperty(post => post.updatedAt, retractedAt), ct);
    }

    /// <inheritdoc/>
    public Task<int> RestoreAsync(
        string envelopeId,
        PostVisibility visibility,
        DateTime restoredAt,
        CancellationToken ct = default)
        => _context.Posts
            .Where(post => post.id == envelopeId && post.sourceType != null && post.retractedAt != null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(post => post.retractedAt, (DateTime?)null)
                .SetProperty(post => post.visibility, visibility)
                .SetProperty(post => post.updatedAt, restoredAt), ct);

    /// <inheritdoc/>
    public Task<int> SetHashtagsAsync(string envelopeId, string[] hashtags, CancellationToken ct = default)
        => _context.Posts
            .Where(post => post.id == envelopeId && post.sourceType != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(post => post.hashtags, hashtags), ct);

    /// <inheritdoc/>
    /// <remarks>updatedAt is left alone: a score is derived state, not an edit to the post.</remarks>
    public Task<int> SetQualityAsync(string postId, decimal quality, DateTime computedAt, CancellationToken ct = default)
        => _context.Posts
            .Where(post => post.id == postId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(post => post.trendingScore, quality)
                .SetProperty(post => post.trendingAt, computedAt), ct);

    private static bool HasSqlState(DbUpdateException failure, string sqlState)
        => failure.InnerException is PostgresException postgres && postgres.SqlState == sqlState;

    /// <summary>Releases the advisory lock, then the connection it lives on; never throws (see Feed/AGENTS.md).</summary>
    private sealed class RunnerLock : IAsyncDisposable
    {
        private readonly ArdaNovaDbContext _context;
        private readonly ILogger _logger;

        public RunnerLock(ArdaNovaDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            // Disposal runs while an exception (often the shutdown's cancellation) may be unwinding; a throw here
            // would replace it. A session lock dies with its connection, so a failed unlock only needs a log line.
            try
            {
                await _context.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({RunnerLockKey})");
            }
            catch (Exception failure)
            {
                _logger.LogWarning(
                    failure,
                    "Feed projector: releasing the runner lock failed; the server frees it when this connection closes.");
            }

            try
            {
                await _context.Database.CloseConnectionAsync();
            }
            catch (Exception failure)
            {
                _logger.LogWarning(failure, "Feed projector: closing the runner lock's connection failed.");
            }
        }
    }
}

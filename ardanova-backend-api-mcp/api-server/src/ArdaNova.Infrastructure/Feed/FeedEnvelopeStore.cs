namespace ArdaNova.Infrastructure.Feed;

using ArdaNova.Application.Feed;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <inheritdoc/>
public sealed class FeedEnvelopeStore : IFeedEnvelopeStore
{
    /// <summary>Session advisory-lock key owned by the feed projector ("ARDNFEED" in ASCII).</summary>
    public const long RunnerLockKey = 0x4152_444E_4645_4544;

    private readonly ArdaNovaDbContext _context;

    public FeedEnvelopeStore(ArdaNovaDbContext context)
    {
        _context = context;
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
                return new RunnerLock(_context);
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

    private static bool HasSqlState(DbUpdateException failure, string sqlState)
        => failure.InnerException is PostgresException postgres && postgres.SqlState == sqlState;

    /// <summary>Releases the advisory lock, then the connection it lives on.</summary>
    private sealed class RunnerLock : IAsyncDisposable
    {
        private readonly ArdaNovaDbContext _context;

        public RunnerLock(ArdaNovaDbContext context)
        {
            _context = context;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({RunnerLockKey})");
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}

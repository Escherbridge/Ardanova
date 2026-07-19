namespace ArdaNova.Infrastructure.Security;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

/// <inheritdoc/>
public sealed class ActorAssertionReplayCleanupStore : IActorAssertionReplayCleanupStore
{
    private const string PurgeBatchSql = """
        WITH candidates AS (
            SELECT "jti"
            FROM "ActorAssertionReplay"
            WHERE "expiresAt" < @expiresBefore
            ORDER BY "expiresAt", "jti"
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
        )
        DELETE FROM "ActorAssertionReplay" AS replay
        USING candidates
        WHERE replay."jti" = candidates."jti";
        """;

    private readonly ArdaNovaDbContext _context;

    public ActorAssertionReplayCleanupStore(ArdaNovaDbContext context)
    {
        _context = context;
    }

    public Task<int> PurgeBatchAsync(
        DateTime expiresBefore,
        int batchSize,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(batchSize, 5_000);

        var cutoffParameter = new NpgsqlParameter("expiresBefore", NpgsqlDbType.Timestamp)
        {
            Value = DateTime.SpecifyKind(expiresBefore, DateTimeKind.Unspecified)
        };
        var batchParameter = new NpgsqlParameter("batchSize", NpgsqlDbType.Integer)
        {
            Value = batchSize
        };

        return _context.Database.ExecuteSqlRawAsync(
            PurgeBatchSql,
            new object[] { cutoffParameter, batchParameter },
            ct);
    }
}

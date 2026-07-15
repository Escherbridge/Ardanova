namespace ArdaNova.Infrastructure.Security;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <inheritdoc/>
public sealed class ActorAssertionReplayLedger : IActorAssertionReplayLedger
{
    private readonly ArdaNovaDbContext _context;

    public ActorAssertionReplayLedger(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<ActorAssertionReplayClaim> TryConsumeAsync(
        ActorAssertionReplayEntry entry,
        CancellationToken ct = default)
    {
        var replay = new ActorAssertionReplay
        {
            jti = entry.Jti,
            expiresAt = entry.ExpiresAt,
            consumedAt = entry.ConsumedAt,
            subject = entry.Subject,
            requestTarget = entry.RequestTarget,
            bodySha256 = entry.BodySha256
        };

        await _context.ActorAssertionReplays.AddAsync(replay, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
            return ActorAssertionReplayClaim.Consumed;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(replay).State = EntityState.Detached;
            return ActorAssertionReplayClaim.Replay;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}

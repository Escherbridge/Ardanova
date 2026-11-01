namespace ArdaNova.Infrastructure.Tasks;

using ArdaNova.Application.Tasks;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <inheritdoc/>
public sealed class TaskClaimStore : ITaskClaimStore
{
    private readonly ArdaNovaDbContext _context;

    public TaskClaimStore(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Compare-and-set: two racing claims both reach this UPDATE, the second waits on the row lock and, once the
    /// first commits, Postgres re-checks the WHERE against the new row and writes nothing. Runs inside the caller's
    /// transaction when one is open. The WHERE mirrors <see cref="TaskClaimRule.Evaluate"/>'s blocking facts; the
    /// real-Postgres agreement table in TaskClaimStoreAgreementTests keeps the two from drifting.
    /// </remarks>
    public Task<int> TryClaimAsync(string taskId, string userId, CancellationToken ct = default)
    {
        // A parameter, not now(): updatedAt is written from the app clock everywhere else.
        var claimedAt = DateTime.UtcNow;
        return _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "ProjectTask" AS task
            SET "assignedToId" = {userId}, "updatedAt" = {claimedAt}
            WHERE task."id" = {taskId}
              AND task."assignedToId" IS NULL
              AND task."status" <> 'COMPLETED'
              AND task."escrowStatus" = 'NONE'
              AND NOT EXISTS (
                SELECT 1 FROM "TaskCommerceAgreement" AS agreement WHERE agreement."taskId" = task."id")
            """,
            ct);
    }
}

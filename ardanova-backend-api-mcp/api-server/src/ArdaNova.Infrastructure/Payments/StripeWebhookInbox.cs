namespace ArdaNova.Infrastructure.Payments;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <inheritdoc/>
public sealed class StripeWebhookInbox : IStripeWebhookInbox
{
    private readonly ArdaNovaDbContext _context;

    public StripeWebhookInbox(ArdaNovaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<StripeWebhookClaim> TryClaimAsync(
        string eventId,
        string eventType,
        TimeSpan processingLease,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (processingLease <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(processingLease));

        var now = DateTime.UtcNow;
        var receipt = await _context.StripeWebhookEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.id == eventId, ct);

        if (receipt is not null)
            return await ResolveExistingClaimAsync(receipt, now, processingLease, ct);

        var insertedReceipt = new StripeWebhookEvent
        {
            id = eventId,
            eventType = eventType,
            status = StripeWebhookEventStatus.PROCESSING,
            attemptCount = 1,
            receivedAt = now,
            processingLeaseExpiresAt = now.Add(processingLease)
        };

        await _context.StripeWebhookEvents.AddAsync(insertedReceipt, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
            return StripeWebhookClaim.Claimed;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(insertedReceipt).State = EntityState.Detached;
            return await ResolveConcurrentClaimAsync(eventId, now, processingLease, ct);
        }
    }

    /// <inheritdoc/>
    public async Task MarkCompletedAsync(string eventId, CancellationToken ct = default)
    {
        var updated = await _context.StripeWebhookEvents
            .Where(item => item.id == eventId && item.status == StripeWebhookEventStatus.PROCESSING)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, StripeWebhookEventStatus.COMPLETED)
                .SetProperty(item => item.completedAt, DateTime.UtcNow), ct);

        EnsureClaimedEventWasUpdated(eventId, updated);
    }

    /// <inheritdoc/>
    public async Task MarkFailedAsync(string eventId, CancellationToken ct = default)
    {
        var updated = await _context.StripeWebhookEvents
            .Where(item => item.id == eventId && item.status == StripeWebhookEventStatus.PROCESSING)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, StripeWebhookEventStatus.FAILED)
                .SetProperty(item => item.lastFailedAt, DateTime.UtcNow), ct);

        EnsureClaimedEventWasUpdated(eventId, updated);
    }

    private async Task<StripeWebhookClaim> ResolveExistingClaimAsync(
        StripeWebhookEvent receipt,
        DateTime now,
        TimeSpan processingLease,
        CancellationToken ct)
    {
        if (receipt.status == StripeWebhookEventStatus.COMPLETED)
            return StripeWebhookClaim.AlreadyCompleted;

        if (receipt.status == StripeWebhookEventStatus.PROCESSING
            && receipt.processingLeaseExpiresAt > now)
        {
            return StripeWebhookClaim.InProgress;
        }

        var reclaimed = await _context.StripeWebhookEvents
            .Where(item => item.id == receipt.id
                && (item.status == StripeWebhookEventStatus.FAILED
                    || (item.status == StripeWebhookEventStatus.PROCESSING
                        && item.processingLeaseExpiresAt <= now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.status, StripeWebhookEventStatus.PROCESSING)
                .SetProperty(item => item.eventType, receipt.eventType)
                .SetProperty(item => item.attemptCount, item => item.attemptCount + 1)
                .SetProperty(item => item.processingLeaseExpiresAt, now.Add(processingLease)), ct);

        if (reclaimed == 1)
            return StripeWebhookClaim.Claimed;

        return await ResolveConcurrentClaimAsync(receipt.id, now, processingLease, ct);
    }

    private async Task<StripeWebhookClaim> ResolveConcurrentClaimAsync(
        string eventId,
        DateTime now,
        TimeSpan processingLease,
        CancellationToken ct)
    {
        var receipt = await _context.StripeWebhookEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.id == eventId, ct)
            ?? throw new InvalidOperationException("Stripe webhook receipt disappeared while being claimed.");

        if (receipt.status == StripeWebhookEventStatus.COMPLETED)
            return StripeWebhookClaim.AlreadyCompleted;

        if (receipt.status == StripeWebhookEventStatus.PROCESSING
            && receipt.processingLeaseExpiresAt > now)
        {
            return StripeWebhookClaim.InProgress;
        }

        return await ResolveExistingClaimAsync(receipt, now, processingLease, ct);
    }

    private static void EnsureClaimedEventWasUpdated(string eventId, int updated)
    {
        if (updated != 1)
            throw new InvalidOperationException($"Stripe webhook event '{eventId}' was not actively claimed.");
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}

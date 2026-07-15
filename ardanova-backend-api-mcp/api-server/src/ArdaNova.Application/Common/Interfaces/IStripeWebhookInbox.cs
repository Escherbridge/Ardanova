namespace ArdaNova.Application.Common.Interfaces;

/// <summary>
/// Provides durable, provider-event idempotency for verified Stripe deliveries.
/// </summary>
public interface IStripeWebhookInbox
{
    /// <summary>
    /// Atomically claims a verified Stripe event or reports its prior delivery state.
    /// </summary>
    Task<StripeWebhookClaim> TryClaimAsync(
        string eventId,
        string eventType,
        TimeSpan processingLease,
        CancellationToken ct = default);

    /// <summary>Marks a claimed delivery as fully processed.</summary>
    Task MarkCompletedAsync(string eventId, CancellationToken ct = default);

    /// <summary>Releases a claimed delivery for a provider retry after processing failed.</summary>
    Task MarkFailedAsync(string eventId, CancellationToken ct = default);
}

/// <summary>Result of attempting to claim a verified Stripe delivery.</summary>
public enum StripeWebhookClaim
{
    Claimed,
    AlreadyCompleted,
    InProgress
}

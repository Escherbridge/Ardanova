namespace ArdaNova.Application.Services.Interfaces;

/// <summary>Creates and retrieves provider checkout sessions behind a testable payment boundary.</summary>
public interface IStripeCheckoutGateway
{
    /// <summary>Creates or replays a Stripe Checkout session using the supplied provider idempotency key.</summary>
    Task<StripeCheckoutSession> CreateAsync(StripeCheckoutRequest request, CancellationToken ct = default);

    /// <summary>Retrieves the current redirect URL for an existing provider checkout session.</summary>
    Task<string> GetUrlAsync(string checkoutSessionId, CancellationToken ct = default);
}

/// <summary>Immutable provider checkout request assembled from a committed FundingIntent.</summary>
public sealed record StripeCheckoutRequest(
    string FundingIntentId,
    string ProjectTokenConfigId,
    string AssetName,
    long AmountInMinorUnits,
    string CurrencyCode,
    string IdempotencyKey);

/// <summary>Provider checkout identity and server-created redirect URL.</summary>
public sealed record StripeCheckoutSession(string Id, string Url);

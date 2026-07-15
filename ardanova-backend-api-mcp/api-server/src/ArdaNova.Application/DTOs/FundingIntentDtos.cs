namespace ArdaNova.Application.DTOs;

/// <summary>Creates a server-owned funding intent for a specific project token configuration.</summary>
public sealed record CreateFundingIntentDto
{
    public required string ProjectTokenConfigId { get; init; }
    public required string Amount { get; init; }
    public required string DisclosureVersion { get; init; }
}

/// <summary>Opaque checkout handoff for an actor-owned funding intent.</summary>
public sealed record FundingCheckoutDto
{
    public required string IntentId { get; init; }
    public required string CheckoutUrl { get; init; }
}

/// <summary>Actor-scoped durable funding state; redirects and query parameters are not inputs.</summary>
public sealed record FundingIntentStatusDto
{
    public required string IntentId { get; init; }
    public required string ProjectTokenConfigId { get; init; }
    public required string CurrencyCode { get; init; }
    public required string Amount { get; init; }
    public required string Status { get; init; }
    public DateTime? PaymentVerifiedAt { get; init; }
}

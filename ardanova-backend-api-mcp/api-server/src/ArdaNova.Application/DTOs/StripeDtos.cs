namespace ArdaNova.Application.DTOs;

// ==================== Stripe DTOs ====================

/// <summary>
/// Result of creating a Stripe Checkout session for project funding.
/// </summary>
public record StripeCheckoutSessionDto
{
    public string SessionId { get; init; } = string.Empty;
    public string SessionUrl { get; init; } = string.Empty;
    public string ProjectTokenConfigId { get; init; } = string.Empty;
    public double UsdAmount { get; init; }
}

/// <summary>
/// Result of creating a Stripe Connected Account for user payouts.
/// </summary>
public record StripeConnectedAccountDto
{
    public string AccountId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string OnboardingUrl { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Result of creating a Stripe Connect transfer for payout.
/// </summary>
public record StripeTransferDto
{
    public string TransferId { get; init; } = string.Empty;
    public string PayoutRequestId { get; init; } = string.Empty;
    public double UsdAmount { get; init; }
    public string Status { get; init; } = string.Empty;
}

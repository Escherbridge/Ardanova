namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Stripe SDK wrapper for crowdfunding inflow (Checkout) and payout outflow (Connect).
/// </summary>
public interface IStripeService
{
    // --- Crowdfunding (Inflow) ---

    /// <summary>
    /// Creates a Stripe Checkout session for project funding.
    /// </summary>
    Task<Result<StripeCheckoutSessionDto>> CreateCheckoutSessionAsync(
        string projectTokenConfigId,
        string userId,
        double usdAmount,
        CancellationToken ct = default);

    /// <summary>
    /// Handles successful payment webhook from Stripe (payment_intent.succeeded).
    /// Creates ProjectInvestment, allocates tokens, credits balance, processes treasury inflow, evaluates Gate 1.
    /// </summary>
    Task<Result<ProjectInvestmentDto>> HandlePaymentSucceededAsync(
        string paymentIntentId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles failed payment webhook from Stripe (payment_intent.payment_failed).
    /// Logs failure, no side effects.
    /// </summary>
    Task<Result<bool>> HandlePaymentFailedAsync(
        string paymentIntentId,
        string failureReason,
        CancellationToken ct = default);

    // --- Stripe Connect (Outflow) ---

    /// <summary>
    /// Creates a Stripe Connected Account for a user to receive payouts.
    /// </summary>
    Task<Result<StripeConnectedAccountDto>> CreateConnectedAccountAsync(
        string userId,
        string email,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a payout transfer to a connected account via Stripe Connect.
    /// </summary>
    Task<Result<StripeTransferDto>> CreatePayoutTransferAsync(
        string payoutRequestId,
        string connectedAccountId,
        double usdAmount,
        CancellationToken ct = default);

    /// <summary>
    /// Handles successful payout webhook from Stripe (transfer.paid).
    /// Updates PayoutRequest status to COMPLETED.
    /// </summary>
    Task<Result<PayoutRequestDto>> HandlePayoutSucceededAsync(
        string transferId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles failed payout webhook from Stripe (transfer.failed).
    /// Updates PayoutRequest status to FAILED, unlocks tokens, logs failure reason.
    /// </summary>
    Task<Result<PayoutRequestDto>> HandlePayoutFailedAsync(
        string transferId,
        string failureReason,
        CancellationToken ct = default);
}

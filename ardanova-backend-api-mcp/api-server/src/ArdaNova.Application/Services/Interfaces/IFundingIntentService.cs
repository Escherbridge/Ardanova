namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using Stripe;

/// <summary>Owns durable funding intent creation, status reads, and payment-verification recording.</summary>
public interface IFundingIntentService
{
    /// <summary>Commits an actor-owned intent before creating or replaying its provider checkout session.</summary>
    Task<Result<FundingCheckoutDto>> CreateCheckoutAsync(
        CreateFundingIntentDto request,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>Reads durable funding state only when the actor owns the intent.</summary>
    Task<Result<FundingIntentStatusDto>> GetStatusAsync(
        string intentId,
        string actorId,
        CancellationToken ct = default);

    /// <summary>Records a signed provider success with one pending local settlement and no value dispatch.</summary>
    Task<Result<bool>> RecordPaymentSucceededAsync(
        string providerEventId,
        PaymentIntent paymentIntent,
        CancellationToken ct = default);

    /// <summary>Records a signed provider payment failure without releasing any economic effect.</summary>
    Task<Result<bool>> RecordPaymentFailedAsync(
        PaymentIntent paymentIntent,
        CancellationToken ct = default);
}

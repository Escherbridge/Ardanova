namespace ArdaNova.Application.Services.Interfaces;

using Stripe;

/// <summary>Retrieves Stripe payment intents for verified webhook processing.</summary>
public interface IStripePaymentIntentGateway
{
    Task<PaymentIntent?> GetAsync(string paymentIntentId, CancellationToken ct = default);
}

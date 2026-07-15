namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Services.Interfaces;
using Stripe;

/// <inheritdoc/>
public sealed class StripePaymentIntentGateway : IStripePaymentIntentGateway
{
    /// <inheritdoc/>
    public Task<PaymentIntent?> GetAsync(string paymentIntentId, CancellationToken ct = default)
        => new PaymentIntentService().GetAsync(paymentIntentId, cancellationToken: ct);
}

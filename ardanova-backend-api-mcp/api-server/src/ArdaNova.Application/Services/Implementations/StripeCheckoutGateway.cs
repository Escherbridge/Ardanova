namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using StripeCheckoutSessionService = Stripe.Checkout.SessionService;

/// <inheritdoc/>
public sealed class StripeCheckoutGateway : IStripeCheckoutGateway
{
    private readonly IConfiguration _configuration;

    public StripeCheckoutGateway(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task<StripeCheckoutSession> CreateAsync(StripeCheckoutRequest request, CancellationToken ct = default)
    {
        var successUrl = RequiredUrl("Stripe:SuccessUrl");
        var cancelUrl = RequiredUrl("Stripe:CancelUrl");
        var metadata = new Dictionary<string, string>
        {
            ["fundingIntentId"] = request.FundingIntentId,
            ["projectTokenConfigId"] = request.ProjectTokenConfigId,
        };
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode,
                        UnitAmount = request.AmountInMinorUnits,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Fund Project: {request.AssetName}",
                        },
                    },
                    Quantity = 1,
                },
            ],
            Metadata = metadata,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>(metadata),
            },
        };
        var session = await new StripeCheckoutSessionService().CreateAsync(
            options,
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            ct);
        return ToSession(session);
    }

    /// <inheritdoc/>
    public async Task<string> GetUrlAsync(string checkoutSessionId, CancellationToken ct = default)
    {
        var session = await new StripeCheckoutSessionService().GetAsync(checkoutSessionId, cancellationToken: ct);
        return ToSession(session).Url;
    }

    private string RequiredUrl(string key)
        => _configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{key} must be configured before funding checkout can be created.");

    private static StripeCheckoutSession ToSession(Session? session)
    {
        if (session is null || string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
            throw new InvalidOperationException("Stripe returned an incomplete checkout session.");

        return new StripeCheckoutSession(session.Id, session.Url);
    }
}

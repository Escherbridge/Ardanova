namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

/// <summary>Receives signed Stripe payment and payout events.</summary>
[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private const int DefaultProcessingLeaseSeconds = 300;
    private const int MinimumProcessingLeaseSeconds = 30;
    private const int MaximumProcessingLeaseSeconds = 900;

    private readonly IStripeService _stripeService;
    private readonly IFundingIntentService _fundingIntentService;
    private readonly IStripeWebhookInbox _inbox;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IStripeService stripeService,
        IFundingIntentService fundingIntentService,
        IStripeWebhookInbox inbox,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _stripeService = stripeService;
        _fundingIntentService = fundingIntentService;
        _inbox = inbox;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Authenticates and processes a Stripe event exactly once per provider event id.</summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogError("Stripe webhook ingress is unavailable because its signing secret is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signature)
            || string.IsNullOrWhiteSpace(signature))
        {
            return BadRequest();
        }

        var payload = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Rejected Stripe webhook with an invalid signature.");
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(stripeEvent.Id) || string.IsNullOrWhiteSpace(stripeEvent.Type))
            return BadRequest();

        var claim = await _inbox.TryClaimAsync(
            stripeEvent.Id,
            stripeEvent.Type,
            GetProcessingLease(),
            ct);
        if (claim == StripeWebhookClaim.AlreadyCompleted)
            return Ok();

        if (claim == StripeWebhookClaim.InProgress)
        {
            Response.Headers["Retry-After"] = "10";
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            await DispatchAsync(stripeEvent, ct);
            await _inbox.MarkCompletedAsync(stripeEvent.Id, ct);
            return Ok();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The processing lease makes an abandoned request eligible for a later retry.
            throw;
        }
        catch
        {
            await _inbox.MarkFailedAsync(stripeEvent.Id, CancellationToken.None);
            throw;
        }
    }

    private async Task DispatchAsync(Event stripeEvent, CancellationToken ct)
    {
        switch (stripeEvent.Type)
        {
            case EventTypes.PaymentIntentSucceeded:
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent
                    ?? throw new StripeWebhookDeliveryException(stripeEvent.Id, stripeEvent.Type);
                var result = await _fundingIntentService.RecordPaymentSucceededAsync(
                    stripeEvent.Id,
                    paymentIntent,
                    ct);
                EnsureSucceeded(result.IsSuccess, stripeEvent);
                break;
            }

            case EventTypes.PaymentIntentPaymentFailed:
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent
                    ?? throw new StripeWebhookDeliveryException(stripeEvent.Id, stripeEvent.Type);
                var result = await _fundingIntentService.RecordPaymentFailedAsync(paymentIntent, ct);
                EnsureSucceeded(result.IsSuccess, stripeEvent);
                break;
            }

            case EventTypes.TransferReversed:
            {
                var transfer = stripeEvent.Data.Object as Transfer
                    ?? throw new StripeWebhookDeliveryException(stripeEvent.Id, stripeEvent.Type);
                var result = await _stripeService.HandlePayoutFailedAsync(transfer.Id, "Transfer reversed", ct);
                EnsureSucceeded(result.IsSuccess, stripeEvent);
                break;
            }

            case EventTypes.TransferUpdated:
            {
                var transfer = stripeEvent.Data.Object as Transfer
                    ?? throw new StripeWebhookDeliveryException(stripeEvent.Id, stripeEvent.Type);
                if (!transfer.Reversed && transfer.DestinationPayment is not null)
                {
                    var result = await _stripeService.HandlePayoutSucceededAsync(transfer.Id, ct);
                    EnsureSucceeded(result.IsSuccess, stripeEvent);
                }

                break;
            }

            case EventTypes.TransferCreated:
            default:
                break;
        }
    }

    private TimeSpan GetProcessingLease()
    {
        var configuredSeconds = _configuration.GetValue<int?>("Stripe:WebhookProcessingLeaseSeconds")
            ?? DefaultProcessingLeaseSeconds;
        var leaseSeconds = Math.Clamp(
            configuredSeconds,
            MinimumProcessingLeaseSeconds,
            MaximumProcessingLeaseSeconds);
        return TimeSpan.FromSeconds(leaseSeconds);
    }

    private static void EnsureSucceeded(bool succeeded, Event stripeEvent)
    {
        if (!succeeded)
            throw new StripeWebhookDeliveryException(stripeEvent.Id, stripeEvent.Type);
    }

    private sealed class StripeWebhookDeliveryException : Exception
    {
        public StripeWebhookDeliveryException(string eventId, string eventType)
            : base($"Stripe webhook '{eventId}' ({eventType}) could not be processed.")
        {
        }
    }
}

namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

/// <summary>
/// Stripe webhook handler for payment and payout events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IStripeService stripeService,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _stripeService = stripeService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint for Stripe events.
    /// Handles: payment_intent.succeeded, payment_intent.payment_failed, transfer.paid, transfer.failed
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        try
        {
            // Read raw body
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            // Get Stripe signature from header
            var signature = Request.Headers["Stripe-Signature"].ToString();
            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("Stripe webhook secret not configured");
                return BadRequest("Webhook secret not configured");
            }

            // Construct and validate event
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Invalid Stripe webhook signature");
                return BadRequest("Invalid signature");
            }

            _logger.LogInformation("Received Stripe webhook event: {EventType}", stripeEvent.Type);

            // Dispatch by event type
            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        var result = await _stripeService.HandlePaymentSucceededAsync(paymentIntent.Id, ct);
                        if (!result.IsSuccess)
                        {
                            _logger.LogError("Failed to handle payment succeeded: {Error}", result.Error);
                        }
                    }
                    break;

                case Events.PaymentIntentPaymentFailed:
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (failedIntent != null)
                    {
                        var errorMessage = failedIntent.LastPaymentError?.Message ?? "Unknown error";
                        await _stripeService.HandlePaymentFailedAsync(failedIntent.Id, errorMessage, ct);
                    }
                    break;

                case Events.TransferCreated:
                    // Transfer created successfully, no action needed (handled in CreatePayoutTransferAsync)
                    _logger.LogInformation("Transfer created: {TransferId}", (stripeEvent.Data.Object as Transfer)?.Id);
                    break;

                case Events.TransferReversed:
                    // Transfer was reversed (payout failed)
                    var reversedTransfer = stripeEvent.Data.Object as Transfer;
                    if (reversedTransfer != null)
                    {
                        await _stripeService.HandlePayoutFailedAsync(
                            reversedTransfer.Id,
                            "Transfer reversed",
                            ct);
                    }
                    break;

                case Events.TransferUpdated:
                    // Transfer status updated - check if it succeeded
                    var updatedTransfer = stripeEvent.Data.Object as Transfer;
                    if (updatedTransfer != null)
                    {
                        // Transfer is considered complete when reversed == false and destination_payment exists
                        if (!updatedTransfer.Reversed && updatedTransfer.DestinationPayment != null)
                        {
                            await _stripeService.HandlePayoutSucceededAsync(updatedTransfer.Id, ct);
                        }
                    }
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, ex.Message);
        }
    }
}

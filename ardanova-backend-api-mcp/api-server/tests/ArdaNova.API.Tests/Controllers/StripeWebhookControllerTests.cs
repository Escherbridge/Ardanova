namespace ArdaNova.API.Tests.Controllers;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;

public class StripeWebhookControllerTests
{
    private const string WebhookSecret = "whsec_test_secret";

    [Fact]
    public async Task Webhook_VerifiedDelivery_ClaimsDispatchesAndCompletes()
    {
        var stripeService = new Mock<IStripeService>();
        var inbox = new Mock<IStripeWebhookInbox>();
        inbox.Setup(store => store.TryClaimAsync(
                "evt_123",
                "payment_intent.payment_failed",
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StripeWebhookClaim.Claimed);

        var controller = CreateController(stripeService.Object, inbox.Object, WebhookSecret, "evt_123");

        var result = await controller.Webhook(CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        inbox.Verify(store => store.MarkCompletedAsync("evt_123", It.IsAny<CancellationToken>()), Times.Once);
        stripeService.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task Webhook_VerifiedFundingDelivery_AcknowledgesOnlyAfterPendingWriterSucceeds()
    {
        var stripeService = new Mock<IStripeService>(MockBehavior.Strict);
        var inbox = new Mock<IStripeWebhookInbox>();
        inbox.Setup(store => store.TryClaimAsync(
                "evt_funding_123",
                EventTypes.PaymentIntentSucceeded,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StripeWebhookClaim.Claimed);
        var fundingWriterCalled = false;
        var controller = CreateController(
            stripeService.Object,
            inbox.Object,
            WebhookSecret,
            "evt_funding_123",
            eventType: EventTypes.PaymentIntentSucceeded,
            configureFundingService: service => service
                .Setup(item => item.RecordPaymentSucceededAsync(
                    "evt_funding_123",
                    It.IsAny<PaymentIntent>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => fundingWriterCalled = true)
                .ReturnsAsync(Result<bool>.Success(true)));

        var result = await controller.Webhook(CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        fundingWriterCalled.Should().BeTrue();
        inbox.Verify(store => store.MarkCompletedAsync("evt_funding_123", It.IsAny<CancellationToken>()), Times.Once);
        stripeService.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task Webhook_CompletedDuplicate_IsAcknowledgedWithoutRedispatch()
    {
        var stripeService = new Mock<IStripeService>(MockBehavior.Strict);
        var inbox = new Mock<IStripeWebhookInbox>();
        inbox.Setup(store => store.TryClaimAsync(
                "evt_123",
                "payment_intent.payment_failed",
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StripeWebhookClaim.AlreadyCompleted);
        var controller = CreateController(stripeService.Object, inbox.Object, WebhookSecret, "evt_123");

        var result = await controller.Webhook(CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        inbox.Verify(store => store.MarkCompletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Webhook_ActiveDuplicate_ReturnsRetryableServiceUnavailable()
    {
        var stripeService = new Mock<IStripeService>(MockBehavior.Strict);
        var inbox = new Mock<IStripeWebhookInbox>();
        inbox.Setup(store => store.TryClaimAsync(
                "evt_123",
                "payment_intent.payment_failed",
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StripeWebhookClaim.InProgress);
        var controller = CreateController(stripeService.Object, inbox.Object, WebhookSecret, "evt_123");

        var result = await controller.Webhook(CancellationToken.None);

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        controller.Response.Headers["Retry-After"].ToString().Should().Be("10");
    }

    [Fact]
    public async Task Webhook_HandlerFailure_MarksReceiptFailedForProviderRetry()
    {
        var stripeService = new Mock<IStripeService>();
        var inbox = new Mock<IStripeWebhookInbox>();
        inbox.Setup(store => store.TryClaimAsync(
                "evt_123",
                "payment_intent.payment_failed",
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StripeWebhookClaim.Claimed);
        var controller = CreateController(
            stripeService.Object,
            inbox.Object,
            WebhookSecret,
            "evt_123",
            fundingResult: Result<bool>.Failure("temporary downstream failure"));

        var action = () => controller.Webhook(CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
        inbox.Verify(store => store.MarkFailedAsync("evt_123", CancellationToken.None), Times.Once);
        inbox.Verify(store => store.MarkCompletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ReturnsBadRequestBeforeClaiming()
    {
        var stripeService = new Mock<IStripeService>(MockBehavior.Strict);
        var inbox = new Mock<IStripeWebhookInbox>(MockBehavior.Strict);
        var controller = CreateController(stripeService.Object, inbox.Object, WebhookSecret, "evt_123", "invalid");

        var result = await controller.Webhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Webhook_MissingSigningSecret_ReturnsRetryableServiceUnavailable()
    {
        var controller = CreateController(
            new Mock<IStripeService>(MockBehavior.Strict).Object,
            new Mock<IStripeWebhookInbox>(MockBehavior.Strict).Object,
            webhookSecret: null,
            eventId: "evt_123");

        var result = await controller.Webhook(CancellationToken.None);

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task ApiKeyMiddleware_LeavesStripeWebhookToSignatureVerification()
    {
        var invoked = false;
        var middleware = new ApiKeyMiddleware(
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            new ConfigurationBuilder().Build(),
            NullLogger<ApiKeyMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/StripeWebhook/webhook";

        await middleware.InvokeAsync(context);

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task ApiKeyMiddleware_StillRejectsOtherUnauthenticatedApiRoutes()
    {
        var middleware = new ApiKeyMiddleware(
            _ => Task.CompletedTask,
            new ConfigurationBuilder().Build(),
            NullLogger<ApiKeyMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/Projects";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    private static StripeWebhookController CreateController(
        IStripeService stripeService,
        IStripeWebhookInbox inbox,
        string? webhookSecret,
        string eventId,
        string? signature = null,
        Result<bool>? fundingResult = null,
        string eventType = EventTypes.PaymentIntentPaymentFailed,
        Action<Mock<IFundingIntentService>>? configureFundingService = null)
    {
        var apiVersion = StripeConfiguration.ApiVersion;
        var payload = $$"""
            {
              "id": "{{eventId}}",
              "object": "event",
              "api_version": "{{apiVersion}}",
              "created": 1735689600,
              "livemode": false,
              "pending_webhooks": 1,
              "request": { "id": "req_123", "idempotency_key": null },
              "data": { "object": { "id": "pi_123", "object": "payment_intent" } },
              "type": "{{eventType}}"
            }
            """;
        var values = new Dictionary<string, string?>();
        if (webhookSecret is not null)
            values["Stripe:WebhookSecret"] = webhookSecret;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        httpContext.Request.Headers["Stripe-Signature"] = signature
            ?? (webhookSecret is null ? "unused" : CreateSignature(payload, webhookSecret));
        var fundingIntentService = new Mock<IFundingIntentService>();
        fundingIntentService
            .Setup(service => service.RecordPaymentFailedAsync(
                It.IsAny<PaymentIntent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fundingResult ?? Result<bool>.Success(false));
        configureFundingService?.Invoke(fundingIntentService);
        var controller = new StripeWebhookController(
            stripeService,
            fundingIntentService.Object,
            inbox,
            configuration,
            NullLogger<StripeWebhookController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        return controller;
    }

    private static string CreateSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }
}

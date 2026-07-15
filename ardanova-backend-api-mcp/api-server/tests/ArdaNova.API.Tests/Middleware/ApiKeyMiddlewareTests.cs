using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArdaNova.API.Tests.Middleware;

public class ApiKeyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithAdminKey_AssignsAdminClaim()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "service-key";
        context.Request.Headers["X-Admin-Api-Key"] = "admin-key";
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.User.Identity?.IsAuthenticated.Should().BeTrue();
        context.User.IsInRole(ApiKeyMiddleware.AdminRole).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithoutAdminKey_AssignsOnlyServiceClaim()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "service-key";

        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.User.Identity?.IsAuthenticated.Should().BeTrue();
        context.User.IsInRole(ApiKeyMiddleware.AdminRole).Should().BeFalse();
        context.User.IsInRole("Service").Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidServiceKey_RejectsBeforeAssigningClaims()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "invalid-key";
        var nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        (context.User.Identity?.IsAuthenticated ?? false).Should().BeFalse();
    }

    [Fact]
    public async Task AdminPolicy_RequiresAdminClaim()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddArdaNovaAuthorization();
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, ApiKeyMiddleware.AdminRole)], "ApiKey"));
        var service = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Service")], "ApiKey"));

        var adminResult = await authorization.AuthorizeAsync(admin, null, AuthorizationPolicies.AdminApiKey);
        var serviceResult = await authorization.AuthorizeAsync(service, null, AuthorizationPolicies.AdminApiKey);

        adminResult.Succeeded.Should().BeTrue();
        serviceResult.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ActorAssertion_WithValidBoundAssertion_AssignsActorClaims()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/Swaps";
        context.Request.QueryString = new QueryString("?projectId=project-1");
        context.Request.ContentType = "Application/Json; Charset=UTF-8";
        context.Request.Headers["X-Idempotency-Key"] = "request-1";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"amount\":\"1.00\"}"));
        context.Request.Headers[ActorAssertionMiddleware.HeaderName] = CreateActorAssertion(
            "actor-key-012345678901234567890123456789",
            "user-123",
            "POST",
            "/api/Swaps?projectId=project-1",
            "{\"amount\":\"1.00\"}",
            "application/json;charset=utf-8",
            "request-1");
        var nextCalled = false;
        var middleware = CreateActorMiddleware(async request =>
        {
            nextCalled = true;
            using var reader = new StreamReader(request.Request.Body, Encoding.UTF8, leaveOpen: true);
            (await reader.ReadToEndAsync()).Should().Be("{\"amount\":\"1.00\"}");
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("user-123");
        context.User.HasClaim(ActorAssertionMiddleware.ClaimType, "v2").Should().BeTrue();
    }

    [Theory]
    [InlineData("query")]
    [InlineData("body")]
    [InlineData("content-type")]
    [InlineData("idempotency-key")]
    public async Task ActorAssertion_WithAlteredSignedRequestMetadata_IsRejected(string alteration)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/Swaps";
        context.Request.QueryString = new QueryString(alteration == "query" ? "?projectId=other" : "?projectId=project-1");
        context.Request.ContentType = alteration == "content-type" ? "text/plain" : "application/json";
        context.Request.Headers["X-Idempotency-Key"] = alteration == "idempotency-key" ? "other" : "request-1";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(alteration == "body" ? "{\"amount\":\"2.00\"}" : "{\"amount\":\"1.00\"}"));
        context.Request.Headers[ActorAssertionMiddleware.HeaderName] = CreateActorAssertion(
            "actor-key-012345678901234567890123456789",
            "user-123",
            "POST",
            "/api/Swaps?projectId=project-1",
            "{\"amount\":\"1.00\"}",
            "application/json",
            "request-1");
        var nextCalled = false;
        var middleware = CreateActorMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ActorAssertion_ConcurrentReplay_HasExactlyOneWinner()
    {
        const string key = "actor-key-012345678901234567890123456789";
        const string body = "{\"amount\":\"1.00\"}";
        var assertion = CreateActorAssertion(key, "user-123", "POST", "/api/Swaps", body, "application/json", null);
        var ledger = new InMemoryActorAssertionReplayLedger();
        var nextCount = 0;
        var middleware = CreateActorMiddleware(
            _ =>
            {
                Interlocked.Increment(ref nextCount);
                return Task.CompletedTask;
            },
            key,
            ledger);

        var first = CreateSignedContext(assertion, body);
        var second = CreateSignedContext(assertion, body);
        await Task.WhenAll(middleware.InvokeAsync(first), middleware.InvokeAsync(second));

        nextCount.Should().Be(1);
        new[] { first.Response.StatusCode, second.Response.StatusCode }
            .Should().ContainSingle(status => status == StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ActorAssertion_V1Envelope_IsRejected()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/Swaps";
        context.Request.Headers[ActorAssertionMiddleware.HeaderName] = CreateV1ActorAssertion(
            "actor-key-012345678901234567890123456789",
            "user-123",
            "GET",
            "/api/Swaps");
        var nextCalled = false;
        var middleware = CreateActorMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task ActorAssertion_BodyOverOneMegabyte_IsRejectedBeforeClaim()
    {
        const string key = "actor-key-012345678901234567890123456789";
        var body = new string('a', 1024 * 1024 + 1);
        var context = CreateSignedContext(
            CreateActorAssertion(key, "user-123", "POST", "/api/Swaps", body, "application/json", null),
            body);
        var nextCalled = false;
        var middleware = CreateActorMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, key);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task ActorAssertion_WithWeakSigningKey_FailsClosed()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/TokenBalance/me/portfolio";
        context.Request.Headers[ActorAssertionMiddleware.HeaderName] = "not-used";
        var nextCalled = false;
        var middleware = CreateActorMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, "too-short");

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ActorPolicy_RequiresVerifiedActorClaim()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddArdaNovaAuthorization();
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var actor = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ActorAssertionMiddleware.ClaimType, "v2")], ActorAssertionMiddleware.AuthenticationType));
        var service = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Service")], "ApiKey"));

        (await authorization.AuthorizeAsync(actor, null, AuthorizationPolicies.ActorAssertion)).Succeeded.Should().BeTrue();
        (await authorization.AuthorizeAsync(service, null, AuthorizationPolicies.ActorAssertion)).Succeeded.Should().BeFalse();
        var legacyActor = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ActorAssertionMiddleware.ClaimType, "v1")], ActorAssertionMiddleware.AuthenticationType));
        (await authorization.AuthorizeAsync(legacyActor, null, AuthorizationPolicies.ActorAssertion)).Succeeded.Should().BeFalse();
    }

    private static ApiKeyMiddleware CreateMiddleware(RequestDelegate next)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey:Key"] = "service-key",
                ["AdminApiKey:Key"] = "admin-key"
            })
            .Build();

        return new ApiKeyMiddleware(next, configuration, NullLogger<ApiKeyMiddleware>.Instance);
    }

    private static ActorAssertionMiddleware CreateActorMiddleware(
        RequestDelegate next,
        string signingKey = "actor-key-012345678901234567890123456789",
        IActorAssertionReplayLedger? replayLedger = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ActorAssertion:HmacKey"] = signingKey
            })
            .Build();

        return new ActorAssertionMiddleware(
            next,
            configuration,
            replayLedger ?? new InMemoryActorAssertionReplayLedger(),
            NullLogger<ActorAssertionMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateSignedContext(string assertion, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/Swaps";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers[ActorAssertionMiddleware.HeaderName] = assertion;
        return context;
    }

    private static string CreateActorAssertion(
        string key,
        string subject,
        string method,
        string requestTarget,
        string body = "",
        string contentType = "",
        string? idempotencyKey = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 2,
            issuer = "ardanova-next-bff",
            audience = "ardanova-api",
            subject,
            role = "INDIVIDUAL",
            method,
            requestTarget,
            contentType,
            bodySha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant(),
            idempotencyKey,
            jti = Guid.NewGuid().ToString("D"),
            issuedAt = now,
            expiresAt = now + 90
        });
        var encodedPayload = Convert.ToBase64String(payload).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload));
        return $"{encodedPayload}.{Convert.ToBase64String(signature).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }

    private static string CreateV1ActorAssertion(string key, string subject, string method, string path)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            issuer = "ardanova-next-bff",
            audience = "ardanova-api",
            subject,
            method,
            path,
            issuedAt = now,
            expiresAt = now + 90
        });
        var encodedPayload = Convert.ToBase64String(payload).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload));
        return $"{encodedPayload}.{Convert.ToBase64String(signature).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }

    private sealed class InMemoryActorAssertionReplayLedger : IActorAssertionReplayLedger
    {
        private readonly ConcurrentDictionary<string, byte> _entries = new(StringComparer.Ordinal);

        public Task<ActorAssertionReplayClaim> TryConsumeAsync(
            ActorAssertionReplayEntry entry,
            CancellationToken ct = default)
            => Task.FromResult(_entries.TryAdd(entry.Jti, 0)
                ? ActorAssertionReplayClaim.Consumed
                : ActorAssertionReplayClaim.Replay);
    }
}

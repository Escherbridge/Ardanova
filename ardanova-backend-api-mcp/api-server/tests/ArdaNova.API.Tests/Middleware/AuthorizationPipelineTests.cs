using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ArdaNova.API.Tests.Middleware;

/// <summary>Drives real HTTP requests through the production middleware order (exception → API key → actor → authorization → MVC).</summary>
public sealed class AuthorizationPipelineTests : IAsyncLifetime
{
    private const string ConfiguredServiceKey = "pipeline-service-key-0123456789012345678901";
    private const string ConfiguredAdminKey = "pipeline-admin-key-01234567890123456789012";
    private const string ConfiguredActorKey = "pipeline-actor-key-01234567890123456789012";
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private string _serviceKey = null!;
    private string _adminKey = null!;
    private string _actorKey = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiKey:Key"] = ConfiguredServiceKey,
            ["AdminApiKey:Key"] = ConfiguredAdminKey,
            ["ActorAssertion:HmacKey"] = ConfiguredActorKey,
        });
        builder.Services.AddScoped<IActorAssertionReplayLedger, InMemoryReplayLedger>();
        builder.Services.AddSingleton<InMemoryReplayLedger.Store>();
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(new AssemblyPart(typeof(PipelineProbeController).Assembly));
                manager.FeatureProviders.Add(new ProbeControllerOnly());
            })
            .AddArdaNovaErrorEnvelope();
        builder.Services.AddArdaNovaAuthentication();
        builder.Services.AddArdaNovaAuthorization();

        _app = builder.Build();
        _app.UseExceptionHandling();
        _app.UseApiKeyAuthentication();
        _app.UseActorAssertions();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();

        // Environment variables win over configuration in the key resolvers; sign with whatever is effective.
        _serviceKey = ApiKeyMiddleware.GetApiKey(_app.Configuration)!;
        _adminKey = ApiKeyMiddleware.GetAdminApiKey(_app.Configuration)!;
        _actorKey = ActorAssertionMiddleware.GetSigningKey(_app.Configuration)!;
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task ActorRoute_WithVerifiedActor_IsServed()
    {
        var response = await SendAsync(HttpMethod.Get, "/probe/me", actor: "user-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("user-1");
    }

    [Fact]
    public async Task ActorRoute_WithoutActorAssertion_Is401WithEnvelope()
    {
        var response = await SendAsync(HttpMethod.Get, "/probe/me");

        await AssertEnvelopeAsync(response, HttpStatusCode.Unauthorized, ArdaNovaDenialAuthenticationHandler.ActorRequiredMessage);
    }

    [Fact]
    public async Task ControllerForbid_ForForeignActor_Is403WithEnvelope()
    {
        var response = await SendAsync(HttpMethod.Get, "/probe/conversations/someone-elses", actor: "user-1");

        await AssertEnvelopeAsync(response, HttpStatusCode.Forbidden, ArdaNovaDenialAuthenticationHandler.ForbiddenMessage);
    }

    [Theory]
    [InlineData(true, false, HttpStatusCode.Forbidden)]
    [InlineData(false, true, HttpStatusCode.Unauthorized)]
    [InlineData(true, true, HttpStatusCode.OK)]
    public async Task AdminRoute_RequiresBothActorAndAdminKey(bool withActor, bool withAdminKey, HttpStatusCode expected)
    {
        var response = await SendAsync(
            HttpMethod.Put,
            "/probe/users/user-1/role",
            actor: withActor ? "user-1" : null,
            adminKey: withAdminKey);

        response.StatusCode.Should().Be(expected);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("authenticationScheme");
    }

    [Fact]
    public async Task UnexpectedServerFault_Is500WithoutEchoingTheMessage()
    {
        var response = await SendAsync(HttpMethod.Get, "/probe/faults/invalid-operation");

        await AssertEnvelopeAsync(response, HttpStatusCode.InternalServerError, ExceptionHandlingMiddleware.ServerErrorMessage);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Azoa");
    }

    [Fact]
    public async Task InvalidClientValue_Is400WithoutEchoingTheMessage()
    {
        var response = await SendAsync(HttpMethod.Get, "/probe/faults/invalid-value?day=Someday");

        await AssertEnvelopeAsync(response, HttpStatusCode.BadRequest, ExceptionHandlingMiddleware.InvalidValueMessage);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Someday");
    }

    [Fact]
    public async Task MalformedBody_Is400EnvelopeWithoutInternalTypeNames()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/probe/notifications")
        {
            Content = new StringContent("{\"unexpected\":true}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Api-Key", _serviceKey);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        await AssertEnvelopeAsync(response, HttpStatusCode.BadRequest, ApiErrorEnvelope.ValidationFailedMessage);
        body.Should().NotContain(typeof(ProbeNotificationDto).Namespace!);
        body.Should().NotContain("rfc9110");
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string target,
        string? actor = null,
        bool adminKey = false)
    {
        var request = new HttpRequestMessage(method, target);
        request.Headers.Add("X-Api-Key", _serviceKey);
        if (adminKey)
            request.Headers.Add("X-Admin-Api-Key", _adminKey);
        if (actor is not null)
            request.Headers.Add(ActorAssertionMiddleware.HeaderName, SignActorAssertion(actor, method.Method, target));
        return await _client.SendAsync(request);
    }

    private static async Task AssertEnvelopeAsync(HttpResponseMessage response, HttpStatusCode status, string error)
    {
        response.StatusCode.Should().Be(status);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("error").GetString().Should().Be(error);
        document.RootElement.GetProperty("statusCode").GetInt32().Should().Be((int)status);
        document.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private string SignActorAssertion(string subject, string method, string requestTarget)
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
            contentType = "",
            bodySha256 = Convert.ToHexString(SHA256.HashData([])).ToLowerInvariant(),
            idempotencyKey = (string?)null,
            jti = Guid.NewGuid().ToString("D"),
            issuedAt = now,
            expiresAt = now + 60,
        });
        var encodedPayload = Base64Url(payload);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_actorKey));
        return $"{encodedPayload}.{Base64Url(hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload)))}";
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class ProbeControllerOnly : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            feature.Controllers.Clear();
            feature.Controllers.Add(typeof(PipelineProbeController).GetTypeInfo());
        }
    }

    private sealed class InMemoryReplayLedger(InMemoryReplayLedger.Store store) : IActorAssertionReplayLedger
    {
        public sealed class Store
        {
            public ConcurrentDictionary<string, byte> Consumed { get; } = new(StringComparer.Ordinal);
        }

        public Task<ActorAssertionReplayClaim> TryConsumeAsync(ActorAssertionReplayEntry entry, CancellationToken ct = default)
            => Task.FromResult(store.Consumed.TryAdd(entry.Jti, 0)
                ? ActorAssertionReplayClaim.Consumed
                : ActorAssertionReplayClaim.Replay);
    }
}

/// <summary>Stand-in endpoints shaped like the real controllers' authorization and binding surfaces.</summary>
[ApiController]
[Route("probe")]
public sealed class PipelineProbeController : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public IActionResult Me() => Ok(new { id = User.FindFirstValue(ClaimTypes.NameIdentifier) });

    [HttpGet("conversations/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public IActionResult Conversation(string id) => Forbid();

    [HttpPut("users/{id}/role")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public IActionResult UpdateRole(string id) => Ok(new { id });

    [HttpGet("faults/invalid-operation")]
    public IActionResult InvalidOperation()
        => throw new InvalidOperationException("Azoa:TimeoutSeconds must be between 1 and 120.");

    [HttpGet("faults/invalid-value")]
    public IActionResult InvalidValue([FromQuery] string day) => Ok(Enum.Parse<DayOfWeek>(day));

    [HttpPost("notifications")]
    public IActionResult CreateNotification([FromBody] ProbeNotificationDto dto) => Ok(dto);
}

public sealed record ProbeNotificationDto
{
    public required string UserId { get; init; }
    public required string Title { get; init; }
}

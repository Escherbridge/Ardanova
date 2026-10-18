using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.API.Controllers;
using ArdaNova.API.Filters;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace ArdaNova.API.Tests.Filters;

/// <summary>Drives the real gamification, event and user controllers through the production pipeline with the endpoint policies installed.</summary>
public sealed class EndpointPolicyPipelineTests : IAsyncLifetime
{
    private const string Victim = "victim-user-0001";
    private const string Attacker = "attacker-user-01";
    private static readonly Type[] LoadedControllers =
    [
        typeof(AchievementsController), typeof(ActivitiesController), typeof(EventsController),
        typeof(LeaderboardsController), typeof(ProductsController), typeof(ReferralsController),
        typeof(UserStreaksController), typeof(XPEventsController), typeof(UsersController),
    ];

    private readonly ConcurrentBag<AwardXPDto> _awards = new();
    private readonly ConcurrentBag<CreateEventDto> _createdEvents = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private EndpointAuthorizationConvention _convention = null!;
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
            ["ApiKey:Key"] = "policy-service-key-0123456789012345678901",
            ["AdminApiKey:Key"] = "policy-admin-key-01234567890123456789012",
            ["ActorAssertion:HmacKey"] = "policy-actor-key-01234567890123456789012",
        });
        builder.Services.AddSingleton<ConcurrentDictionary<string, byte>>();
        builder.Services.AddScoped<IActorAssertionReplayLedger, InMemoryReplayLedger>();
        RegisterServices(builder.Services);
        builder.Services.AddControllers(options =>
            {
                options.AddArdaNovaEndpointPolicies();
                _convention = options.Conventions.OfType<EndpointAuthorizationConvention>().Single();
            })
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(new AssemblyPart(typeof(EventsController).Assembly));
                manager.FeatureProviders.Add(new OnlyControllers(LoadedControllers));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
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

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("INDIVIDUAL", HttpStatusCode.Forbidden)]
    [InlineData("ADMIN", HttpStatusCode.Created)]
    public async Task XpAward_IsAnAdministratorAction(string? actorRole, HttpStatusCode expected)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/XPEvents/award",
            new { userId = Victim, eventType = "CONTRIBUTION_MADE", amount = 1, source = "qa" },
            actor: actorRole is null ? null : Attacker,
            role: actorRole);

        response.StatusCode.Should().Be(expected);
        _awards.Should().HaveCount(expected == HttpStatusCode.Created ? 1 : 0);
    }

    [Fact]
    public async Task EventCreate_ForAnotherOrganizer_IsRefusedBeforeAnyWrite()
    {
        var response = await SendAsync(HttpMethod.Post, "/api/Events", NewEvent(organizerId: Victim), actor: Attacker);

        await AssertEnvelopeAsync(response, HttpStatusCode.Forbidden);
        _createdEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EventCreate_ByTheOrganizerThemself_IsServed()
    {
        var response = await SendAsync(HttpMethod.Post, "/api/Events", NewEvent(organizerId: Attacker), actor: Attacker);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _createdEvents.Should().ContainSingle(created => created.OrganizerId == Attacker);
    }

    [Theory]
    [InlineData("/api/Achievements")]
    [InlineData("/api/Leaderboards")]
    [InlineData("/api/UserStreaks")]
    [InlineData("/api/Products")]
    [InlineData("/api/Referrals")]
    [InlineData("/api/Activities")]
    public async Task ServiceKeyAlone_CannotCreate(string route)
    {
        var response = await SendAsync(HttpMethod.Post, route, new { userId = Victim });

        await AssertEnvelopeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(Attacker, false, false)]
    [InlineData(Victim, false, true)]
    [InlineData(null, true, true)]
    public async Task UserById_ShowsPrivateFieldsOnlyToTheUserOrAnAdministrator(string? actor, bool adminKey, bool seesPrivate)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/Users/{Victim}", actor: actor, adminKey: adminKey);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(Victim).And.Contain("Victim Name");
        foreach (var privateValue in new[] { "victim@example.test", "+15550100", "\"trustScore\"", "\"role\"" })
        {
            if (seesPrivate) body.Should().Contain(privateValue);
            else body.Should().NotContain(privateValue);
        }
    }

    [Theory]
    [InlineData(Attacker, HttpStatusCode.Forbidden)]
    [InlineData(Victim, HttpStatusCode.OK)]
    public async Task ReferralLookupByUser_IsLimitedToThatUser(string actor, HttpStatusCode expected)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/Referrals/referred/{Victim}", actor: actor);

        response.StatusCode.Should().Be(expected);
        (await response.Content.ReadAsStringAsync()).Contains("referral-1").Should().Be(expected == HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserSearch_RedactsEveryRowForAnAnonymousCaller()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/Users/search?query=victim");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(Victim).And.NotContain("victim@example.test");
    }

    [Theory]
    [InlineData("/api/Users")]
    [InlineData("/api/Users/email/victim@example.test")]
    public async Task UnpagedAndByEmailUserLookups_RequireTheAdminKey(string route)
    {
        (await SendAsync(HttpMethod.Get, route)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Get, route, adminKey: true)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("page=-3&pageSize=5", 1, 5)]
    [InlineData("pageSize=-5", 1, 1)]
    [InlineData("pageSize=0", 1, 1)]
    [InlineData("pageSize=100000", 1, 100)]
    [InlineData("page=2147483647&pageSize=2147483647", 21474836, 100)]
    public async Task PagingExtremes_AreClampedInsteadOf500(string query, int expectedPage, int expectedPageSize)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/Users/paged?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("page").GetInt32().Should().Be(expectedPage);
        document.RootElement.GetProperty("pageSize").GetInt32().Should().Be(expectedPageSize);
        document.RootElement.GetProperty("hasNextPage").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task BusinessNotFound_UsesTheSharedEnvelope()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/Referrals/code/does-not-exist");

        await AssertEnvelopeAsync(response, HttpStatusCode.NotFound, "Referral not found");
    }

    [Fact]
    public void EveryMutatingEndpoint_CarriesAuthorizationMetadata()
    {
        var mutations = _app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                .Any(method => method is "POST" or "PUT" or "PATCH" or "DELETE") == true)
            .ToList();

        mutations.Should().NotBeEmpty();
        mutations.Should().AllSatisfy(endpoint =>
            (endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null
                || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                .Should().BeTrue($"{endpoint.DisplayName} must declare who may call it"));
    }

    [Fact]
    public void EveryPolicyOverrideAndSubjectBinding_NamesALoadedAction()
    {
        var configured = EndpointAuthorizationConvention.PolicyOverrides.Keys
            .Concat(EndpointAuthorizationConvention.SubjectBindings.Keys)
            .Concat(EndpointAuthorizationConvention.ResourceGuards.Keys)
            .Distinct();

        _convention.MatchedKeys.Should().BeEquivalentTo(configured);
    }

    private static object NewEvent(string organizerId) => new
    {
        organizerId,
        title = "Town hall",
        type = "TOWN_HALL",
        timezone = "UTC",
        startDate = "2026-11-01T10:00:00Z",
        endDate = "2026-11-01T11:00:00Z",
    };

    private void RegisterServices(IServiceCollection services)
    {
        var victim = new UserDto
        {
            Id = Victim,
            Email = "victim@example.test",
            Phone = "+15550100",
            Name = "Victim Name",
            TrustScore = 42,
        };

        var users = new Mock<IUserService>();
        users.Setup(s => s.GetByIdAsync(Victim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(victim));
        users.Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(victim));
        users.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success([victim]));
        users.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, int page, int pageSize, CancellationToken _) =>
                Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>([victim], 1, page, pageSize)));
        users.Setup(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int pageSize, CancellationToken _) =>
                Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>([], 0, page, pageSize)));
        services.AddSingleton(users.Object);
        services.AddSingleton(new Mock<IUserFollowService>().Object);

        var xp = new Mock<IXPEventService>();
        xp.Setup(s => s.AwardXPAsync(It.IsAny<AwardXPDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AwardXPDto dto, CancellationToken _) =>
            {
                _awards.Add(dto);
                return Result<XPEventDto>.Success(new XPEventDto { Id = "xp-1", UserId = dto.UserId, Amount = dto.Amount });
            });
        services.AddSingleton(xp.Object);

        var events = new Mock<IEventService>();
        events.Setup(s => s.CreateAsync(It.IsAny<CreateEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateEventDto dto, CancellationToken _) =>
            {
                _createdEvents.Add(dto);
                return Result<EventDto>.Success(new EventDto { Id = "event-1", Title = dto.Title, Slug = "town-hall" });
            });
        events.Setup(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EventDto>.NotFound("Event not found"));
        services.AddSingleton(events.Object);

        services.AddSingleton(new Mock<IAchievementService>().Object);
        services.AddSingleton(new Mock<IActivityService>().Object);
        services.AddSingleton(new Mock<ILeaderboardService>().Object);
        services.AddSingleton(new Mock<IProductService>().Object);
        var referrals = new Mock<IReferralService>();
        referrals.Setup(s => s.GetByReferredIdAsync(Victim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReferralDto>.Success(new ReferralDto { Id = "referral-1", ReferredId = Victim }));
        referrals.Setup(s => s.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReferralDto>.NotFound("Referral not found"));
        services.AddSingleton(referrals.Object);
        services.AddSingleton(new Mock<IUserStreakService>().Object);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string target,
        object? body = null,
        string? actor = null,
        string? role = "INDIVIDUAL",
        bool adminKey = false)
    {
        var bytes = body is null ? [] : JsonSerializer.SerializeToUtf8Bytes(body);
        var request = new HttpRequestMessage(method, target);
        if (body is not null)
        {
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        request.Headers.Add("X-Api-Key", _serviceKey);
        if (adminKey)
            request.Headers.Add("X-Admin-Api-Key", _adminKey);
        if (actor is not null)
            request.Headers.Add(
                ActorAssertionMiddleware.HeaderName,
                SignActorAssertion(actor, role, method.Method, target, body is null ? "" : "application/json", bytes));
        return await _client.SendAsync(request);
    }

    private static async Task AssertEnvelopeAsync(HttpResponseMessage response, HttpStatusCode status, string? error = null)
    {
        response.StatusCode.Should().Be(status);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (error is not null)
            document.RootElement.GetProperty("error").GetString().Should().Be(error);
        else
            document.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("statusCode").GetInt32().Should().Be((int)status);
        document.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private string SignActorAssertion(string subject, string? role, string method, string requestTarget, string contentType, byte[] body)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 2,
            issuer = "ardanova-next-bff",
            audience = "ardanova-api",
            subject,
            role,
            method,
            requestTarget,
            contentType,
            bodySha256 = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant(),
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

    private sealed class OnlyControllers(Type[] controllers) : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            feature.Controllers.Clear();
            foreach (var controller in controllers)
                feature.Controllers.Add(controller.GetTypeInfo());
        }
    }

    private sealed class InMemoryReplayLedger(ConcurrentDictionary<string, byte> consumed) : IActorAssertionReplayLedger
    {
        public Task<ActorAssertionReplayClaim> TryConsumeAsync(ActorAssertionReplayEntry entry, CancellationToken ct = default)
            => Task.FromResult(consumed.TryAdd(entry.Jti, 0)
                ? ActorAssertionReplayClaim.Consumed
                : ActorAssertionReplayClaim.Replay);
    }
}

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
using ArdaNova.Application;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Common.Security;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Validators;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ArdaNova.API.Tests.Filters;

/// <summary>Drives Events, Products and Referrals through the production pipeline over seeded rows: the API itself must enforce record ownership, event visibility and input bounds.</summary>
public sealed class ResourceGuardPipelineTests : IAsyncLifetime
{
    private const string Organizer = "organizer-user-01";
    private const string Stranger = "stranger-user-001";
    private const string Attendee = "attendee-user-001";
    private const string Invitee = "invitee-user-0001";
    private const string ProjectFounder = "founder-user-0001";
    private const string ProjectLeader = "leader-user-00001";
    private const string ProjectContributor = "contrib-user-0001";

    private readonly PipelineHost _host = new();
    private SeededWorld World => _host.World;

    public Task InitializeAsync() => _host.StartAsync(permitsPerMinute: 1_000);

    public Task DisposeAsync() => _host.DisposeAsync().AsTask();

    [Theory]
    [InlineData(Stranger, "INDIVIDUAL", HttpStatusCode.Forbidden)]
    [InlineData(Organizer, "INDIVIDUAL", HttpStatusCode.OK)]
    [InlineData(Stranger, "ADMIN", HttpStatusCode.OK)]
    public async Task EventUpdate_IsTheOrganizersAlone(string actor, string role, HttpStatusCode expected)
    {
        var response = await _host.SendAsync(HttpMethod.Put, "/api/Events/event-public", new { title = "Renamed" }, actor, role);

        response.StatusCode.Should().Be(expected);
        World.Events.Single(e => e.id == "event-public").title
            .Should().Be(expected == HttpStatusCode.OK ? "Renamed" : "Public town hall");
    }

    [Fact]
    public async Task EventDelete_ByAnotherActor_LeavesTheEventInPlace()
    {
        var response = await _host.SendAsync(HttpMethod.Delete, "/api/Events/event-public", actor: Stranger);

        await AssertEnvelopeAsync(response, HttpStatusCode.Forbidden);
        World.Events.Should().Contain(e => e.id == "event-public");

        (await _host.SendAsync(HttpMethod.Delete, "/api/Events/event-public", actor: Organizer))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        World.Events.Should().NotContain(e => e.id == "event-public");
    }

    [Fact]
    public async Task EventMutation_OnAnUnknownId_Is404NotAForbiddenLeak()
        => await AssertEnvelopeAsync(
            await _host.SendAsync(HttpMethod.Put, "/api/Events/no-such-event", new { title = "x" }, Stranger),
            HttpStatusCode.NotFound);

    [Theory]
    [InlineData(null, false)]
    [InlineData(Stranger, false)]
    [InlineData(Invitee, true)]
    [InlineData(Organizer, true)]
    public async Task InviteOnlyEvent_ExistsOnlyForItsHostAndInvitees(string? viewer, bool visible)
    {
        var byId = await _host.SendAsync(HttpMethod.Get, "/api/Events/event-invite", actor: viewer);
        var upcoming = await _host.SendAsync(HttpMethod.Get, "/api/Events/upcoming", actor: viewer);
        var attendees = await _host.SendAsync(HttpMethod.Get, "/api/Events/event-invite/attendees", actor: viewer);

        byId.StatusCode.Should().Be(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        attendees.StatusCode.Should().Be(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        upcoming.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await upcoming.Content.ReadAsStringAsync();
        listed.Should().Contain("event-public");
        listed.Contains("event-invite").Should().Be(visible);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(ProjectContributor, true)]
    [InlineData(ProjectFounder, true)]
    public async Task ProjectMembersEvent_IsListedOnlyForThatProjectsMembers(string? viewer, bool visible)
    {
        var body = await (await _host.SendAsync(HttpMethod.Get, "/api/Events/upcoming", actor: viewer)).Content.ReadAsStringAsync();

        body.Contains("event-members").Should().Be(visible);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(Stranger, false)]
    [InlineData(Attendee, true)]
    [InlineData(Organizer, true)]
    public async Task MeetingLink_IsShownOnlyToHostsAndAttendees(string? viewer, bool seesLink)
    {
        var response = await _host.SendAsync(HttpMethod.Get, "/api/Events/event-public", actor: viewer);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Contains("https://meet.example.test/public").Should().Be(seesLink);
    }

    [Fact]
    public async Task RegisteringForAnEventTheActorCannotSee_IsRefused()
    {
        var response = await _host.SendAsync(HttpMethod.Post, "/api/Events/event-invite/register", new { userId = Stranger }, Stranger);

        await AssertEnvelopeAsync(response, HttpStatusCode.NotFound);
        World.Attendees.Should().NotContain(a => a.userId == Stranger);
    }

    [Fact]
    public async Task EventTimestamps_AreWrittenAsUtc()
    {
        var body = await (await _host.SendAsync(HttpMethod.Get, "/api/Events/event-public")).Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("startDate").GetString().Should().Be("2027-01-10T15:00:00Z");
    }

    [Theory]
    [InlineData(Stranger, Stranger, HttpStatusCode.Forbidden)]
    [InlineData(ProjectContributor, ProjectContributor, HttpStatusCode.Forbidden)]
    [InlineData(ProjectLeader, ProjectFounder, HttpStatusCode.Forbidden)]
    [InlineData(ProjectLeader, ProjectLeader, HttpStatusCode.Created)]
    [InlineData(ProjectFounder, ProjectFounder, HttpStatusCode.Created)]
    public async Task ProductCreate_RequiresAProjectManagerActingForThemself(string actor, string namedOwner, HttpStatusCode expected)
    {
        var response = await _host.SendAsync(
            HttpMethod.Post,
            "/api/Products",
            new { projectId = "project-1", userId = namedOwner, name = "Sticker", price = 3.5m },
            actor);

        response.StatusCode.Should().Be(expected);
        World.Products.Count(p => p.name == "Sticker").Should().Be(expected == HttpStatusCode.Created ? 1 : 0);
    }

    [Theory]
    [InlineData(Stranger, HttpStatusCode.Forbidden)]
    [InlineData(ProjectContributor, HttpStatusCode.Forbidden)]
    [InlineData(ProjectLeader, HttpStatusCode.OK)]
    public async Task ProductUpdate_IsForProjectManagers(string actor, HttpStatusCode expected)
    {
        var response = await _host.SendAsync(HttpMethod.Put, "/api/Products/product-1", new { name = "Mug v2" }, actor);

        response.StatusCode.Should().Be(expected);
        World.Products.Single(p => p.id == "product-1").name.Should().Be(expected == HttpStatusCode.OK ? "Mug v2" : "Mug");
    }

    [Fact]
    public async Task ProductDelete_ByANonMember_KeepsTheProduct()
    {
        await AssertEnvelopeAsync(await _host.SendAsync(HttpMethod.Delete, "/api/Products/product-1", actor: Stranger), HttpStatusCode.Forbidden);

        World.Products.Should().Contain(p => p.id == "product-1");
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(Stranger, HttpStatusCode.Forbidden)]
    [InlineData(Attendee, HttpStatusCode.OK)]
    [InlineData(Organizer, HttpStatusCode.OK)]
    public async Task ReferralById_IsReadableOnlyByItsParticipants(string? actor, HttpStatusCode expected)
    {
        var response = await _host.SendAsync(HttpMethod.Get, "/api/Referrals/referral-1", actor: actor);

        response.StatusCode.Should().Be(expected);
        (await response.Content.ReadAsStringAsync()).Contains(Attendee).Should().Be(expected == HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(Stranger, false)]
    [InlineData(Organizer, true)]
    public async Task ReferralCodeLookup_NamesTheParticipantsOnlyToThem(string? actor, bool seesParticipants)
    {
        var response = await _host.SendAsync(HttpMethod.Get, "/api/Referrals/code/WELCOME-1", actor: actor);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("WELCOME-1").And.Contain("PENDING");
        body.Contains(Organizer).Should().Be(seesParticipants);
        body.Contains(Attendee).Should().Be(seesParticipants);
    }

    [Fact]
    public async Task EventCreate_WithDangerousOrImpossibleValues_IsRejectedFieldByField()
    {
        var response = await _host.SendAsync(HttpMethod.Post, "/api/Events", new
        {
            organizerId = Organizer,
            title = new string('x', 20_032),
            type = "MEETING",
            timezone = "Not/AZone",
            meetingUrl = "javascript:alert(document.cookie)",
            locationUrl = "javascript:alert(1)",
            startDate = "2027-01-10T00:00:00Z",
            endDate = "2026-01-01T00:00:00Z",
            maxAttendees = -5,
        }, Organizer);

        await AssertEnvelopeAsync(response, HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("errors").EnumerateObject().Select(field => field.Name)
            .Should().BeEquivalentTo("title", "timezone", "meetingUrl", "locationUrl", "endDate", "maxAttendees");
        World.Events.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("PST")]
    [InlineData("Europe/Berlin")]
    public async Task EventCreate_WithTheFormsTimezonesAndAnHttpsLink_IsServed(string timezone)
    {
        var response = await _host.SendAsync(HttpMethod.Post, "/api/Events", new
        {
            organizerId = Organizer,
            title = "Office hours",
            type = "MEETING",
            timezone,
            meetingUrl = "https://meet.example.test/office",
            startDate = "2027-02-01T10:00:00+02:00",
            endDate = "2027-02-01T11:00:00+02:00",
        }, Organizer);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        World.Events.Single(e => e.title == "Office hours").startDate
            .Should().Be(new DateTime(2027, 2, 1, 8, 0, 0, DateTimeKind.Utc), "an offset timestamp is stored as its UTC instant");
    }

    [Fact]
    public async Task ProductCreate_WithANegativePrice_IsRejected()
    {
        var response = await _host.SendAsync(
            HttpMethod.Post,
            "/api/Products",
            new { projectId = "project-1", userId = ProjectFounder, name = "Refund machine", price = -1000m },
            ProjectFounder);

        await AssertEnvelopeAsync(response, HttpStatusCode.BadRequest);
        World.Products.Should().NotContain(p => p.name == "Refund machine");
    }

    [Fact]
    public async Task AValidationFailure_NeverOutranksTheOwnershipCheck()
    {
        var response = await _host.SendAsync(HttpMethod.Put, "/api/Products/product-1", new { price = -1m }, Stranger);

        await AssertEnvelopeAsync(response, HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(Stranger, "project-1", null, HttpStatusCode.Forbidden)]
    [InlineData(ProjectContributor, "project-1", null, HttpStatusCode.Created)]
    [InlineData(ProjectFounder, "project-1", null, HttpStatusCode.Created)]
    [InlineData(Stranger, null, "guild-1", HttpStatusCode.Forbidden)]
    [InlineData(Attendee, null, "guild-1", HttpStatusCode.Created)]
    [InlineData(ProjectContributor, "project-1", "guild-1", HttpStatusCode.Forbidden)]
    [InlineData(Stranger, "no-such-project", null, HttpStatusCode.NotFound)]
    [InlineData(Stranger, null, null, HttpStatusCode.Created)]
    public async Task EventCreate_UnderAProjectOrGuild_NeedsTheOrganizerToBelongThere(
        string actor, string? projectId, string? guildId, HttpStatusCode expected)
    {
        var before = World.Events.Count;

        var response = await _host.SendAsync(HttpMethod.Post, "/api/Events", new
        {
            organizerId = actor,
            title = "Community meetup",
            type = "MEETING",
            timezone = "UTC",
            visibility = "PROJECT_MEMBERS",
            projectId,
            guildId,
            meetingUrl = "https://meet.example.test/meetup",
            startDate = "2027-03-01T10:00:00Z",
            endDate = "2027-03-01T11:00:00Z",
        }, actor);

        response.StatusCode.Should().Be(expected);
        World.Events.Should().HaveCount(expected == HttpStatusCode.Created ? before + 1 : before);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(Stranger, 0)]
    [InlineData(Invitee, 1)]
    public async Task EventSearchTotals_DoNotCountEventsTheViewerCannotSee(string? viewer, int expectedTotal)
    {
        var response = await _host.SendAsync(HttpMethod.Get, "/api/Events/search?searchTerm=board%20dinner", actor: viewer);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("totalCount").GetInt32().Should().Be(expectedTotal);
        document.RootElement.GetProperty("items").GetArrayLength().Should().Be(expectedTotal);
    }

    [Theory]
    [InlineData("GET", "/api/NoSuchThing", HttpStatusCode.NotFound)]
    [InlineData("DELETE", "/api/Events", HttpStatusCode.MethodNotAllowed)]
    public async Task UnroutableRequests_StillAnswerWithTheErrorEnvelope(string method, string target, HttpStatusCode expected)
        => await AssertEnvelopeAsync(await _host.SendAsync(new HttpMethod(method), target), expected);

    [Fact]
    public void EveryResourceGuard_NamesALoadedAction()
        => _host.Convention.MatchedKeys.Should().Contain(EndpointAuthorizationConvention.ResourceGuards.Keys);

    private static async Task AssertEnvelopeAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.Should().Be(status);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("statusCode").GetInt32().Should().Be((int)status);
    }

    /// <summary>Rows the repositories and the feature-service stand-ins share, as the database would.</summary>
    internal sealed class SeededWorld
    {
        public List<Event> Events { get; } =
        [
            new()
            {
                id = "event-public", title = "Public town hall", visibility = EventVisibility.PUBLIC, organizerId = Organizer,
                meetingUrl = "https://meet.example.test/public", timezone = "UTC",
                startDate = new DateTime(2027, 1, 10, 15, 0, 0, DateTimeKind.Unspecified),
                endDate = new DateTime(2027, 1, 10, 16, 0, 0, DateTimeKind.Unspecified),
            },
            new()
            {
                id = "event-invite", title = "Board dinner", visibility = EventVisibility.INVITE_ONLY, organizerId = Organizer,
                meetingUrl = "https://meet.example.test/invite", timezone = "UTC",
            },
            new()
            {
                id = "event-members", title = "Sprint review", visibility = EventVisibility.PROJECT_MEMBERS, organizerId = ProjectFounder,
                projectId = "project-1", timezone = "UTC",
            },
        ];

        public List<EventAttendee> Attendees { get; } =
        [
            new() { id = "rsvp-1", eventId = "event-public", userId = Attendee, status = AttendeeStatus.GOING },
            new() { id = "rsvp-2", eventId = "event-invite", userId = Invitee, status = AttendeeStatus.INVITED },
        ];

        public List<Project> Projects { get; } = [new() { id = "project-1", createdById = ProjectFounder }];

        public List<ProjectMember> ProjectMembers { get; } =
        [
            new() { id = "pm-1", projectId = "project-1", userId = ProjectLeader, role = ProjectRole.LEADER },
            new() { id = "pm-2", projectId = "project-1", userId = ProjectContributor, role = ProjectRole.CONTRIBUTOR },
        ];

        public List<Guild> Guilds { get; } = [new() { id = "guild-1", ownerId = Organizer }];

        public List<GuildMember> GuildMembers { get; } =
        [
            new() { id = "gm-1", guildId = "guild-1", userId = Attendee },
        ];

        public List<Product> Products { get; } = [new() { id = "product-1", projectId = "project-1", name = "Mug", userId = ProjectFounder }];

        public List<Referral> Referrals { get; } =
        [
            new() { id = "referral-1", referrerId = Organizer, referredId = Attendee, referralCode = "WELCOME-1", status = ReferralStatus.PENDING },
        ];

        public void Register(IServiceCollection services)
        {
            services.AddSingleton<IRepository<Event>>(new InMemoryRepository<Event>(Events));
            services.AddSingleton<IRepository<EventAttendee>>(new InMemoryRepository<EventAttendee>(Attendees));
            services.AddSingleton<IRepository<EventCoHost>>(new InMemoryRepository<EventCoHost>([]));
            services.AddSingleton<IRepository<Project>>(new InMemoryRepository<Project>(Projects));
            services.AddSingleton<IRepository<ProjectMember>>(new InMemoryRepository<ProjectMember>(ProjectMembers));
            services.AddSingleton<IRepository<Guild>>(new InMemoryRepository<Guild>(Guilds));
            services.AddSingleton<IRepository<GuildMember>>(new InMemoryRepository<GuildMember>(GuildMembers));
            services.AddSingleton<IRepository<Product>>(new InMemoryRepository<Product>(Products));
            services.AddSingleton<IRepository<Referral>>(new InMemoryRepository<Referral>(Referrals));
            services.AddScoped<IResourceAuthority, ResourceAuthority>();
            services.AddRequestValidators();
            services.AddSingleton(EventService());
            services.AddSingleton(ProductService());
            services.AddSingleton(ReferralService());
            services.AddSingleton(new Mock<IUserService>().Object);
            services.AddSingleton(new Mock<IUserFollowService>().Object);
        }

        private IEventService EventService()
        {
            var service = new Mock<IEventService>();
            service.Setup(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) => Events.FirstOrDefault(e => e.id == id) is { } row
                    ? Result<EventDto>.Success(ToDto(row))
                    : Result<EventDto>.NotFound("Event not found"));
            service.Setup(s => s.GetUpcomingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Result<IReadOnlyList<EventDto>>.Success(Events.Select(ToDto).ToList()));
            service.Setup(s => s.SearchAsync(
                    It.IsAny<string?>(), It.IsAny<EventType?>(), It.IsAny<EventStatus?>(), It.IsAny<bool?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string? term, EventType? _, EventStatus? _, bool? _, int page, int pageSize, CancellationToken _) =>
                {
                    // The repository matches every row, hidden or not, as the real query does.
                    var matches = Events
                        .Where(e => term is null || e.title.Contains(term, StringComparison.OrdinalIgnoreCase))
                        .Select(ToDto)
                        .ToList();
                    return Result<PagedResult<EventDto>>.Success(new PagedResult<EventDto>(matches, matches.Count, page, pageSize));
                });
            service.Setup(s => s.GetAttendeesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) => Result<IReadOnlyList<EventAttendeeDto>>.Success(
                    Attendees.Where(a => a.eventId == id).Select(a => new EventAttendeeDto { Id = a.id, EventId = id, UserId = a.userId }).ToList()));
            service.Setup(s => s.CreateAsync(It.IsAny<CreateEventDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateEventDto dto, CancellationToken _) =>
                {
                    var row = new Event { id = $"event-{Events.Count + 1}", title = dto.Title, organizerId = dto.OrganizerId, startDate = dto.StartDate, endDate = dto.EndDate, timezone = dto.Timezone };
                    Events.Add(row);
                    return Result<EventDto>.Success(ToDto(row));
                });
            service.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateEventDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, UpdateEventDto dto, CancellationToken _) =>
                {
                    var row = Events.Single(e => e.id == id);
                    row.title = dto.Title ?? row.title;
                    return Result<EventDto>.Success(ToDto(row));
                });
            service.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) => Result<bool>.Success(Events.RemoveAll(e => e.id == id) == 1));
            service.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<RegisterEventDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, RegisterEventDto dto, CancellationToken _) =>
                {
                    Attendees.Add(new EventAttendee { id = $"rsvp-{Attendees.Count + 1}", eventId = id, userId = dto.UserId, status = AttendeeStatus.GOING });
                    return Result<EventAttendeeDto>.Success(new EventAttendeeDto { Id = "rsvp", EventId = id, UserId = dto.UserId });
                });
            return service.Object;
        }

        private IProductService ProductService()
        {
            var service = new Mock<IProductService>();
            service.Setup(s => s.CreateAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateProductDto dto, CancellationToken _) =>
                {
                    var row = new Product { id = $"product-{Products.Count + 1}", projectId = dto.ProjectId, name = dto.Name, price = dto.Price, userId = dto.UserId };
                    Products.Add(row);
                    return Result<ProductDto>.Success(new ProductDto { Id = row.id, ProjectId = row.projectId, Name = row.name, UserId = row.userId });
                });
            service.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateProductDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, UpdateProductDto dto, CancellationToken _) =>
                {
                    var row = Products.Single(p => p.id == id);
                    row.name = dto.Name ?? row.name;
                    return Result<ProductDto>.Success(new ProductDto { Id = row.id, ProjectId = row.projectId, Name = row.name, UserId = row.userId });
                });
            service.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) => Result<bool>.Success(Products.RemoveAll(p => p.id == id) == 1));
            return service.Object;
        }

        private IReferralService ReferralService()
        {
            var service = new Mock<IReferralService>();
            service.Setup(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) => Result<ReferralDto>.Success(ToDto(Referrals.Single(r => r.id == id))));
            service.Setup(s => s.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string code, CancellationToken _) => Referrals.FirstOrDefault(r => r.referralCode == code) is { } row
                    ? Result<ReferralDto>.Success(ToDto(row))
                    : Result<ReferralDto>.NotFound("Referral not found"));
            return service.Object;
        }

        private static EventDto ToDto(Event row) => new()
        {
            Id = row.id, Title = row.title, Slug = row.id, Visibility = row.visibility, OrganizerId = row.organizerId,
            ProjectId = row.projectId, GuildId = row.guildId, MeetingUrl = row.meetingUrl, Timezone = row.timezone,
            StartDate = row.startDate, EndDate = row.endDate,
        };

        private static ReferralDto ToDto(Referral row) => new()
        {
            Id = row.id, ReferrerId = row.referrerId, ReferredId = row.referredId, ReferralCode = row.referralCode, Status = row.status,
        };
    }

    /// <summary>The production middleware order and MVC policies over the Events, Products and Referrals controllers.</summary>
    internal sealed class PipelineHost : IAsyncDisposable
    {
        private WebApplication _app = null!;
        private HttpClient _client = null!;
        private string _serviceKey = null!;
        private string _actorKey = null!;

        public SeededWorld World { get; } = new();
        public EndpointAuthorizationConvention Convention { get; private set; } = null!;

        public async Task StartAsync(int permitsPerMinute)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey:Key"] = "guard-service-key-01234567890123456789012",
                ["AdminApiKey:Key"] = "guard-admin-key-012345678901234567890123",
                ["ActorAssertion:HmacKey"] = "guard-actor-key-012345678901234567890123",
                [ActorRateLimiting.PermitsPerMinuteKey] = permitsPerMinute.ToString(),
            });
            builder.Services.AddSingleton<ConcurrentDictionary<string, byte>>();
            builder.Services.AddScoped<IActorAssertionReplayLedger, InMemoryReplayLedger>();
            World.Register(builder.Services);
            builder.Services.AddControllers(options =>
                {
                    options.AddArdaNovaEndpointPolicies();
                    Convention = options.Conventions.OfType<EndpointAuthorizationConvention>().Single();
                })
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.ApplicationParts.Add(new AssemblyPart(typeof(EventsController).Assembly));
                    manager.FeatureProviders.Add(new OnlyControllers([typeof(EventsController), typeof(ProductsController), typeof(ReferralsController)]));
                })
                .AddJsonOptions(options => ArdaNovaJson.Configure(options.JsonSerializerOptions))
                .AddArdaNovaErrorEnvelope();
            builder.Services.AddArdaNovaActorRateLimiting(builder.Configuration);
            builder.Services.AddArdaNovaAuthentication();
            builder.Services.AddArdaNovaAuthorization();

            _app = builder.Build();
            _app.UseExceptionHandling();
            _app.UseArdaNovaStatusEnvelope();
            _app.UseApiKeyAuthentication();
            _app.UseActorAssertions();
            _app.UseArdaNovaActorRateLimiting();
            _app.UseAuthorization();
            _app.MapControllers();
            await _app.StartAsync();

            _serviceKey = ApiKeyMiddleware.GetApiKey(_app.Configuration)!;
            _actorKey = ActorAssertionMiddleware.GetSigningKey(_app.Configuration)!;
            _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        public async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string target,
            object? body = null,
            string? actor = null,
            string role = "INDIVIDUAL")
        {
            var bytes = body is null ? [] : JsonSerializer.SerializeToUtf8Bytes(body);
            var request = new HttpRequestMessage(method, target);
            if (body is not null)
            {
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            request.Headers.Add("X-Api-Key", _serviceKey);
            if (actor is not null)
                request.Headers.Add(
                    ActorAssertionMiddleware.HeaderName,
                    Sign(actor, role, method.Method, target, body is null ? "" : "application/json", bytes));
            return await _client.SendAsync(request);
        }

        private string Sign(string subject, string role, string method, string requestTarget, string contentType, byte[] body)
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
            var encoded = Base64Url(payload);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_actorKey));
            return $"{encoded}.{Base64Url(hmac.ComputeHash(Encoding.ASCII.GetBytes(encoded)))}";
        }

        private static string Base64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

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
            => Task.FromResult(consumed.TryAdd(entry.Jti, 0) ? ActorAssertionReplayClaim.Consumed : ActorAssertionReplayClaim.Replay);
    }
}

/// <summary>The per-actor budget answers 429 for the actor who spent it, and nobody else.</summary>
public sealed class ActorRateLimitTests : IAsyncLifetime
{
    private readonly ResourceGuardPipelineTests.PipelineHost _host = new();

    public Task InitializeAsync() => _host.StartAsync(permitsPerMinute: 2);

    public Task DisposeAsync() => _host.DisposeAsync().AsTask();

    [Fact]
    public async Task AnActorPastTheirBudget_Gets429_WhileOthersAreServed()
    {
        for (var i = 0; i < 2; i++)
            (await _host.SendAsync(HttpMethod.Get, "/api/Events/event-public", actor: "busy-actor-00001")).StatusCode.Should().Be(HttpStatusCode.OK);

        var limited = await _host.SendAsync(HttpMethod.Get, "/api/Events/event-public", actor: "busy-actor-00001");
        var other = await _host.SendAsync(HttpMethod.Get, "/api/Events/event-public", actor: "calm-actor-00001");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        other.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

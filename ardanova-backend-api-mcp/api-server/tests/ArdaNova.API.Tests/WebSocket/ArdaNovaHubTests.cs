using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.API.Middleware;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Hubs;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArdaNova.API.Tests.WebSocket;

public class ArdaNovaHubTests
{
    private const string ValidApiKey = "test-api-key-012345678901234567890123456789";
    private const string ActorSigningKey = "actor-key-012345678901234567890123456789";
    private const string TestUserId = "user-123";
    private const string ConnectionId = "connection-1";

    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["API_KEY"] = ValidApiKey,
            ["ActorAssertion:HmacKey"] = ActorSigningKey
        })
        .Build();

    [Fact]
    public void Constructor_WithValidDependencies_CreatesHub()
    {
        var fixture = CreateHub();

        fixture.Hub.Should().NotBeNull();
    }

    [Fact]
    public async Task OnConnectedAsync_WithBearerActorAssertion_BindsActorAndJoinsPersonalGroup()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ArdaNovaHub.ApiKeyHeaderName] = ValidApiKey;
        httpContext.Request.Headers.Authorization = $"Bearer {CreateActorAssertion()}";
        var fixture = CreateHub(httpContext);

        await fixture.Hub.OnConnectedAsync();

        fixture.Context.Verify(context => context.Abort(), Times.Never);
        fixture.Groups.Verify(
            groups => groups.AddToGroupAsync(
                ConnectionId,
                $"user:{TestUserId}",
                It.IsAny<CancellationToken>()),
            Times.Once);
        httpContext.User.HasClaim(ActorAssertionMiddleware.ClaimType, "v2").Should().BeTrue();
    }

    [Fact]
    public async Task OnConnectedAsync_WithWebSocketQueryActorAssertion_JoinsPersonalGroup()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ArdaNovaHub.ApiKeyHeaderName] = ValidApiKey;
        httpContext.Request.QueryString = new QueryString(
            $"?{ArdaNovaHub.ActorAssertionQueryName}={CreateActorAssertion()}");
        var fixture = CreateHub(httpContext);

        await fixture.Hub.OnConnectedAsync();

        fixture.Context.Verify(context => context.Abort(), Times.Never);
        fixture.Groups.Verify(
            groups => groups.AddToGroupAsync(
                ConnectionId,
                $"user:{TestUserId}",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithoutActorAssertion_RejectsConnection()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ArdaNovaHub.ApiKeyHeaderName] = ValidApiKey;
        var fixture = CreateHub(httpContext);

        await fixture.Hub.OnConnectedAsync();

        fixture.Context.Verify(context => context.Abort(), Times.Once);
        fixture.Groups.Verify(
            groups => groups.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnConnectedAsync_WithWrongApiKey_RejectsConnection()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ArdaNovaHub.ApiKeyHeaderName] = "wrong-key";
        httpContext.Request.Headers.Authorization = $"Bearer {CreateActorAssertion()}";
        var fixture = CreateHub(httpContext);

        await fixture.Hub.OnConnectedAsync();

        fixture.Context.Verify(context => context.Abort(), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithUnsignedActorIdHeader_RejectsConnection()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ArdaNovaHub.ApiKeyHeaderName] = ValidApiKey;
        httpContext.Request.Headers["X-Ardanova-Actor-Id"] = TestUserId;
        var fixture = CreateHub(httpContext);

        await fixture.Hub.OnConnectedAsync();

        fixture.Context.Verify(context => context.Abort(), Times.Once);
        fixture.Groups.Verify(
            groups => groups.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnConnectedAsync_WithReplayedActorAssertion_RejectsSecondConnection()
    {
        var assertion = CreateActorAssertion();
        var ledger = new InMemoryActorAssertionReplayLedger();
        var firstContext = ContextWithCredentials(assertion);
        var secondContext = ContextWithCredentials(assertion);
        var first = CreateHub(firstContext, replayLedger: ledger);
        var second = CreateHub(secondContext, replayLedger: ledger);

        await first.Hub.OnConnectedAsync();
        await second.Hub.OnConnectedAsync();

        first.Context.Verify(context => context.Abort(), Times.Never);
        second.Context.Verify(context => context.Abort(), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithAssertionForDifferentTarget_RejectsConnection()
    {
        var httpContext = ContextWithCredentials(CreateActorAssertion(requestTarget: "/api/Projects"));
        var fixture = CreateHub(httpContext);

        await fixture.Hub.OnConnectedAsync();

        fixture.Context.Verify(context => context.Abort(), Times.Once);
    }

    [Fact]
    public async Task SubscribeToProject_WhenActorCreatedProject_JoinsWithoutMemberRow()
    {
        const string projectId = "project-1";
        var projectService = new Mock<IProjectService>();
        projectService
            .Setup(service => service.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto
            {
                Id = projectId,
                CreatedById = TestUserId
            }));
        var projectMemberService = new Mock<IProjectMemberService>();
        var fixture = CreateConnectedHub(
            projectService: projectService,
            projectMemberService: projectMemberService);

        await fixture.Hub.SubscribeToProject(projectId);

        fixture.Groups.Verify(
            groups => groups.AddToGroupAsync(
                ConnectionId,
                $"project:{projectId}",
                It.IsAny<CancellationToken>()),
            Times.Once);
        projectMemberService.Verify(
            service => service.GetByProjectIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubscribeToConversation_WhenActorIsNotMember_DoesNotJoinGroup()
    {
        const string conversationId = "conversation-1";
        var chatService = new Mock<IChatService>();
        chatService
            .Setup(service => service.GetConversationByIdAsync(
                conversationId,
                TestUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Forbidden("not a member"));
        var fixture = CreateConnectedHub(chatService);

        var action = () => fixture.Hub.SubscribeToConversation(conversationId);

        await action.Should().ThrowAsync<HubException>()
            .WithMessage("Conversation access denied.");
        fixture.Groups.Verify(
            groups => groups.AddToGroupAsync(
                ConnectionId,
                $"conversation:{conversationId}",
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendTypingIndicator_WhenActorIsNotMember_DoesNotBroadcast()
    {
        const string conversationId = "conversation-1";
        var chatService = new Mock<IChatService>();
        chatService
            .Setup(service => service.SendTypingIndicatorAsync(
                TestUserId,
                It.Is<TypingIndicatorDto>(dto =>
                    dto.ConversationId == conversationId && dto.IsTyping),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Forbidden("not a member"));
        var fixture = CreateConnectedHub(chatService);

        var action = () => fixture.Hub.SendTypingIndicator(conversationId, true);

        await action.Should().ThrowAsync<HubException>()
            .WithMessage("Conversation access denied.");
        fixture.Clients.Verify(
            clients => clients.OthersInGroup(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkAsRead_WhenActorIsNotMember_DoesNotBroadcast()
    {
        const string conversationId = "conversation-1";
        var chatService = new Mock<IChatService>();
        chatService
            .Setup(service => service.GetConversationByIdAsync(
                conversationId,
                TestUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Forbidden("not a member"));
        var fixture = CreateConnectedHub(chatService);

        var action = () => fixture.Hub.MarkAsRead(conversationId, "message-1");

        await action.Should().ThrowAsync<HubException>()
            .WithMessage("Conversation access denied.");
        fixture.Clients.Verify(
            clients => clients.OthersInGroup(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void Hub_DoesNotExposeGlobalSubscriptionMethods()
    {
        typeof(ArdaNovaHub).GetMethod("SubscribeToAll").Should().BeNull();
        typeof(ArdaNovaHub).GetMethod("UnsubscribeFromAll").Should().BeNull();
    }

    private HubFixture CreateConnectedHub(
        Mock<IChatService>? chatService = null,
        Mock<IProjectService>? projectService = null,
        Mock<IProjectMemberService>? projectMemberService = null)
    {
        var httpContext = ContextWithCredentials(CreateActorAssertion());
        var fixture = CreateHub(
            httpContext,
            chatService,
            projectService,
            projectMemberService);
        fixture.Hub.OnConnectedAsync().GetAwaiter().GetResult();
        return fixture;
    }

    private HubFixture CreateHub(
        HttpContext? httpContext = null,
        Mock<IChatService>? chatService = null,
        Mock<IProjectService>? projectService = null,
        Mock<IProjectMemberService>? projectMemberService = null,
        IActorAssertionReplayLedger? replayLedger = null)
    {
        httpContext ??= new DefaultHttpContext();
        chatService ??= new Mock<IChatService>();
        projectService ??= new Mock<IProjectService>();
        projectMemberService ??= new Mock<IProjectMemberService>();

        var httpContextFeature = new Mock<IHttpContextFeature>();
        httpContextFeature.SetupGet(feature => feature.HttpContext).Returns(httpContext);
        var features = new FeatureCollection();
        features.Set(httpContextFeature.Object);

        var context = new Mock<HubCallerContext>();
        context.SetupGet(value => value.ConnectionId).Returns(ConnectionId);
        context.SetupGet(value => value.ConnectionAborted).Returns(CancellationToken.None);
        context.SetupGet(value => value.Features).Returns(features);
        context.SetupGet(value => value.Items)
            .Returns(new Dictionary<object, object?>());

        var groups = new Mock<IGroupManager>();
        groups
            .Setup(manager => manager.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groups
            .Setup(manager => manager.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubCallerClients<IArdaNovaHubClient>>();
        var clientProxy = new Mock<IArdaNovaHubClient>();
        clients
            .Setup(value => value.OthersInGroup(It.IsAny<string>()))
            .Returns(clientProxy.Object);

        var hub = new ArdaNovaHub(
            NullLogger<ArdaNovaHub>.Instance,
            _configuration,
            replayLedger ?? new InMemoryActorAssertionReplayLedger(),
            projectService.Object,
            projectMemberService.Object,
            new Mock<IGuildService>().Object,
            new Mock<IGuildMemberService>().Object,
            chatService.Object)
        {
            Context = context.Object,
            Groups = groups.Object,
            Clients = clients.Object
        };

        return new HubFixture(hub, context, groups, clients);
    }

    private static DefaultHttpContext ContextWithCredentials(string assertion)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ArdaNovaHub.ApiKeyHeaderName] = ValidApiKey;
        httpContext.Request.Headers.Authorization = $"Bearer {assertion}";
        return httpContext;
    }

    private static string CreateActorAssertion(
        string subject = TestUserId,
        string requestTarget = ArdaNovaHub.ActorAssertionRequestTarget)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 2,
            issuer = "ardanova-next-bff",
            audience = "ardanova-api",
            subject,
            role = "INDIVIDUAL",
            method = "GET",
            requestTarget,
            contentType = string.Empty,
            bodySha256 = Convert.ToHexString(SHA256.HashData(Array.Empty<byte>())).ToLowerInvariant(),
            idempotencyKey = (string?)null,
            jti = Guid.NewGuid().ToString("D"),
            issuedAt = now,
            expiresAt = now + 90
        });
        var encodedPayload = Convert.ToBase64String(payload)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ActorSigningKey));
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload));
        var encodedSignature = Convert.ToBase64String(signature)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"{encodedPayload}.{encodedSignature}";
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

    private sealed record HubFixture(
        ArdaNovaHub Hub,
        Mock<HubCallerContext> Context,
        Mock<IGroupManager> Groups,
        Mock<IHubCallerClients<IArdaNovaHubClient>> Clients);
}

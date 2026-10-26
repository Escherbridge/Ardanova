namespace ArdaNova.Application.Tests.Azoa;

using System.Net;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// The worker-award hold lifecycle driven through the real <see cref="AzoaHoldClient"/> against a
/// handler scripted with the node's actual hold door (<c>HoldCommandsController</c>,
/// <c>api/hold-commands</c>) and its wire records (<c>HoldStartRequest</c>, <c>HoldCommandIntent</c>,
/// <c>HoldCommandView</c>, <c>HoldLiabilityView</c>, <c>HoldReserveAssetView</c>). A route the node
/// does not serve answers 404 here exactly as it does live — the bug this closes was
/// <c>POST /api/holds</c>. See Infrastructure/Azoa/AGENTS.md §Hold commands.
/// </summary>
public class AzoaHoldClientContractTests
{
    private static readonly Guid TenantUser = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid Worker = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");
    private const string EscrowHoldId = "award-bid-1";
    private const string WindowHoldId = "award-bid-1-window";

    // Literal vectors computed by the node's HoldCommandIdentity.Hold; the node pins the same
    // literals in tests/AZOA.WebAPI.Tests/Helpers/HoldCommandIdentityContractTests.cs.
    private const string DevnetEscrowRecordId = "94432cfe7b0e3f02c2d5f2250b39468f05b1e45c23092b44d96cf66ba3087e66";
    private const string TestnetWindowRecordId = "920b8f2030c870373ffcaffb60de513a260b6bbc452cddd4ddc605ec92ee6061";
    private const string TestnetEscapedProtocol = "SGO1GKSzyE7IEPItTxCByw9x8FmnrCDexi9/cOUJOiI=+";

    [Fact]
    public async Task AcceptingABid_OpensAReserveHoldThroughTheCommandDoor_AndReportsItHeld()
    {
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets(("Devnet", "genesis")))
            .Answer(HttpMethod.Post, "/api/hold-commands", CommandView("Open", "Completed", EscrowHoldId, "Devnet", "genesis"));

        var opened = await Client(node).OpenAsync(new AzoaHoldOpenRequest(
            new AzoaHoldReference("Algorand", EscrowHoldId), "42", "25000000", "contributors", Worker,
            ReleaseNotBefore: null, AutoRelease: false, Purpose: "Stake set aside for accepted project work"));

        opened.IsSuccess.Should().BeTrue(opened.Error);
        opened.Value!.State.Should().Be(AzoaHoldStates.Held);
        opened.Value.HoldId.Should().Be(EscrowHoldId);
        opened.Value.AmountBaseUnits.Should().Be("25000000");
        node.Paths.Should().Equal("GET /api/hold-commands/reserve-assets", "POST /api/hold-commands");

        var body = node.BodyOf(HttpMethod.Post, "/api/hold-commands");
        body.GetProperty("senderWalletId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("recipientWalletId").ValueKind.Should().Be(JsonValueKind.Null);
        var intent = body.GetProperty("intent");
        intent.GetProperty("version").GetInt32().Should().Be(1);
        intent.GetProperty("kind").GetString().Should().Be("Open");
        intent.GetProperty("requestKey").GetString().Should().Be("open:" + EscrowHoldId);
        intent.GetProperty("holdId").GetString().Should().Be(EscrowHoldId);
        intent.GetProperty("actorUserId").GetGuid().Should().Be(TenantUser);
        intent.GetProperty("ownerUserId").GetGuid().Should().Be(TenantUser);
        intent.GetProperty("tenantId").ValueKind.Should().Be(JsonValueKind.Null);
        intent.GetProperty("custodyTenantId").ValueKind.Should().Be(JsonValueKind.Null);
        AssertEchoedAsset(intent.GetProperty("asset"), "Devnet", "genesis");
        var definition = intent.GetProperty("definition");
        AssertEchoedAsset(definition.GetProperty("asset"), "Devnet", "genesis");
        definition.GetProperty("quantityBaseUnits").GetString().Should().Be("25000000");
        definition.GetProperty("fundingSource").GetString().Should().Be("Reserve");
        definition.GetProperty("funderUserId").ValueKind.Should().Be(JsonValueKind.Null);
        definition.GetProperty("beneficiaryUserId").GetGuid().Should().Be(Worker);
        definition.GetProperty("reservePartition").GetString().Should().Be("contributors");
        definition.GetProperty("autoRelease").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData("Devnet", "genesis", EscrowHoldId, DevnetEscrowRecordId)]
    [InlineData("Testnet", TestnetEscapedProtocol, WindowHoldId, TestnetWindowRecordId)]
    public async Task ReadingAHold_AddressesTheLiabilityByTheNodesOwnRecordId(
        string network, string protocol, string holdId, string recordId)
    {
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets((network, protocol)))
            .Answer(HttpMethod.Get, "/api/hold-commands/liabilities/" + recordId,
                Liability(holdId, "Held", network, protocol));

        var read = await Client(node).GetAsync(new AzoaHoldReference("Algorand", holdId));

        read.IsSuccess.Should().BeTrue(read.Error);
        read.Value!.State.Should().Be(AzoaHoldStates.Held);
        read.Value.ReservePartition.Should().Be("contributors");
        read.Value.BeneficiaryReference.Should().Be(Worker.ToString());
    }

    [Fact]
    public async Task ReadingAHoldTheNodeDoesNotHold_IsNotFound()
    {
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets(("Devnet", "genesis")));

        var read = await Client(node).GetAsync(new AzoaHoldReference("Algorand", "award-unknown"));

        read.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task ApprovingWork_ContinuesTheLiveLiabilityIntoTheWorkersWindow()
    {
        var window = DateTimeOffset.Parse("2027-01-23T00:00:00Z");
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets(("Devnet", "genesis")))
            .Answer(HttpMethod.Get, "/api/hold-commands/liabilities/" + DevnetEscrowRecordId,
                Liability(EscrowHoldId, "Held", "Devnet", "genesis"))
            .Answer(HttpMethod.Post, "/api/hold-commands",
                CommandView("ReleaseIntoHold", "Completed", EscrowHoldId, "Devnet", "genesis"));

        var continued = await Client(node).ReleaseIntoHoldAsync(new AzoaHoldChainRequest(
            new AzoaHoldReference("Algorand", EscrowHoldId), WindowHoldId, Worker,
            ReleaseNotBefore: window, AutoRelease: false, Purpose: "Awarded stake"));

        continued.IsSuccess.Should().BeTrue(continued.Error);
        continued.Value!.State.Should().Be(AzoaHoldStates.ReleasedIntoHold);
        continued.Value.ChildHoldId.Should().Be(WindowHoldId);

        var intent = node.BodyOf(HttpMethod.Post, "/api/hold-commands").GetProperty("intent");
        intent.GetProperty("kind").GetString().Should().Be("ReleaseIntoHold");
        intent.GetProperty("requestKey").GetString().Should().Be($"release-into-hold:{EscrowHoldId}:{WindowHoldId}");
        intent.GetProperty("ownerUserId").GetGuid().Should().Be(TenantUser);
        intent.GetProperty("definition").ValueKind.Should().Be(JsonValueKind.Null);
        AssertEchoedAsset(intent.GetProperty("asset"), "Devnet", "genesis");
        intent.GetProperty("nextHoldId").GetString().Should().Be(WindowHoldId);
        intent.GetProperty("nextBeneficiaryUserId").GetGuid().Should().Be(Worker);
        intent.GetProperty("nextReleaseNotBefore").GetDateTimeOffset().Should().Be(window);
        intent.GetProperty("nextAutoRelease").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task RefundingAnAward_GivesTheCommitmentBackThroughTheCommandDoor()
    {
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets(("Devnet", "genesis")))
            .Answer(HttpMethod.Get, "/api/hold-commands/liabilities/" + DevnetEscrowRecordId,
                Liability(EscrowHoldId, "Held", "Devnet", "genesis"))
            .Answer(HttpMethod.Post, "/api/hold-commands", CommandView("Refund", "Completed", EscrowHoldId, "Devnet", "genesis"));

        var refunded = await Client(node).RefundAsync(new AzoaHoldReference("Algorand", EscrowHoldId), "bid withdrawn");

        refunded.IsSuccess.Should().BeTrue(refunded.Error);
        refunded.Value!.State.Should().Be(AzoaHoldStates.Refunded);
        var intent = node.BodyOf(HttpMethod.Post, "/api/hold-commands").GetProperty("intent");
        intent.GetProperty("kind").GetString().Should().Be("Refund");
        intent.GetProperty("requestKey").GetString().Should().Be("refund:" + EscrowHoldId);
        intent.GetProperty("reason").GetString().Should().Be("bid withdrawn");
    }

    [Fact]
    public async Task ANodeDeclinedOpening_PropagatesAsADecline()
    {
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets(("Devnet", "genesis")))
            .Answer(HttpMethod.Post, "/api/hold-commands",
                """{"result":null,"isError":true,"message":"partition oversubscribed: internal detail"}""",
                HttpStatusCode.BadRequest);

        var opened = await Client(node).OpenAsync(OpenRequest());

        opened.Type.Should().Be(ResultType.BadRequest);
        opened.Error.Should().NotContain("internal detail");
    }

    public static TheoryData<string, string, bool, ResultType, int> RefusedBeforeTheCommand => new()
    {
        // tenant setting, asset id, auto-release, refusal, node calls made
        { "", "42", false, ResultType.Failure, 0 },
        { TenantUser.ToString(), "42", true, ResultType.ValidationError, 0 },
        { TenantUser.ToString(), "not-registered", false, ResultType.NotFound, 1 },
    };

    [Theory]
    [MemberData(nameof(RefusedBeforeTheCommand))]
    public async Task AnOpeningTheNodeWouldRefuse_IsRefusedBeforeAnyCommandIsSent(
        string tenantId, string assetId, bool autoRelease, ResultType refusal, int calls)
    {
        var node = new ScriptedNode()
            .Answer(HttpMethod.Get, "/api/hold-commands/reserve-assets", ReserveAssets(("Devnet", "genesis")));

        var opened = await Client(node, tenantId).OpenAsync(
            OpenRequest() with { AssetId = assetId, AutoRelease = autoRelease });

        opened.Type.Should().Be(refusal);
        node.Paths.Should().HaveCount(calls).And.NotContain("POST /api/hold-commands");
    }

    private static AzoaHoldOpenRequest OpenRequest() => new(
        new AzoaHoldReference("Algorand", EscrowHoldId), "42", "25000000", "contributors", Worker,
        ReleaseNotBefore: null, AutoRelease: false, Purpose: null);

    private static void AssertEchoedAsset(JsonElement asset, string network, string protocol)
    {
        asset.GetProperty("chainType").GetString().Should().Be("Algorand");
        asset.GetProperty("network").GetString().Should().Be(network);
        asset.GetProperty("protocolIdentity").GetString().Should().Be(protocol);
        asset.GetProperty("assetId").GetString().Should().Be("42");
        asset.GetProperty("holdingAddress").GetString().Should().Be("RESERVEADDR");
    }

    private static AzoaHoldClient Client(ScriptedNode node, string? tenantId = null)
        => new(
            new HttpClient(node) { BaseAddress = new Uri("https://azoa.test") },
            NullLogger<AzoaHoldClient>.Instance,
            Options.Create(new AzoaSettings { ChainType = "Algorand", TenantId = tenantId ?? TenantUser.ToString() }));

    private static string Asset(string network, string protocol)
        => $$"""{"chainType":"Algorand","network":"{{network}}","protocolIdentity":{{JsonSerializer.Serialize(protocol)}},"assetId":"42","holdingAddress":"RESERVEADDR"}""";

    /// <summary>AZOAResult&lt;IReadOnlyList&lt;HoldReserveAssetView&gt;&gt;.</summary>
    private static string ReserveAssets(params (string Network, string Protocol)[] assets)
        => Envelope("[" + string.Join(",", assets.Select(asset =>
            $$"""{"asset":{{Asset(asset.Network, asset.Protocol)}},"unitName":"PROJ","decimals":6,"partitions":[{"name":"contributors","declaredBaseUnits":"100000000","availableBaseUnits":"75000000"}]}"""))
            + "]");

    /// <summary>AZOAResult&lt;HoldCommandView&gt; as Render emits it.</summary>
    private static string CommandView(string kind, string state, string holdId, string network, string protocol)
        => Envelope($$"""
            {"operationId":"op-1","version":0,"kind":"{{kind}}","state":"{{state}}","holdRecordId":"{{DevnetEscrowRecordId}}",
             "holdId":"{{holdId}}","asset":{{Asset(network, protocol)}},"quantityBaseUnits":"25000000",
             "actorUserId":"{{TenantUser}}","ownerUserId":"{{TenantUser}}","tenantId":null,"beneficiaryUserId":"{{Worker}}",
             "transactionId":null,"detail":null,"nextHoldRecordId":null,"custodyTenantId":null,"costQuote":null,
             "acceptanceState":null,"generation":0}
            """);

    /// <summary>AZOAResult&lt;HoldLiabilityView&gt;.</summary>
    private static string Liability(string holdId, string state, string network, string protocol)
        => Envelope($$"""
            {"holdRecordId":"rec","holdId":"{{holdId}}","asset":{{Asset(network, protocol)}},"quantityBaseUnits":"25000000",
             "state":"{{state}}","ownerUserId":"{{TenantUser}}","beneficiaryUserId":"{{Worker}}","funderUserId":null,
             "custodyTenantId":null,"fundingSource":"Reserve","reservePartition":"contributors","releaseNotBefore":null,
             "autoRelease":false,"activeReleaseMandateId":null,"purchased":false,"openingCommandId":"op-1",
             "createdAt":"2026-10-25T00:00:00+00:00","updatedAt":"2026-10-25T00:00:00+00:00"}
            """);

    private static string Envelope(string resultJson) => $$"""{"result":{{resultJson}},"isError":false,"message":null}""";

    /// <summary>Answers only the routes it was scripted with; every other route is the node's 404.</summary>
    private sealed class ScriptedNode : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = new();
        private readonly List<(string Route, string Body)> _sent = new();

        public IReadOnlyList<string> Paths => _sent.Select(request => request.Route).ToList();

        public ScriptedNode Answer(HttpMethod method, string path, string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _routes[method.Method + " " + path] = (status, body);
            return this;
        }

        public JsonElement BodyOf(HttpMethod method, string path)
            => JsonDocument.Parse(_sent.Single(request => request.Route == method.Method + " " + path).Body).RootElement;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var route = request.Method.Method + " " + request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            _sent.Add((route, body));

            var (status, answer) = _routes.TryGetValue(route, out var scripted)
                ? scripted
                : (HttpStatusCode.NotFound, string.Empty);
            return new HttpResponseMessage(status) { Content = new StringContent(answer, Encoding.UTF8, "application/json") };
        }
    }
}

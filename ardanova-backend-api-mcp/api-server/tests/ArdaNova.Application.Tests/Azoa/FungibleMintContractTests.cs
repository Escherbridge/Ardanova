namespace ArdaNova.Application.Tests.Azoa;

using System.Net;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// Pins the wire shape of <c>POST /api/nft/fungible-mint</c> against AZOA's real
/// DTOs — <c>Models/Requests/FungibleMintRequest.cs</c> and
/// <c>Models/Responses/FungibleTokenResult.cs</c> in the AZOA repo (oasis-sleek).
/// ArdaNova cannot build or run the AZOA solution from this repo, so these tests
/// are the closest buildable stand-in for an OpenAPI schema diff: they assert the
/// exact JSON property names and CLR types AZOA's controller actually binds/emits,
/// literally transcribed from its source at the time this test was written. If
/// AZOA's DTO shape changes, this test (and the wire models it pins) must change
/// with it — that drift is the whole point of the guard.
///
/// The bug this closes: <see cref="AzoaFungibleMintRequest"/> used to serialize
/// total supply as an opaque JSON string (<c>"totalSupply"</c>), mirroring the
/// allocation door's convention. AZOA's <c>FungibleMintRequest.Total</c> is a real
/// <c>ulong</c> bound from a JSON number named <c>"total"</c>; a string body
/// silently deserialized to <c>Total = 0</c> and the node rejected the call with
/// "zero total" on every single mint once <c>Algorand:Provider</c> was set to
/// <c>Azoa</c>.
/// </summary>
public class FungibleMintContractTests
{
    private static readonly JsonSerializerOptions WireJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ========================================================================
    // Request shape — must match AZOA's FungibleMintRequest exactly
    // ========================================================================

    [Fact]
    public void AzoaFungibleMintRequest_SerializesTotalAsANumber_NotAnOpaqueString()
    {
        // Arrange
        var request = new AzoaFungibleMintRequest
        {
            ChainType = "Algorand",
            Name = "ARDA Token",
            UnitName = "ARDA",
            Total = 1_000_000_000UL,
            Decimals = 6,
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);

        // Assert — "total" is a JSON number, matching AZOA's ulong-typed field.
        doc.RootElement.TryGetProperty("total", out var totalElement).Should().BeTrue();
        totalElement.ValueKind.Should().Be(JsonValueKind.Number);
        totalElement.GetUInt64().Should().Be(1_000_000_000UL);

        // Assert — the legacy opaque-string field name must never come back.
        doc.RootElement.TryGetProperty("totalSupply", out _).Should().BeFalse(
            "AZOA's FungibleMintRequest has no totalSupply field; a string body here " +
            "deserializes Total to 0 and every mint fails with \"zero total\"");
    }

    [Fact]
    public void AzoaFungibleMintRequest_SerializesDecimalsAsANumber()
    {
        // Arrange
        var request = new AzoaFungibleMintRequest
        {
            ChainType = "Algorand",
            Name = "Proj Token",
            UnitName = "PROJ",
            Total = 42,
            Decimals = 6,
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.GetProperty("decimals").ValueKind.Should().Be(JsonValueKind.Number);
        doc.RootElement.GetProperty("decimals").GetInt32().Should().Be(6);
    }

    [Fact]
    public void AzoaFungibleMintRequest_JsonPropertyNames_MatchAzoaFungibleMintRequestExactly()
    {
        // Arrange — the full field set AZOA's controller binds for this route
        // (chainType, name, unitName, total, decimals; holonId is optional and
        // intentionally unset by this adapter — the legacy IAlgorandService
        // surface carries no holon context to link).
        var request = new AzoaFungibleMintRequest
        {
            ChainType = "Algorand",
            Name = "ARDA Token",
            UnitName = "ARDA",
            Total = 1,
            Decimals = 6,
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);
        var actualPropertyNames = doc.RootElement.EnumerateObject()
            .Select(p => p.Name)
            .ToArray();

        // Assert — exactly the wire names AZOA's FungibleMintRequest binds.
        actualPropertyNames.Should().BeEquivalentTo(new[]
        {
            "chainType", "name", "unitName", "total", "decimals",
        });
    }

    [Fact]
    public void AzoaFungibleMintRequest_TotalProperty_IsUlongNotString()
    {
        // A reflection guard against a future regression back to an opaque
        // string amount (the exact shape of the original defect).
        var property = typeof(AzoaFungibleMintRequest).GetProperty(nameof(AzoaFungibleMintRequest.Total));

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(ulong));
    }

    // ========================================================================
    // Response shape — must match AZOA's FungibleTokenResult exactly
    // ========================================================================

    [Fact]
    public void AzoaFungibleMintResult_DeserializesAzoaFungibleTokenResultShape()
    {
        // Arrange — literal transcription of AZOA's FungibleTokenResult wire
        // shape (Models/Responses/FungibleTokenResult.cs): operation (the admitted
        // FungibleCreationView), userId, walletId, walletAddress, walletProvisioned,
        // assetId (empty until confirmed), replayed.
        var azoaResponseJson = $$"""
            {
              "operation": {{View("AwaitingConsent", 0)}},
              "userId": "11111111-1111-1111-1111-111111111111",
              "walletId": "22222222-2222-2222-2222-222222222222",
              "walletAddress": "PLATFORMRESERVEADDR",
              "walletProvisioned": true,
              "assetId": "",
              "replayed": false
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<AzoaFungibleMintResult>(azoaResponseJson, WireJsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        result.WalletId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        result.WalletAddress.Should().Be("PLATFORMRESERVEADDR");
        result.WalletProvisioned.Should().BeTrue();
        result.AssetId.Should().BeEmpty();
        result.Replayed.Should().BeFalse();
        result.Operation.Should().NotBeNull();
        result.Operation!.OperationId.Should().Be(OperationId);
        result.Operation.State.Should().Be("AwaitingConsent");
    }

    [Fact]
    public void AzoaFungibleMintResult_HasNoTxHashOrOperationIdProperty()
    {
        // AZOA's FungibleTokenResult never carried these fields; the adapter's
        // old wire model read them anyway (they always deserialized to the CLR
        // default from a JSON payload that never sent them). Guard against
        // reintroducing a field that silently reads as empty forever.
        typeof(AzoaFungibleMintResult).GetProperty("TxHash").Should().BeNull();
        typeof(AzoaFungibleMintResult).GetProperty("OperationId").Should().BeNull();
    }

    // ========================================================================
    // End-to-end — the adapter method, wired against the pinned request/response
    // ========================================================================

    // Flows below drive the real AzoaNodeClient (route policy, header, envelope) over a scripted
    // HTTP edge that answers like the node did in the 2026-10-25 live drive:
    // mint 202 AwaitingConsent → quote 202 v1 → accept 202 Admitted v2 → continue 202 Submitted v7
    // → refresh 200 Completed v9.

    private const string OperationId = "9f2c4e6a8b0d1f3a5c7e9b1d3f5a7c9e";
    private const string GenesisKey = "project-token-genesis:cfg-1";
    private const string ChainAssetId = "6260732372893272157";
    private const string StepPrefix = "/api/fungible-creation/" + OperationId;

    [Fact]
    public async Task CreateFungibleASAAsync_WalksTheNodesConsentLifecycle_ToTheCompletedViewsAssetId()
    {
        var node = new ScriptedNode()
            .On("/api/nft/fungible-mint", HttpStatusCode.Accepted, MintJson(View("AwaitingConsent", 0)))
            .On($"{StepPrefix}/quote", HttpStatusCode.Accepted, View("AwaitingConsent", 1, quoteId: "q-1"))
            .On($"{StepPrefix}/accept-quote", HttpStatusCode.Accepted, View("Admitted", 2, quoteId: "q-1"))
            .On($"{StepPrefix}/continue", HttpStatusCode.Accepted, View("Submitted", 7, quoteId: "q-1", transactionId: "TXCREATE"))
            .On($"{StepPrefix}/refresh", HttpStatusCode.Accepted, View("Submitted", 8, quoteId: "q-1", transactionId: "TXCREATE"))
            .On($"{StepPrefix}/refresh", HttpStatusCode.OK,
                View("Completed", 9, quoteId: "q-1", transactionId: "TXCREATE", assetId: ChainAssetId));

        var result = await NewAdapter(node).CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000, GenesisKey);

        // The stake exists, named by the TERMINAL view's asset id and creation transaction.
        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.AssetId.Should().Be(ChainAssetId);
        result.Value.TxHash.Should().Be("TXCREATE");
        result.Value.ChainType.Should().Be("Algorand");

        // The mint carried the declaration's key (a blank one is a 400 on the node) and a real
        // numeric supply at the shared asset scale.
        var mint = node.Sent.First(request => request.Path == "/api/nft/fungible-mint");
        mint.IdempotencyKey.Should().Be(GenesisKey);
        using (var mintBody = JsonDocument.Parse(mint.Body))
        {
            mintBody.RootElement.GetProperty("total").GetUInt64().Should().Be(1_000_000_000UL);
            mintBody.RootElement.GetProperty("decimals").GetInt32().Should().Be(6);
        }

        // Acceptance echoes the quote and version the node offered, and continuation authorizes
        // the exact version the acceptance produced - stale echoes are refused by the node.
        using (var accept = JsonDocument.Parse(node.Sent.Single(request => request.Path.EndsWith("/accept-quote")).Body))
        {
            accept.RootElement.GetProperty("quoteId").GetString().Should().Be("q-1");
            accept.RootElement.GetProperty("version").GetInt64().Should().Be(1);
        }

        using (var proceed = JsonDocument.Parse(node.Sent.Single(request => request.Path.EndsWith("/continue")).Body))
        {
            proceed.RootElement.GetProperty("version").GetInt64().Should().Be(2);
            proceed.RootElement.GetProperty("requestId").GetGuid().Should().NotBe(Guid.Empty);
        }
    }

    [Fact]
    public async Task CreateFungibleASAAsync_AReplayThatIsAlreadyCompleted_ReturnsItsAssetWithoutDrivingAnyStep()
    {
        // A retry of a finished declaration: the node replays the keyed admission as Completed.
        var node = new ScriptedNode()
            .On("/api/nft/fungible-mint", HttpStatusCode.OK,
                MintJson(View("Completed", 9, quoteId: "q-1", assetId: ChainAssetId), replayed: true));

        var result = await NewAdapter(node).CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000, GenesisKey);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.AssetId.Should().Be(ChainAssetId);
        node.Sent.Select(request => request.Path).Should().Equal(
            ["/api/nft/fungible-mint"],
            "a finished creation has no consent left to give; any step would be a stray value-key call");
    }

    [Fact]
    public async Task CreateFungibleASAAsync_ReportsTheChainTheNodeMintedOn_NotTheOneItAskedFor()
    {
        // The adapter asks for Algorand; a Blockchain:Mode=Simulated node redirects the creation and
        // says so on the view. Treasury and partitions must follow the view, not the request.
        var node = new ScriptedNode()
            .On("/api/nft/fungible-mint", HttpStatusCode.OK,
                MintJson(View("Completed", 9, quoteId: "q-1", assetId: ChainAssetId, chainType: "Simulated"), replayed: true));

        var result = await NewAdapter(node).CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000, GenesisKey);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.ChainType.Should().Be("Simulated");
    }

    [Fact]
    public async Task CreateFungibleASAAsync_ACreationThatNeverLeavesSubmitted_FailsHonestlyWhenTheBudgetRunsOut()
    {
        var node = new ScriptedNode()
            .On("/api/nft/fungible-mint", HttpStatusCode.Accepted, MintJson(View("Submitted", 7, quoteId: "q-1")))
            .On($"{StepPrefix}/refresh", HttpStatusCode.Accepted, View("Submitted", 8, quoteId: "q-1"));
        var budget = new AzoaFungibleCreationBudget(TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(10));

        var result = await NewAdapter(node, budget).CreateFungibleASAAsync("ARDA Token", "ARDA", 1000, GenesisKey);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("last state: Submitted")
            .And.Contain("resumes the same creation");
        node.Sent.Should().Contain(request => request.Path == $"{StepPrefix}/refresh",
            "the budget is spent polling the node, not waiting blind");
    }

    private static AzoaBackedAlgorandService NewAdapter(ScriptedNode node, AzoaFungibleCreationBudget? budget = null) =>
        new(
            new AzoaNodeClient(new HttpClient(node) { BaseAddress = new Uri("https://azoa.test") },
                NullLogger<AzoaNodeClient>.Instance),
            Options.Create(new AzoaSettings { BaseUrl = "https://azoa.test", ValueApiKey = "k", ChainType = "Algorand" }),
            NullLogger<AzoaBackedAlgorandService>.Instance,
            budget ?? new AzoaFungibleCreationBudget(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1)));

    /// <summary>Literal transcription of AZOA's <c>FungibleCreationView</c> as the node serializes it.</summary>
    private static string View(
        string state, long version, string? quoteId = null, string? transactionId = null, string? assetId = null,
        string chainType = "Algorand")
    {
        var quote = quoteId is null
            ? "null"
            : $$"""{"quoteId":"{{quoteId}}","intentHash":"ih","protocolIdentity":"algorand:testnet","networkFeeBaseUnits":"1000","minimumBalanceBaseUnits":"100000","currency":"ALGO","decimals":6,"payerAddress":"RESERVE","expiresAt":"2026-10-25T12:00:00+00:00"}""";

        return $$"""{"operationId":"{{OperationId}}","version":{{version}},"state":"{{state}}","chainType":"{{chainType}}","network":"TestNet","assetId":{{Json(assetId)}},"transactionId":{{Json(transactionId)}},"detail":null,"quote":{{quote}},"walletId":"22222222-2222-2222-2222-222222222222","walletAddress":"RESERVE","walletProvisioned":true,"requestedHolonId":null,"purpose":"Participant"}""";
    }

    private static string MintJson(string operationJson, bool replayed = false) =>
        $$"""{"operation":{{operationJson}},"userId":"11111111-1111-1111-1111-111111111111","walletId":"22222222-2222-2222-2222-222222222222","walletAddress":"RESERVE","walletProvisioned":true,"assetId":"","replayed":{{(replayed ? "true" : "false")}}}""";

    private static string Json(string? value) => value is null ? "null" : $"\"{value}\"";

    /// <summary>
    /// The HTTP edge: answers each path from its own script in order, repeating the last answer,
    /// and records what left the process. An unscripted path is a 404, as on the node.
    /// </summary>
    private sealed class ScriptedNode : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<(HttpStatusCode Status, string Result)>> _scripts = new();
        private readonly Dictionary<string, (HttpStatusCode Status, string Result)> _last = new();

        public List<SentRequest> Sent { get; } = new();

        public ScriptedNode On(string path, HttpStatusCode status, string resultJson)
        {
            if (!_scripts.TryGetValue(path, out var script))
                _scripts[path] = script = new Queue<(HttpStatusCode, string)>();
            script.Enqueue((status, resultJson));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Sent.Add(new SentRequest(
                path,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.FirstOrDefault() : null));

            if (_scripts.TryGetValue(path, out var script) && script.Count > 0)
                _last[path] = script.Dequeue();

            if (!_last.TryGetValue(path, out var answer))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return new HttpResponseMessage(answer.Status)
            {
                Content = new StringContent(
                    """{"isError":false,"message":"Success","result":""" + answer.Result + "}",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    private sealed record SentRequest(string Path, string Body, string? IdempotencyKey);
}

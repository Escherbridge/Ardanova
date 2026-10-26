namespace ArdaNova.Application.Tests.Azoa;

using System.Net;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// The three node transports L2 wired, tested where they actually refuse: at the request.
/// </summary>
/// <remarks>
/// <para>Every test here drives the PUBLIC adapter against a handler that records what left the
/// process. A path the policy refuses must produce a refusal AND an empty recording - the
/// distinction matters, because a client that sends first and refuses afterwards has already
/// presented a scoped credential to a route family it was never meant to reach.</para>
/// <para>The response bodies are literal transcriptions of the AZOA wire shapes these adapters
/// bind: <c>ProjectTreasuryTenantController.TenantProjectTreasuryResponse</c>,
/// <c>PlatformReserveController.ReservePartitionsView</c>, <c>Models/UserNFT.cs</c> and
/// <c>FiatPayoutController</c>'s payout view, all wrapped in the node's
/// <c>AZOAResult&lt;T&gt;</c> envelope. ArdaNova cannot build the AZOA solution from this repo,
/// so - as with <see cref="FungibleMintContractTests"/> - these transcriptions are the closest
/// buildable stand-in for a schema diff, and drift is meant to break them.</para>
/// </remarks>
public class AzoaNodeTransportAdapterTests
{
    private static readonly Uri NodeBase = new("https://azoa.test");

    private static readonly Guid Recipient = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    // ========================================================================
    // The value credential's route policy — AzoaNodeClient.PostAsync
    // ========================================================================

    [Fact]
    public async Task PostAsync_ReachesTheFungibleMintDoor()
    {
        var handler = RespondingWith(FungibleMintResultJson);
        var client = new AzoaNodeClient(HttpFor(handler), NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<AzoaFungibleMintResult>(
            "/api/nft/fungible-mint", new { chainType = "Algorand" });

        result.IsSuccess.Should().BeTrue();
        handler.Sent.Should().ContainSingle()
            .Which.Uri.AbsolutePath.Should().Be("/api/nft/fungible-mint");
    }

    [Fact]
    public async Task PostAsync_ReachesTheReservePartitionDoor()
    {
        // The allowance door is the second half of creating a stake, so the value key that
        // created it may declare its budgets - and nothing beyond those two.
        var handler = RespondingWith(FungibleMintResultJson);
        var client = new AzoaNodeClient(HttpFor(handler), NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<AzoaFungibleMintResult>(
            "/api/reserve/12345/partitions", new { chain = "Algorand" });

        result.IsSuccess.Should().BeTrue();
        handler.Sent.Should().ContainSingle()
            .Which.Uri.AbsolutePath.Should().Be("/api/reserve/12345/partitions");
    }

    [Theory]
    [InlineData("quote")]
    [InlineData("accept-quote")]
    [InlineData("continue")]
    [InlineData("refresh")]
    public async Task PostAsync_ReachesTheMintsOwnConsentLifecycleSteps(string step)
    {
        // The key that created a stake must be able to see that creation through.
        var handler = RespondingWith(FungibleMintResultJson);
        var client = new AzoaNodeClient(HttpFor(handler), NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<AzoaFungibleMintResult>(
            $"/api/fungible-creation/9f2c4e6a8b0d1f3a/{step}", null);

        result.IsSuccess.Should().BeTrue();
        handler.Sent.Should().ContainSingle()
            .Which.Uri.AbsolutePath.Should().Be($"/api/fungible-creation/9f2c4e6a8b0d1f3a/{step}");
    }

    [Theory]
    // Another credential's route family: the value key must never reach tenant custody.
    [InlineData("/api/tenant/custodial-accounts")]
    // The consent door L2.7 uses; it is the CUSTODY key's, not this one's.
    [InlineData("/api/tenant/consent/provisioned")]
    // The treasury door this wave opened, on the custody key — also not this one's.
    [InlineData("/api/projects/treasury/resolve")]
    // A query string: the shared path policy refuses '?' outright, so a caller that needs one
    // guards the bare path and appends afterwards.
    [InlineData("/api/reserve/12345/partitions?chain=Algorand")]
    // Traversal out of the reserve namespace and into custody.
    [InlineData("/api/reserve/../tenant/custodial-accounts/partitions")]
    // An asset id that is really two segments — a way to address a route that is not a partition.
    [InlineData("/api/reserve/12345/steal/partitions")]
    // Right namespace, wrong door.
    [InlineData("/api/reserve/12345/draw")]
    // Prefix and suffix both satisfied with no asset id between them.
    [InlineData("/api/reserve/partitions")]
    [InlineData("/api/reserve//partitions")]
    // Fungible-creation doors that are NOT the mint's own lifecycle: undoing or re-pointing a
    // creation is not authority the value key was handed.
    [InlineData("/api/fungible-creation/op123/cancel")]
    [InlineData("/api/fungible-creation/op123/replace")]
    [InlineData("/api/fungible-creation/op123/attachment")]
    // The participant listing, and a lifecycle step with no operation between prefix and step.
    [InlineData("/api/fungible-creation")]
    [InlineData("/api/fungible-creation//refresh")]
    // An operation id that is really two segments, or a traversal out of the namespace.
    [InlineData("/api/fungible-creation/op123/extra/refresh")]
    [InlineData("/api/fungible-creation/../tenant/refresh")]
    public async Task PostAsync_RefusesAPathOutsideTheValueFamily_WithoutSendingAnything(string path)
    {
        var handler = RespondingWith(FungibleMintResultJson);
        var client = new AzoaNodeClient(HttpFor(handler), NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<AzoaFungibleMintResult>(path, new { });

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        handler.Sent.Should().BeEmpty(
            "a credential refused after the request has left has already been presented");
    }

    // ========================================================================
    // Project funding — the treasury leg (custody credential)
    // ========================================================================

    [Fact]
    public async Task ResolveTreasuryAsync_AsksForTheProjectByChainAndReferenceAlone()
    {
        var (gateway, treasury, _) = BuildGateway(
            treasuryJson: TreasuryJson("Algorand", "proj-7", "TREASURYADDR", "Open"));

        var result = await gateway.ResolveTreasuryAsync(new AzoaProjectTreasuryRequest("Algorand", "proj-7"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Address.Should().Be("TREASURYADDR");
        result.Value.Status.Should().Be("Open");
        result.Value.ProjectReference.Should().Be("proj-7");

        var sent = treasury.Sent.Should().ContainSingle().Subject;
        sent.Uri.AbsolutePath.Should().Be("/api/projects/treasury/resolve");

        using var body = JsonDocument.Parse(sent.Body);
        body.RootElement.GetProperty("chainType").GetString().Should().Be("Algorand");
        body.RootElement.GetProperty("projectReference").GetString().Should().Be("proj-7");
        // A tenant on the body would be a way to reach another tenant's treasuries; the node
        // reads the tenant off the credential instead.
        body.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(new[] { "chainType", "projectReference" });
    }

    [Fact]
    public async Task ResolveTreasuryAsync_RefusesAnAccountThatNamesADifferentProject()
    {
        // The address is written into config.treasuryAddress and the project is then called
        // funded. Another project's account here points this project's funding at that one.
        var (gateway, _, _) = BuildGateway(
            treasuryJson: TreasuryJson("Algorand", "proj-OTHER", "OTHERADDR", "Open"));

        var result = await gateway.ResolveTreasuryAsync(new AzoaProjectTreasuryRequest("Algorand", "proj-7"));

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task ResolveTreasuryAsync_RefusesAnAccountWithNoAddress()
    {
        var (gateway, _, _) = BuildGateway(
            treasuryJson: TreasuryJson("Algorand", "proj-7", string.Empty, "Open"));

        var result = await gateway.ResolveTreasuryAsync(new AzoaProjectTreasuryRequest("Algorand", "proj-7"));

        result.IsSuccess.Should().BeFalse();
    }

    // ========================================================================
    // Project funding — the allowance leg (value credential)
    // ========================================================================

    [Fact]
    public async Task DeclareReservePartitionsAsync_SendsTheBudgetsAndTheRailInTheBody()
    {
        var (gateway, _, value) = BuildGateway(
            partitionsJson: PartitionsJson("12345", ("contributor", "600000"), ("investor", "400000")));

        var result = await gateway.DeclareReservePartitionsAsync(new AzoaReservePartitionRequest(
            "Algorand",
            "12345",
            new Dictionary<string, string> { ["contributor"] = "600000", ["investor"] = "400000" }));

        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().Be("12345");
        result.Value.DeclaredBaseUnitsByPartition["contributor"].Should().Be("600000");
        result.Value.DeclaredBaseUnitsByPartition["investor"].Should().Be("400000");

        var sent = value.Sent.Should().ContainSingle().Subject;
        sent.Uri.AbsolutePath.Should().Be("/api/reserve/12345/partitions");
        // The rail travels in the body, never as '?chain=' — a query string is refused before
        // the request is built.
        sent.Uri.Query.Should().BeEmpty();

        using var body = JsonDocument.Parse(sent.Body);
        body.RootElement.GetProperty("chain").GetString().Should().Be("Algorand");
        var partitions = body.RootElement.GetProperty("partitions");
        partitions.GetProperty("contributor").GetString().Should().Be("600000");
        partitions.GetProperty("investor").GetString().Should().Be("400000");
    }

    [Fact]
    public async Task DeclareReservePartitionsAsync_FailsWhenTheNodeRecordedADifferentBudget()
    {
        // The node SETS a budget rather than adding to it, so the echo is what a draw will be
        // measured against. Accepting a smaller one would open the project on allowances it
        // never declared — and the caller's next act is to call the project fundable.
        var (gateway, _, _) = BuildGateway(
            partitionsJson: PartitionsJson("12345", ("contributor", "600000"), ("investor", "1")));

        var result = await gateway.DeclareReservePartitionsAsync(new AzoaReservePartitionRequest(
            "Algorand",
            "12345",
            new Dictionary<string, string> { ["contributor"] = "600000", ["investor"] = "400000" }));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeclareReservePartitionsAsync_FailsWhenAnAllowanceIsMissingFromTheEcho()
    {
        var (gateway, _, _) = BuildGateway(
            partitionsJson: PartitionsJson("12345", ("contributor", "600000")));

        var result = await gateway.DeclareReservePartitionsAsync(new AzoaReservePartitionRequest(
            "Algorand",
            "12345",
            new Dictionary<string, string> { ["contributor"] = "600000", ["investor"] = "400000" }));

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1 000")]
    [InlineData("1.5")]
    [InlineData("")]
    public async Task DeclareReservePartitionsAsync_RefusesABudgetThatIsNotAWholeCount(string budget)
    {
        var (gateway, treasury, value) = BuildGateway(
            partitionsJson: PartitionsJson("12345", ("contributor", "600000")));

        var result = await gateway.DeclareReservePartitionsAsync(new AzoaReservePartitionRequest(
            "Algorand", "12345", new Dictionary<string, string> { ["contributor"] = budget }));

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        treasury.Sent.Should().BeEmpty();
        value.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task DeclareReservePartitionsAsync_RefusesAStakeItCannotAddress()
    {
        var (gateway, _, value) = BuildGateway(
            partitionsJson: PartitionsJson("12345", ("contributor", "600000")));

        var result = await gateway.DeclareReservePartitionsAsync(new AzoaReservePartitionRequest(
            "Algorand",
            "../tenant/custodial-accounts",
            new Dictionary<string, string> { ["contributor"] = "600000" }));

        result.IsSuccess.Should().BeFalse();
        value.Sent.Should().BeEmpty();
    }

    // ========================================================================
    // Credential issuance
    // ========================================================================

    [Fact]
    public async Task IssueAsync_IssuesASoulboundCredentialToTheRecipientRoute_AndMapsItBack()
    {
        var issuanceId = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");
        var handler = RespondingWith(CredentialRowJson(
            issuanceId, Recipient, tokenId: "cred-token-1", soulbound: true, transferable: false));
        var client = NewIssuanceClient(handler);

        var result = await client.IssueAsync(Recipient, SampleIssueRequest(), "membership-credential-issue:cred-9");

        result.IsSuccess.Should().BeTrue();
        result.Value!.IssuanceReference.Should().Be(issuanceId.ToString("D"));
        result.Value.ProofReference.Should().Be("cred-token-1");
        result.Value.MetadataUri.Should().Be("ipfs://membership-9");
        result.Value.IssuedAt.Should().Be(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));

        var sent = handler.Sent.Should().ContainSingle().Subject;
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Uri.AbsolutePath.Should().Be($"/api/credential/issue/{Recipient:D}");
        sent.IdempotencyKey.Should().Be("membership-credential-issue:cred-9");

        using var body = JsonDocument.Parse(sent.Body);
        body.RootElement.GetProperty("isSoulbound").GetBoolean().Should().BeTrue();
        body.RootElement.GetProperty("isTransferable").GetBoolean().Should().BeFalse();
        body.RootElement.GetProperty("metadataURI").GetString().Should().Be("ipfs://membership-9");
        body.RootElement.GetProperty("chainType").GetString().Should().Be("Algorand");
        body.RootElement.GetProperty("attributes").GetProperty("credentialId").GetString()
            .Should().Be("cred-9");
        // The recipient is the route value and the issuer is the presented credential. Neither
        // may be a body field, or a body could redirect a credential or nominate its revoker.
        body.RootElement.TryGetProperty("recipientUserId", out _).Should().BeFalse();
        body.RootElement.TryGetProperty("issuerUserId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task IssueAsync_RefusesARowHeldBySomebodyElse()
    {
        // Its caller stamps CredentialProofOrigin.IssuanceNode on whatever comes back, which is
        // the single thing that makes a member's card read "independently verifiable".
        var handler = RespondingWith(CredentialRowJson(
            Guid.NewGuid(), Guid.NewGuid(), tokenId: "cred-token-1", soulbound: true, transferable: false));
        var client = NewIssuanceClient(handler);

        var result = await client.IssueAsync(Recipient, SampleIssueRequest(), "membership-credential-issue:cred-9");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
    }

    [Fact]
    public async Task IssueAsync_RefusesARowTheNodeLeftTransferable()
    {
        var handler = RespondingWith(CredentialRowJson(
            Guid.NewGuid(), Recipient, tokenId: "cred-token-1", soulbound: false, transferable: true));
        var client = NewIssuanceClient(handler);

        var result = await client.IssueAsync(Recipient, SampleIssueRequest(), "membership-credential-issue:cred-9");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
    }

    [Fact]
    public async Task IssueAsync_RefusesARowWithNoIssuanceIdentity()
    {
        var handler = RespondingWith(CredentialRowJson(
            Guid.Empty, Recipient, tokenId: "cred-token-1", soulbound: true, transferable: false));
        var client = NewIssuanceClient(handler);

        var result = await client.IssueAsync(Recipient, SampleIssueRequest(), "membership-credential-issue:cred-9");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData("key with spaces")]
    [InlineData("key/with/slashes")]
    public async Task IssueAsync_RefusesAnUnusableKey_WithoutSendingAnything(string key)
    {
        var handler = RespondingWith(CredentialRowJson(
            Guid.NewGuid(), Recipient, tokenId: "t", soulbound: true, transferable: false));
        var client = NewIssuanceClient(handler);

        var result = await client.IssueAsync(Recipient, SampleIssueRequest(), key);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        handler.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task IssueAsync_RefusesAMemberWithNoAccount_WithoutSendingAnything()
    {
        var handler = RespondingWith(CredentialRowJson(
            Guid.NewGuid(), Recipient, tokenId: "t", soulbound: true, transferable: false));
        var client = NewIssuanceClient(handler);

        var result = await client.IssueAsync(
            Guid.Empty, SampleIssueRequest(), "membership-credential-issue:cred-9");

        result.IsSuccess.Should().BeFalse();
        handler.Sent.Should().BeEmpty();
    }

    // ========================================================================
    // Cash out — the adapter OI-56 asked for, already shipped and bound
    // ========================================================================

    [Fact]
    public async Task CashOutRequestAsync_ClaimsAgainstTheSubjectRoute_AndSendsNoDestination()
    {
        // A caller-supplied destination would be a way to redirect somebody else's money into
        // your own bank, so the payer is the route value and the body carries no account at all.
        var handler = RespondingWith(CashOutRowJson);
        var client = new AzoaCashOutClient(HttpFor(handler), NullLogger<AzoaCashOutClient>.Instance);

        var result = await client.RequestAsync(Recipient, "payout-req-1", 2500);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountMinorUnits.Should().Be(2500);
        result.Value.CurrencyCode.Should().Be("USD");

        var sent = handler.Sent.Should().ContainSingle().Subject;
        sent.Uri.AbsolutePath.Should().Be($"/api/fiat/payouts/{Recipient:D}");

        using var body = JsonDocument.Parse(sent.Body);
        body.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(new[] { "requestKey", "amountMinorUnits" });
    }

    // ========================================================================
    // Fixtures
    // ========================================================================

    private static MembershipCredentialIssueRequest SampleIssueRequest() => new(
        CredentialId: "cred-9",
        Scope: "PROJECT",
        ScopeId: "proj-7",
        RecipientUserId: "member-3",
        Tier: "Core",
        GrantedVia: "task-completion",
        MetadataUri: "ipfs://membership-9");

    private static AzoaCredentialIssuanceClient NewIssuanceClient(RecordingHandler handler) =>
        new(HttpFor(handler),
            NullLogger<AzoaCredentialIssuanceClient>.Instance,
            Options.Create(new AzoaSettings { BaseUrl = NodeBase.ToString(), ChainType = "Algorand" }));

    /// <summary>
    /// A gateway over two recorded transports: the custody one it owns for the treasury leg, and
    /// a real <see cref="AzoaNodeClient"/> for the allowance leg — so the value credential's own
    /// route policy runs in these tests rather than being mocked away.
    /// </summary>
    private static (AzoaProjectTokenNodeGateway Gateway, RecordingHandler Treasury, RecordingHandler Value)
        BuildGateway(string? treasuryJson = null, string? partitionsJson = null)
    {
        var treasury = RespondingWith(treasuryJson ?? TreasuryJson("Algorand", "proj-7", "ADDR", "Open"));
        var value = RespondingWith(partitionsJson ?? PartitionsJson("12345"));

        var gateway = new AzoaProjectTokenNodeGateway(
            HttpFor(treasury),
            new AzoaNodeClient(HttpFor(value), NullLogger<AzoaNodeClient>.Instance),
            NullLogger<AzoaProjectTokenNodeGateway>.Instance);

        return (gateway, treasury, value);
    }

    private const string FungibleMintResultJson =
        """{"userId":"11111111-1111-1111-1111-111111111111","walletId":"22222222-2222-2222-2222-222222222222","walletAddress":"RESERVE","walletProvisioned":true,"assetId":"12345","replayed":false}""";

    private const string CashOutRowJson =
        """{"id":"co_1","status":"Pending","statusMessage":"This withdrawal is on its way.","amountMinorUnits":2500,"currencyCode":"usd","requestedAt":"2026-09-01T10:00:00Z"}""";

    private static string TreasuryJson(string chain, string reference, string address, string status) =>
        $$"""{"chainType":"{{chain}}","projectReference":"{{reference}}","treasuryAddress":"{{address}}","status":"{{status}}"}""";

    private static string PartitionsJson(string assetId, params (string Name, string Declared)[] partitions)
    {
        var rows = partitions.Select(partition =>
            $$"""{"name":"{{partition.Name}}","declared":"{{partition.Declared}}","issued":"0","held":"0","returned":"0","available":"{{partition.Declared}}"}""");
        var joinedRows = string.Join(",", rows);

        return $$"""{"assetId":"{{assetId}}","chain":"Algorand","network":"testnet","unitName":"PLOT","decimals":2,"totalSupply":"1000000","version":3,"partitions":[{{joinedRows}}]}""";
    }

    private static string CredentialRowJson(
        Guid id, Guid holder, string tokenId, bool soulbound, bool transferable)
    {
        var soulboundJson = soulbound ? "true" : "false";
        var transferableJson = transferable ? "true" : "false";

        return $$"""{"id":"{{id:D}}","userId":"{{holder:D}}","tokenId":"{{tokenId}}","metadataURI":"ipfs://membership-9","isSoulbound":{{soulboundJson}},"isTransferable":{{transferableJson}},"mintedDate":"2026-09-01T10:00:00Z"}""";
    }

    /// <summary>Wraps a payload in the node's <c>AZOAResult&lt;T&gt;</c> envelope.</summary>
    private static string Envelope(string resultJson) =>
        """{"isError":false,"message":"Success","result":""" + resultJson + "}";

    private static RecordingHandler RespondingWith(string resultJson) => new(Envelope(resultJson));

    private static HttpClient HttpFor(RecordingHandler handler) =>
        new(handler) { BaseAddress = NodeBase };

    /// <summary>Records everything that left the process, and answers with one canned envelope.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public RecordingHandler(string responseBody) => _responseBody = responseBody;

        public List<SentRequest> Sent { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Sent.Add(new SentRequest(
                request.Method,
                request.RequestUri!,
                body,
                request.Headers.TryGetValues("Idempotency-Key", out var keys)
                    ? keys.FirstOrDefault()
                    : null));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed record SentRequest(HttpMethod Method, Uri Uri, string Body, string? IdempotencyKey);
}

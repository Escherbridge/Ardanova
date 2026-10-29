namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.ValueObjects;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// H2's two client-side controls with real branching: the startup check that keeps a late NOT_FOUND
/// meaningful (W &gt; W_admit + skew), and the gateway never sending past a send's own deadline.
/// H3's boot verdict for a value key that changed under open settlements.
/// </summary>
public class ExchangeAdmissionTests
{
    // -- The startup check -----------------------------------------------------------------------

    [Fact]
    public void FromConfiguration_NothingConfigured_IsFourMinutesToAdmitFiveToBelieve_AndClaimsNoEnforcement()
    {
        var policy = AzoaExchangeAdmissionSettings.FromConfiguration(Configuration());

        policy.AdmitWindow.Should().Be(TimeSpan.FromMinutes(4));
        policy.QuietWindow.Should().Be(TimeSpan.FromMinutes(5));
        policy.NodeEnforcesAdmitBefore.Should().BeFalse("only an operator who saw the node deploy may claim it");
        policy.ProofWindow.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("240", "270", "30")] // W == W_admit + skew: a read at the window's end could precede an admission
    [InlineData("240", "200", "0")] // W < W_admit
    [InlineData("0", "300", "30")] // no admission window at all
    [InlineData("240", "300", "-1")] // negative skew
    [InlineData("four", "300", "30")] // unparseable
    public void FromConfiguration_ACombinationThatBreaksTheProof_FailsBoot(string admit, string quiet, string skew)
    {
        var configuration = Configuration(
            ("AdmitWindowSeconds", admit), ("QuietWindowSeconds", quiet), ("MaximumClockSkewSeconds", skew));

        var boot = () => new ServiceCollection().AddAzoaExchangeProof(configuration, valueApiKey: string.Empty);

        boot.Should().Throw<InvalidOperationException>().WithMessage("Azoa:Exchange:*");
    }

    [Fact]
    public void FromConfiguration_JustInsideTheProof_Boots()
    {
        var policy = AzoaExchangeAdmissionSettings.FromConfiguration(Configuration(
            ("AdmitWindowSeconds", "240"), ("QuietWindowSeconds", "271"), ("MaximumClockSkewSeconds", "30")));

        policy.QuietWindow.Should().Be(TimeSpan.FromSeconds(271));
    }

    [Theory]
    [InlineData("false", 300)] // a shorter W never weakens today's lease-bounded proof until the node enforces
    [InlineData("true", 120)]
    public void ProofWindow_DropsBelowTodaysOnlyOnceTheNodeEnforcesAdmitBefore(string enforces, int expectedSeconds)
    {
        var policy = AzoaExchangeAdmissionSettings.FromConfiguration(Configuration(
            ("AdmitWindowSeconds", "60"), ("QuietWindowSeconds", "120"), ("MaximumClockSkewSeconds", "30"),
            ("NodeEnforcesAdmitBefore", enforces)));

        policy.ProofWindow.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    // -- The gateway never sends past a send's own deadline ----------------------------------------

    [Fact]
    public async Task DispatchAsync_AfterItsAdmissionDeadline_IsNeverSent_AndAdmittedNothing()
    {
        var node = new CountingNode();
        var request = Request() with { AdmitBefore = DateTimeOffset.UtcNow.AddSeconds(-1) };

        var result = await Gateway(node).DispatchAsync(request);

        result.Outcome.Should().Be(AzoaSettlementGatewayOutcome.Retry);
        result.Code.Should().Be(EconomicSettlementFailureCodes.AdmissionDeadlinePassed);
        node.Calls.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_BeforeItsDeadline_SendsTheDeadlineAsAStrictUtcHeader()
    {
        var node = new CountingNode();
        var admitBefore = new DateTimeOffset(2099, 1, 2, 3, 4, 5, TimeSpan.Zero);

        await Gateway(node).DispatchAsync(Request() with { AdmitBefore = admitBefore });

        node.Calls.Should().Be(1);
        node.AdmitBefore.Should().Be("2099-01-02T03:04:05Z");
    }

    // -- H3: the value key under open settlements ---------------------------------------------------

    [Theory]
    [InlineData(null, 3, false)] // nothing pinned: the boot log still names the fingerprint, nothing to compare
    [InlineData("aaaaaaaaaaaa", 3, false)] // the pinned key is the configured key
    [InlineData("bbbbbbbbbbbb", 0, false)] // rotated with nothing open: allowed
    [InlineData("bbbbbbbbbbbb", 3, true)] // rotated while settlements are open: P6-03
    public void Mismatch_IsRaisedOnlyForARotatedKeyWhileSettlementsAreOpen(string? pinned, int open, bool raised)
    {
        var verdict = AzoaExchangeProofBootCheck.Mismatch("aaaaaaaaaaaa", pinned, open);

        (verdict is not null).Should().Be(raised);
    }

    [Fact]
    public void Fingerprint_IsShortStableAcrossWhitespaceAndDistinctPerKey()
    {
        var fingerprint = AzoaValueKeyFingerprint.Of("value-key-secret");

        fingerprint.Should().HaveLength(12).And.Be(AzoaValueKeyFingerprint.Of(" value-key-secret "));
        AzoaValueKeyFingerprint.Of("another-key").Should().NotBe(fingerprint);
    }

    // -- Harness -----------------------------------------------------------------------------------

    private static IConfiguration Configuration(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(
                $"{ExchangeAdmissionPolicy.SectionName}:{value.Key}", value.Value)))
            .Build();

    private static AzoaSettlementRequest Request()
    {
        FixedScaleAmount.TryFromPositiveDecimal(12.34m, 2, out var amount).Should().BeTrue();
        return new AzoaSettlementRequest(
            "settlement-admit", "avatar:funding-intent:intent-admit", "user-admit", "PROJ", amount, TermsSnapshot);
    }

    private static AzoaSettlementGateway Gateway(CountingNode node)
    {
        var readiness = new Mock<IAzoaValueEligibilityService>();
        readiness.Setup(service => service.RequireReadyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(new AzoaCustodialAccountStatusDto
            {
                AvatarId = Guid.NewGuid().ToString("D"),
                Ready = true,
            }));
        return new AzoaSettlementGateway(
            new AzoaExchangeClient(
                new HttpClient(node) { BaseAddress = new Uri("https://node.test") },
                NullLogger<AzoaExchangeClient>.Instance,
                Options.Create(new AzoaSettings())),
            readiness.Object,
            new AzoaSettlementGatewayCapability("node-admit", true, DateTimeOffset.UtcNow.AddDays(1)),
            TimeProvider.System,
            NullLogger<AzoaSettlementGateway>.Instance);
    }

    private const string TermsSnapshot = """
        {"settlementLeg":"projectUnit","chainType":"Simulated","isBuy":true,"projectReference":"project-admit",
         "projectAssetId":"asset-admit","reservePartition":"sponsor","platformAmountBaseUnits":"5000",
         "projectUnitAmountBaseUnits":"1234","purpose":"sponsorship",
         "declaredRate":{"projectTokenConfigId":"config-admit","ardaPerWholeProjectUnit":"4.05",
           "usdMajorUnitsPerWholeProjectUnit":"0.04","projectUnitDecimals":2,"source":"tenant-declared",
           "declaredAt":"2026-10-25T08:00:00+00:00","version":"1"}}
        """;

    /// <summary>The node's exchange edge: counts sends and keeps the last Admit-Before header.</summary>
    private sealed class CountingNode : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? AdmitBefore { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            AdmitBefore = request.Headers.TryGetValues(ExchangeAdmissionPolicy.AdmitBeforeHeader, out var values)
                ? values.Single()
                : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("""{"isError":true,"code":"DEPENDENCY_UNAVAILABLE","message":"x"}""", Encoding.UTF8, "application/json"),
            });
        }
    }
}

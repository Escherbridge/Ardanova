namespace ArdaNova.Application.Tests.Azoa;

using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;

/// <summary>
/// Pins the one word QA reads off /health, /ready and the startup banner, and the Production
/// refusal that stops a simulated stack from booting as if it were an economy. The trap this
/// closes: a deployment where Azoa:Mode=Simulated answers every funding, allocation and payout
/// call with a deterministic sim: identifier, so every screen reports success and no funds move.
/// </summary>
public class EconomyModeResolutionTests
{
    private const string SimulatedProvider = "Simulated";
    private const string AzoaProvider = "Azoa";
    private const string LegacyProvider = "Legacy";

    private static AzoaSettings Settings(
        string mode = "Simulated",
        bool fundingCheckout = false,
        bool settlementOutbox = false,
        bool custodialAccounts = false)
        => new()
        {
            Mode = mode,
            EnableFundingCheckout = fundingCheckout,
            EnableSettlementOutboxWorker = settlementOutbox,
            EnableCustodialAccounts = custodialAccounts,
        };

    // ========================================================================
    // Mode resolution
    // ========================================================================

    // ========================================================================
    // The add-funds rail: a live economy with no way into it
    // ========================================================================

    /// <summary>
    /// Azoa:OnRamp:Enabled is a DIFFERENT switch from the ones that decide the mode, so "live"
    /// alone never proved a person could put money in. An operator reading economy == live off
    /// /health would have concluded money works while IFiatRailGateway was still the refusal stub.
    /// </summary>
    [Fact]
    public void A_live_economy_with_the_on_ramp_shut_is_reported_as_having_no_way_in()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Live"),
            AzoaProvider,
            algorandIsNoChain: false,
            addFundsEnabled: false);

        report.Mode.Should().Be(EconomyMode.Live);
        report.AddFundsEnabled.Should().BeFalse();
        report.IsLiveWithNoWayIn.Should().BeTrue();
        EconomyModeResolution.DescribeBanner(report).Should().Contain("nobody can");
    }

    [Fact]
    public void A_live_economy_with_the_on_ramp_open_has_a_way_in()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Live"),
            AzoaProvider,
            algorandIsNoChain: false,
            addFundsEnabled: true);

        report.AddFundsEnabled.Should().BeTrue();
        report.IsLiveWithNoWayIn.Should().BeFalse();
        EconomyModeResolution.DescribeBanner(report).Should().NotContain("nobody can");
    }

    /// <summary>The warning is about a LIVE economy specifically; a simulated one is already loud.</summary>
    [Fact]
    public void A_simulated_economy_is_not_also_flagged_for_the_on_ramp()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Simulated", fundingCheckout: true),
            SimulatedProvider,
            algorandIsNoChain: true,
            addFundsEnabled: false);

        report.Mode.Should().Be(EconomyMode.Simulated);
        report.IsLiveWithNoWayIn.Should().BeFalse();
    }

    [Fact]
    public void Defaults_resolve_to_a_disabled_economy()
    {
        var report = EconomyModeResolution.Resolve(Settings(), SimulatedProvider, algorandIsNoChain: true);

        report.Mode.Should().Be(EconomyMode.Disabled);
        report.Economy.Should().Be("disabled");
        report.IsPartiallyReal.Should().BeFalse();
        report.Reason.Should().Contain("No value rail is switched on");
    }

    [Fact]
    public void Live_node_mode_with_a_chain_backed_provider_is_live()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Live"),
            AzoaProvider,
            algorandIsNoChain: false);

        report.Mode.Should().Be(EconomyMode.Live);
        report.Economy.Should().Be("live");
        report.NodeValueMovesAreReal.Should().BeTrue();
        report.ChainValueMovesAreReal.Should().BeTrue();
        report.IsPartiallyReal.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void A_switched_on_feature_over_simulated_defaults_is_simulated_not_disabled(
        bool fundingCheckout,
        bool settlementOutbox,
        bool custodialAccounts)
    {
        var report = EconomyModeResolution.Resolve(
            Settings(
                fundingCheckout: fundingCheckout,
                settlementOutbox: settlementOutbox,
                custodialAccounts: custodialAccounts),
            SimulatedProvider,
            algorandIsNoChain: true);

        report.Mode.Should().Be(EconomyMode.Simulated);
        report.Economy.Should().Be("simulated");
        report.IsPartiallyReal.Should().BeFalse();
        report.Reason.Should().Contain("without").And.Contain("moving funds");
    }

    [Fact]
    public void A_live_node_behind_a_no_chain_provider_is_simulated_and_flagged_partially_real()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Live"),
            SimulatedProvider,
            algorandIsNoChain: true);

        report.Mode.Should().Be(EconomyMode.Simulated);
        report.NodeValueMovesAreReal.Should().BeTrue();
        report.ChainValueMovesAreReal.Should().BeFalse();
        report.IsPartiallyReal.Should().BeTrue();
        report.Reason.Should().Contain("Partial economy");
    }

    [Fact]
    public void A_simulated_node_behind_a_chain_backed_provider_is_simulated_and_flagged_partially_real()
    {
        var report = EconomyModeResolution.Resolve(Settings(), LegacyProvider, algorandIsNoChain: false);

        report.Mode.Should().Be(EconomyMode.Simulated);
        report.NodeValueMovesAreReal.Should().BeFalse();
        report.ChainValueMovesAreReal.Should().BeTrue();
        report.IsPartiallyReal.Should().BeTrue();
        report.Reason.Should().Contain("no sponsorship, stake or payout moves funds");
    }

    [Fact]
    public void Mode_matching_ignores_case_and_surrounding_whitespace()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "  live  "),
            AzoaProvider,
            algorandIsNoChain: false);

        report.Mode.Should().Be(EconomyMode.Live);
        report.AzoaMode.Should().Be("live");
    }

    [Fact]
    public void A_blank_provider_name_is_reported_as_the_simulated_default()
    {
        var report = EconomyModeResolution.Resolve(Settings(), "   ", algorandIsNoChain: true);

        report.AlgorandProvider.Should().Be("Simulated");
        report.Mode.Should().Be(EconomyMode.Disabled);
    }

    // ========================================================================
    // Production refusal
    // ========================================================================

    [Fact]
    public void Production_refuses_a_simulated_economy()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(fundingCheckout: true),
            SimulatedProvider,
            algorandIsNoChain: true);

        var refusal = () => EconomyModeResolution.ThrowIfNotLiveInProduction(report, isProduction: true);

        refusal.Should().Throw<InvalidOperationException>()
            .WithMessage("*refuses a 'simulated' economy*")
            .WithMessage("*Azoa:Mode=Live*");
    }

    [Fact]
    public void Production_refuses_a_disabled_economy()
    {
        var report = EconomyModeResolution.Resolve(Settings(), SimulatedProvider, algorandIsNoChain: true);

        var refusal = () => EconomyModeResolution.ThrowIfNotLiveInProduction(report, isProduction: true);

        refusal.Should().Throw<InvalidOperationException>()
            .WithMessage("*refuses a 'disabled' economy*");
    }

    [Fact]
    public void Production_refuses_a_live_node_that_sits_behind_a_simulated_provider()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Live"),
            SimulatedProvider,
            algorandIsNoChain: true);

        var refusal = () => EconomyModeResolution.ThrowIfNotLiveInProduction(report, isProduction: true);

        refusal.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Production_accepts_a_live_economy()
    {
        var report = EconomyModeResolution.Resolve(
            Settings(mode: "Live", fundingCheckout: true),
            AzoaProvider,
            algorandIsNoChain: false);

        var refusal = () => EconomyModeResolution.ThrowIfNotLiveInProduction(report, isProduction: true);

        refusal.Should().NotThrow();
    }

    [Fact]
    public void Non_production_environments_keep_their_simulated_defaults()
    {
        var report = EconomyModeResolution.Resolve(Settings(), SimulatedProvider, algorandIsNoChain: true);

        var refusal = () => EconomyModeResolution.ThrowIfNotLiveInProduction(report, isProduction: false);

        refusal.Should().NotThrow();
    }
}

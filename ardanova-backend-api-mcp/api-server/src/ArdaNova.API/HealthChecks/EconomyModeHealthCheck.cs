namespace ArdaNova.API.HealthChecks;

using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;

/// <summary>
/// The API host's view of the economy mode: the /health and /ready projection, and the startup
/// banner's log level. The mode itself, its reason and the banner text come from
/// <see cref="EconomyModeResolution"/> in ArdaNova.Infrastructure, so the MCP host -- which boots
/// the same AZOA settings through AddInfrastructure -- reports the same words from the same code.
/// </summary>
public static class EconomyModeHealthCheck
{
    /// <summary>
    /// Resolves the mode from the container's registered capabilities. This is authoritative:
    /// <see cref="EconomyModeResolution.ResolveFromConfiguration"/> maps provider names to chain
    /// reality on its own, and Program.cs re-runs the Production refusal against this result so
    /// a drift between the two fails closed at startup instead of shipping a wrong banner.
    /// </summary>
    public static EconomyModeReport Resolve(
        IConfiguration configuration,
        AlgorandProviderCapabilities algorandCapabilities)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(algorandCapabilities);

        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        return EconomyModeResolution.Resolve(
            settings,
            algorandCapabilities.Provider,
            algorandCapabilities.IsNoChain,
            configuration.GetValue<bool>($"{ArdaOnRampOptions.SectionName}:Enabled"));
    }

    /// <summary>The /ready projection: the mode plus the per-leg facts that justify it.</summary>
    public static object Describe(EconomyModeReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new
        {
            mode = report.Economy,
            azoaMode = report.AzoaMode,
            algorandProvider = report.AlgorandProvider,
            nodeValueMovesAreReal = report.NodeValueMovesAreReal,
            chainValueMovesAreReal = report.ChainValueMovesAreReal,
            fundingCheckoutEnabled = report.FundingCheckoutEnabled,
            settlementOutboxEnabled = report.SettlementOutboxEnabled,
            custodialAccountsEnabled = report.CustodialAccountsEnabled,
            addFundsEnabled = report.AddFundsEnabled,
            partiallyReal = report.IsPartiallyReal,
            liveWithNoWayIn = report.IsLiveWithNoWayIn,
            reason = report.Reason,
        };
    }

    /// <summary>
    /// Writes the startup banner. A non-live economy is never an Information line, so a simulated
    /// stack cannot scroll past unnoticed in a deploy log.
    /// </summary>
    public static void LogStartupBanner(ILogger logger, EconomyModeReport report, bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(report);

        var banner = EconomyModeResolution.DescribeBanner(report);

        if (report.Mode == EconomyMode.Live)
            logger.LogInformation("{EconomyBanner}", banner);
        else if (isProduction)
            logger.LogCritical("{EconomyBanner}", banner);
        else
            logger.LogWarning("{EconomyBanner}", banner);
    }
}

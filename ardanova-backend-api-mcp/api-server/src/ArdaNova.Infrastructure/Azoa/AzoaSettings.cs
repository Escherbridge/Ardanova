namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Infrastructure.Algorand;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Configuration for the shared/managed AZOA blockchain-node integration.
/// Bound from the "Azoa" config section and/or environment variables
/// (for example <c>Azoa__CustodyApiKey</c>, <c>Azoa__ValueApiKey</c>, and
/// <c>Azoa__QuestApiKey</c>).
///
/// ArdaNova integrates a tenant-bound AZOA node. The node operator owns the
/// custody and provider configuration; ArdaNova receives only safe references.
/// </summary>
public class AzoaSettings
{
    public const string SectionName = "Azoa";

    /// <summary>
    /// Base URL of the AZOA node (e.g. https://azoa.example.com). No trailing slash required.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Custody-only key for tenant onboarding, managed wallets, and KYC.
    /// </summary>
    public string CustodyApiKey { get; set; } = string.Empty;

    /// <summary>Value-operation key with only the explicitly provisioned value scopes.</summary>
    public string ValueApiKey { get; set; } = string.Empty;

    /// <summary>Quest-only key; authoring requires dapp:develop and a developer-role owner.</summary>
    public string QuestApiKey { get; set; } = string.Empty;

    /// <summary>Legacy non-Production migration key; ignored in Production.</summary>
    public string TenantApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Provider/blockchain mode mirrored from the node: "Live" or "Simulated".
    /// Simulated yields deterministic <c>sim:</c> ids with no chain I/O — used for
    /// dev, CI, and the cross-process e2e harness.
    /// </summary>
    public string Mode { get; set; } = "Simulated";

    /// <summary>
    /// Default chain type passed to the node for value moves (e.g. "Algorand").
    /// </summary>
    public string ChainType { get; set; } = "Algorand";

    /// <summary>
    /// Per-request timeout (seconds) for calls to the AZOA node.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Explicit operator opt-in for payment collection. This switch alone never opens checkout:
    /// it is one of four independent conditions, three of which are read outside this POCO.
    /// Checkout is open only when all four hold:
    /// (1) <c>Azoa:EnableFundingCheckout=true</c> -- this flag;
    /// (2) <see cref="SelectedSettlementNodeId"/> names the node that will settle the collected
    ///     funds, which is what selects <c>AzoaSettlementGateway</c> over
    ///     <c>DisabledAzoaSettlementGateway</c> in <c>AddInfrastructure</c>;
    /// (3) <c>Azoa:SettlementAttestationExpiresAt</c> is a future instant -- the operator's review
    ///     of that node's dispatch and reconciliation capability, which EXPIRES so a one-off
    ///     sign-off cannot hold checkout open forever;
    /// (4) <c>IFundingSettlementReadiness.IsReady</c> agrees at request time.
    /// Setting this flag without (2)-(4) collects no money: <c>/ready</c> reports not_ready, which
    /// is the intended fail-closed outcome rather than a silent half-open checkout.
    /// </summary>
    public bool EnableFundingCheckout { get; set; }

    /// <summary>
    /// Immutable identifier of the node authorized to settle collected funding.
    /// Configuration alone is insufficient; it must match the gateway capability.
    /// </summary>
    public string SelectedSettlementNodeId { get; set; } = string.Empty;

    /// <summary>Explicit operator opt-in for the hosted settlement outbox loop.</summary>
    public bool EnableSettlementOutboxWorker { get; set; }

    /// <summary>Maximum dispatch and reconciliation attempts per hosted cycle.</summary>
    public int SettlementOutboxBatchSize { get; set; } = 10;

    /// <summary>Delay between bounded hosted cycles.</summary>
    public int SettlementOutboxIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Explicitly enables tenant-bound per-user custody and KYC orchestration. Flipping this
    /// alone does not make identity/KYC/wallet provisioning work: the AZOA node's own three
    /// legs -- custody (<c>CustodialAccounts__CustodyMode=Platform</c> plus a boot-validated
    /// <c>AZOA__WalletEncryptionKey</c>), a live chain (<c>Blockchain__Mode</c> not
    /// <c>Simulated</c>), and KYC (<c>Kyc__Identomat__CompanyKey</c>/<c>ConfigId</c>/
    /// <c>CallbackApiKey</c>) -- must each be live on the AZOA Railway service first. Any leg
    /// still missing surfaces by name, never silently, in the capabilities probe this app reads
    /// at <c>GET /api/tenant/custodial-accounts/capabilities</c> and republishes verbatim as
    /// this service's own <c>/ready</c> response under
    /// <c>checks.azoa.custodialAccountsUnavailableReason</c> (see Program.cs). Setting this
    /// flag before every AZOA leg is live does not corrupt anything, but it does turn this
    /// app's own <c>/ready</c> not_ready -- see <see cref="TenantId"/> for why it should wait.
    /// </summary>
    public bool EnableCustodialAccounts { get; set; }

    /// <summary>
    /// GUID minted by AZOA when the ArdaNova tenant is provisioned (see the AZOA repo's
    /// docs/NODE-HOST.md, section 8.6). Required, and validated as a GUID, whenever
    /// <see cref="EnableCustodialAccounts"/> is true -- so set this and
    /// <see cref="CustodyApiKey"/> together, from the one onboarding call, never
    /// <see cref="EnableCustodialAccounts"/> alone: an enabled flag with no matching tenant
    /// fails the GUID check above and takes this app's own <c>/ready</c> to not_ready, not just
    /// the custodial-accounts sub-check.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    public bool IsSimulated =>
        Mode.Equals("Simulated", StringComparison.OrdinalIgnoreCase);
}

public static class AzoaCredentialSelection
{
    public static string ResolveCustodyKey(AzoaSettings settings, bool isProduction)
        => Resolve(settings.CustodyApiKey, settings.TenantApiKey, isProduction);

    public static string ResolveValueKey(AzoaSettings settings, bool isProduction)
        => Resolve(settings.ValueApiKey, settings.TenantApiKey, isProduction);

    public static string ResolveQuestKey(AzoaSettings settings, bool isProduction)
        => Resolve(settings.QuestApiKey, settings.TenantApiKey, isProduction);

    public static bool AreCredentialsSeparated(
        string custodyApiKey,
        string valueApiKey,
        string questApiKey,
        bool isProduction)
    {
        if (!isProduction)
            return true;

        var configured = new[] { custodyApiKey, valueApiKey, questApiKey }
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToArray();
        return configured.Distinct(StringComparer.Ordinal).Count() == configured.Length;
    }

    private static string Resolve(string explicitKey, string legacyKey, bool isProduction)
        => !string.IsNullOrWhiteSpace(explicitKey)
            ? explicitKey
            : isProduction
                ? string.Empty
                : legacyKey;
}

/// <summary>
/// What the running configuration actually does with value. Derived from configuration and the
/// container's provider choice, never configured directly -- see <see cref="EconomyModeResolution"/>.
/// </summary>
public enum EconomyMode
{
    /// <summary>No value rail is switched on; nothing a user does reaches funds.</summary>
    Disabled,

    /// <summary>A value rail is switched on but at least one leg is a deterministic fake.</summary>
    Simulated,

    /// <summary>Both the AZOA node and the Algorand boundary move real value.</summary>
    Live,
}

/// <summary>The resolved economy mode plus the operator-facing evidence behind it.</summary>
public sealed record EconomyModeReport(
    EconomyMode Mode,
    string AzoaMode,
    string AlgorandProvider,
    bool NodeValueMovesAreReal,
    bool ChainValueMovesAreReal,
    bool FundingCheckoutEnabled,
    bool SettlementOutboxEnabled,
    bool CustodialAccountsEnabled,
    string Reason,
    bool AddFundsEnabled = false)
{
    /// <summary>Wire word published by /health and /ready: "live", "simulated" or "disabled".</summary>
    public string Economy => Mode switch
    {
        EconomyMode.Live => "live",
        EconomyMode.Simulated => "simulated",
        _ => "disabled",
    };

    /// <summary>True when the mode is not live yet one leg still moves real value.</summary>
    public bool IsPartiallyReal =>
        Mode != EconomyMode.Live && (NodeValueMovesAreReal || ChainValueMovesAreReal);

    /// <summary>
    /// True when the economy is live but nobody can put money INTO it. The add-funds rail is a
    /// separate switch (<c>Azoa:OnRamp:Enabled</c>) from the ones that decide the mode, so a
    /// deployment can report "live" while <c>IFiatRailGateway</c> is still the fail-closed stub.
    /// Reporting the mode without this would tell an operator money works when no sponsor can
    /// hold a single unit to spend.
    /// </summary>
    public bool IsLiveWithNoWayIn => Mode == EconomyMode.Live && !AddFundsEnabled;
}

/// <summary>
/// Derives the economy mode from <c>Azoa:Mode</c>, <c>Algorand:Provider</c> and the AZOA feature
/// switches, and refuses a non-live rail in Production. There is deliberately no override flag:
/// an escape hatch here would be indistinguishable from the default it exists to refuse.
/// </summary>
public static class EconomyModeResolution
{
    public const string LiveModeName = "Live";
    public const string SimulatedModeName = "Simulated";
    public const string SimulatedProviderName = "Simulated";

    private const string BannerRule =
        "==============================================================================";

    /// <summary>
    /// Resolves the mode. <paramref name="algorandIsNoChain"/> comes from the registered
    /// <c>AlgorandProviderCapabilities</c> so the report cannot drift from the container's choice.
    /// </summary>
    public static EconomyModeReport Resolve(
        AzoaSettings settings,
        string? algorandProvider,
        bool algorandIsNoChain,
        bool addFundsEnabled = false)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var providerName = string.IsNullOrWhiteSpace(algorandProvider)
            ? SimulatedProviderName
            : algorandProvider.Trim();
        var azoaModeName = string.IsNullOrWhiteSpace(settings.Mode)
            ? SimulatedModeName
            : settings.Mode.Trim();
        var nodeValueMovesAreReal = azoaModeName.Equals(LiveModeName, StringComparison.OrdinalIgnoreCase);
        var chainValueMovesAreReal = !algorandIsNoChain;

        var anyRailSwitchedOn = settings.EnableFundingCheckout
            || settings.EnableSettlementOutboxWorker
            || settings.EnableCustodialAccounts
            || nodeValueMovesAreReal
            || chainValueMovesAreReal;

        var mode = !anyRailSwitchedOn
            ? EconomyMode.Disabled
            : nodeValueMovesAreReal && chainValueMovesAreReal
                ? EconomyMode.Live
                : EconomyMode.Simulated;

        return new EconomyModeReport(
            mode,
            azoaModeName,
            providerName,
            nodeValueMovesAreReal,
            chainValueMovesAreReal,
            settings.EnableFundingCheckout,
            settings.EnableSettlementOutboxWorker,
            settings.EnableCustodialAccounts,
            DescribeReason(mode, azoaModeName, providerName, nodeValueMovesAreReal, chainValueMovesAreReal),
            addFundsEnabled);
    }

    /// <summary>
    /// Resolves the mode straight from configuration, before any container exists. Both hosts that
    /// call <c>AddInfrastructure</c> boot the same AZOA settings, so both gate on this same call.
    /// </summary>
    public static EconomyModeReport ResolveFromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = AlgorandProviderSelection.Parse(
            configuration[$"{AlgorandSettings.SectionName}:Provider"]);
        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        // The add-funds switch lives in its own section and is bound here rather than inferred,
        // so both hosts and /health read the same key the DI registration branches on.
        var addFundsEnabled = configuration.GetValue<bool>($"{ArdaOnRampOptions.SectionName}:Enabled");
        return Resolve(settings, provider.ToString(), IsNoChain(provider), addFundsEnabled);
    }

    /// <summary>
    /// The operator-facing startup banner. The text lives here so every host prints the same
    /// words; the host only chooses the log level.
    /// </summary>
    public static string DescribeBanner(EconomyModeReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var lines = new List<string>
        {
            BannerRule,
            $"  ArdaNova economy mode: {report.Economy.ToUpperInvariant()}",
            $"    Azoa:Mode          = {report.AzoaMode} "
                + $"({(report.NodeValueMovesAreReal ? "node value moves are real" : "node returns deterministic sim: identifiers")})",
            $"    Algorand:Provider  = {report.AlgorandProvider} "
                + $"({(report.ChainValueMovesAreReal ? "chain-backed" : "no chain I/O")})",
            $"    Funding checkout   = {FormatSwitchState(report.FundingCheckoutEnabled)}",
            $"    Settlement outbox  = {FormatSwitchState(report.SettlementOutboxEnabled)}",
            $"    Custodial accounts = {FormatSwitchState(report.CustodialAccountsEnabled)}",
            $"    Add funds (on-ramp)= {FormatSwitchState(report.AddFundsEnabled)}",
            $"    {report.Reason}",
        };

        if (report.Mode != EconomyMode.Live)
        {
            lines.Add("  WARNING: this deployment does not implement a working economy. Do not use it");
            lines.Add("  to validate sponsorship, stake, settlement or payout behaviour.");
        }

        if (report.IsPartiallyReal)
            lines.Add("  WARNING: one leg is real regardless -- operations on it move real funds.");

        if (report.IsLiveWithNoWayIn)
        {
            lines.Add("  WARNING: the economy is live but Azoa:OnRamp:Enabled is off, so nobody can");
            lines.Add("  add funds. Sponsorship can only spend balances that already exist.");
        }

        lines.Add(BannerRule);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Refuses a simulated or disabled economy in Production, naming the offending settings.</summary>
    public static void ThrowIfNotLiveInProduction(EconomyModeReport report, bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!isProduction || report.Mode == EconomyMode.Live)
            return;

        throw new InvalidOperationException(
            $"Production refuses a '{report.Economy}' economy. Azoa:Mode='{report.AzoaMode}' and "
            + $"Algorand:Provider='{report.AlgorandProvider}' must both be real value rails "
            + "(Azoa:Mode=Live, and Algorand:Provider=Azoa or Legacy). "
            + report.Reason);
    }

    /// <summary>Mirrors AddInfrastructure's AlgorandProviderCapabilities registration.</summary>
    private static bool IsNoChain(AlgorandProvider provider) => provider switch
    {
        AlgorandProvider.Simulated => true,
        AlgorandProvider.Legacy => false,
        AlgorandProvider.Azoa => false,
        _ => throw new InvalidOperationException("Unsupported Algorand provider selection."),
    };

    private static string FormatSwitchState(bool enabled) => enabled ? "on" : "off";

    private static string DescribeReason(
        EconomyMode mode,
        string azoaMode,
        string algorandProvider,
        bool nodeValueMovesAreReal,
        bool chainValueMovesAreReal)
        => mode switch
        {
            EconomyMode.Live =>
                $"Azoa:Mode={azoaMode} settles on the AZOA node and Algorand:Provider={algorandProvider} "
                + "reaches a chain: value operations move real funds.",
            EconomyMode.Disabled =>
                $"No value rail is switched on: Azoa:Mode={azoaMode} returns deterministic sim: identifiers, "
                + $"Algorand:Provider={algorandProvider} performs no chain I/O, and funding checkout, the "
                + "settlement outbox and custodial accounts are all off. Any value surface that still answers "
                + "returns a placeholder.",
            _ when nodeValueMovesAreReal =>
                $"Azoa:Mode={azoaMode}, so AZOA node operations (funding settlement, allocation, fungible mint) "
                + $"are real, but Algorand:Provider={algorandProvider} performs no chain I/O, so ArdaNova's own "
                + "Algorand port (credentials, direct ASA operations) is a deterministic fake. Partial economy.",
            _ when chainValueMovesAreReal =>
                $"Azoa:Mode={azoaMode} returns deterministic sim: identifiers for every AZOA node value operation "
                + "(funding settlement, allocation, fungible mint), so no sponsorship, stake or payout moves funds; "
                + $"only Algorand:Provider={algorandProvider} reaches a chain. Partial economy.",
            _ =>
                $"Azoa:Mode={azoaMode} returns deterministic sim: identifiers and Algorand:Provider={algorandProvider} "
                + "performs no chain I/O, yet a value feature is switched on: the economy surfaces answer without "
                + "moving funds.",
        };
}

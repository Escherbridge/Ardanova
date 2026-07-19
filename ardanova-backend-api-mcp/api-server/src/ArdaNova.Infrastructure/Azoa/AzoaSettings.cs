namespace ArdaNova.Infrastructure.Azoa;

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
    /// Explicit operator opt-in for payment collection. This remains false until
    /// a real selected-node gateway also exposes current dispatch and reconciliation attestation.
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

    /// <summary>Explicitly enables tenant-bound per-user custody and KYC orchestration.</summary>
    public bool EnableCustodialAccounts { get; set; }

    /// <summary>Stable tenant binding used for per-user custodial AZOA accounts.</summary>
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

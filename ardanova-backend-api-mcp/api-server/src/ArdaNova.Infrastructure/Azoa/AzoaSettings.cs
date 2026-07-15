namespace ArdaNova.Infrastructure.Azoa;

/// <summary>
/// Configuration for the shared/managed AZOA blockchain-node integration.
/// Bound from the "Azoa" config section and/or environment variables
/// (e.g. <c>Azoa__TenantApiKey</c> from the secret store — never committed).
///
/// Per the locked integration contract (§11): ArdaNova integrates a
/// shared/managed AZOA node (the node operator custodies keys), with
/// self-register + self-run avatars (no tenant:provision, no acting-as path).
/// </summary>
public class AzoaSettings
{
    public const string SectionName = "Azoa";

    /// <summary>
    /// Base URL of the AZOA node (e.g. https://azoa.example.com). No trailing slash required.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The tenant API key sent as the <c>X-Api-Key</c> header. Deploy-time secret,
    /// read from the secret store as <c>Azoa__TenantApiKey</c>. NEVER committed.
    /// Must carry the <c>nft:mint</c> / <c>wallet:manage</c> scopes (NOT
    /// <c>tenant:provision</c>, per the self-run model §11.2).
    /// </summary>
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

    public bool IsSimulated =>
        Mode.Equals("Simulated", StringComparison.OrdinalIgnoreCase);
}

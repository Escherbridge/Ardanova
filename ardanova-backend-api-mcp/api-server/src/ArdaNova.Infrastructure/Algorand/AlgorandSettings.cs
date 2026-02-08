namespace ArdaNova.Infrastructure.Algorand;

/// <summary>
/// Configuration settings for the Algorand blockchain integration.
/// Bound from appsettings.json "Algorand" section and/or environment variables.
/// </summary>
public class AlgorandSettings
{
    public const string SectionName = "Algorand";

    /// <summary>
    /// Algorand network: "testnet", "mainnet", or "betanet"
    /// </summary>
    public string Network { get; set; } = "testnet";

    /// <summary>
    /// Algod node API URL
    /// </summary>
    public string NodeUrl { get; set; } = "https://testnet-api.algonode.cloud";

    /// <summary>
    /// Algorand Indexer API URL
    /// </summary>
    public string IndexerUrl { get; set; } = "https://testnet-idx.algonode.cloud";

    /// <summary>
    /// 25-word mnemonic for the platform (custodial) account.
    /// This account signs all ASA creation, freeze, and clawback transactions.
    /// </summary>
    public string PlatformMnemonic { get; set; } = string.Empty;

    /// <summary>
    /// Platform account Algorand address (derived from mnemonic).
    /// If not set explicitly, it will be derived from PlatformMnemonic at startup.
    /// </summary>
    public string PlatformAddress { get; set; } = string.Empty;

    /// <summary>
    /// Optional Algod API token (not required for AlgoNode public endpoints)
    /// </summary>
    public string AlgodToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional Indexer API token (not required for AlgoNode public endpoints)
    /// </summary>
    public string IndexerToken { get; set; } = string.Empty;
}

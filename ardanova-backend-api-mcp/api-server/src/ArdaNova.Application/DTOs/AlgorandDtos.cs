namespace ArdaNova.Application.DTOs;

/// <summary>
/// Result of minting a soulbound ASA on Algorand
/// </summary>
public record SoulboundAsaMintResult
{
    /// <summary>
    /// The Algorand Standard Asset ID created on-chain
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// The transaction hash of the ASA creation transaction
    /// </summary>
    public required string TxHash { get; init; }
}

/// <summary>
/// Information about an Algorand Standard Asset
/// </summary>
public record AsaInfoDto
{
    /// <summary>
    /// The ASA ID on-chain
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// The display name of the asset
    /// </summary>
    public string? AssetName { get; init; }

    /// <summary>
    /// Short unit name (e.g., "CRED")
    /// </summary>
    public string? UnitName { get; init; }

    /// <summary>
    /// Total supply of the asset
    /// </summary>
    public ulong Total { get; init; }

    /// <summary>
    /// Number of decimal places
    /// </summary>
    public int Decimals { get; init; }

    /// <summary>
    /// Whether the asset is frozen by default (true for soulbound)
    /// </summary>
    public bool DefaultFrozen { get; init; }

    /// <summary>
    /// The URL pointing to ARC-19 metadata
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// The manager address
    /// </summary>
    public string? ManagerAddress { get; init; }

    /// <summary>
    /// The freeze address
    /// </summary>
    public string? FreezeAddress { get; init; }

    /// <summary>
    /// The clawback address
    /// </summary>
    public string? ClawbackAddress { get; init; }

    /// <summary>
    /// The reserve address
    /// </summary>
    public string? ReserveAddress { get; init; }

    /// <summary>
    /// The creator address
    /// </summary>
    public string? CreatorAddress { get; init; }

    /// <summary>
    /// Whether the ASA has been deleted/destroyed
    /// </summary>
    public bool IsDeleted { get; init; }
}

/// <summary>
/// Result of creating a fungible ASA on Algorand (Track 09 — Tokenomics)
/// </summary>
public record FungibleAsaCreateResult
{
    /// <summary>
    /// The Algorand Standard Asset ID created on-chain
    /// </summary>
    public required string AssetId { get; init; }

    /// <summary>
    /// The transaction hash of the ASA creation transaction
    /// </summary>
    public required string TxHash { get; init; }
}

/// <summary>
/// Metadata input for building ARC-19 JSON for a credential
/// </summary>
public record CredentialMetadataInput
{
    /// <summary>
    /// The credential database ID
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// "PROJECT" or "GUILD"
    /// </summary>
    public required string Scope { get; init; }

    /// <summary>
    /// The project or guild ID
    /// </summary>
    public required string ScopeId { get; init; }

    /// <summary>
    /// The project or guild name
    /// </summary>
    public required string ScopeName { get; init; }

    /// <summary>
    /// The user ID who owns this credential
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The user's tier (BRONZE, SILVER, GOLD, PLATINUM, DIAMOND, or null)
    /// </summary>
    public string? Tier { get; init; }

    /// <summary>
    /// How the credential was granted (FOUNDER, DAO_VOTE, etc.)
    /// </summary>
    public required string GrantedVia { get; init; }

    /// <summary>
    /// When the credential was granted
    /// </summary>
    public required DateTime GrantedAt { get; init; }
}

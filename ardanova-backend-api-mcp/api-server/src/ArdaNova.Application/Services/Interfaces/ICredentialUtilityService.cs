namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Orchestrates credential lifecycle operations, combining off-chain credential management
/// (IMembershipCredentialService) with on-chain Algorand operations (IAlgorandService).
/// </summary>
public interface ICredentialUtilityService
{
    /// <summary>
    /// Grants a credential off-chain, then mints a soulbound ASA on Algorand.
    /// If minting fails (chain down), the credential is still returned as valid off-chain.
    /// </summary>
    Task<Result<MembershipCredentialDto>> GrantAndMintAsync(
        GrantMembershipCredentialDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Burns the on-chain ASA (if it exists) and revokes the credential off-chain.
    /// </summary>
    Task<Result<MembershipCredentialDto>> RevokeAndBurnAsync(
        string id,
        CancellationToken ct = default);

    /// <summary>
    /// Upgrades the credential tier with validation that tiers can only go up.
    /// Tier ordering: BRONZE(0) &lt; SILVER(1) &lt; GOLD(2) &lt; PLATINUM(3) &lt; DIAMOND(4).
    /// </summary>
    Task<Result<MembershipCredentialDto>> UpgradeTierAsync(
        string id,
        string newTier,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a user is eligible for a credential and auto-grants + mints if so.
    /// Returns null if the user already has a credential or is not eligible.
    /// </summary>
    Task<Result<MembershipCredentialDto?>> CheckAndAutoGrantAsync(
        string userId,
        string? projectId,
        string? guildId,
        CancellationToken ct = default);

    /// <summary>
    /// Retries minting for a credential that was granted off-chain but failed to mint on-chain.
    /// Returns ValidationError if credential already has an assetId or is not ACTIVE.
    /// </summary>
    Task<Result<MembershipCredentialDto>> RetryMintAsync(
        string id,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a credential enriched with on-chain Algorand data (ASA info, ownership verification).
    /// </summary>
    Task<Result<CredentialWithChainDataDto>> GetCredentialWithChainDataAsync(
        string id,
        CancellationToken ct = default);
}

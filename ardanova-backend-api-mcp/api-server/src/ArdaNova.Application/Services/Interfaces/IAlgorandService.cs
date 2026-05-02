namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Abstraction for Algorand blockchain operations.
/// Implementation lives in ArdaNova.Infrastructure (AlgorandService).
/// Handles soulbound ASA creation, burning, queries, and ARC-19 metadata.
/// </summary>
public interface IAlgorandService
{
    /// <summary>
    /// Mints a soulbound (non-transferable) Algorand Standard Asset for a credential.
    /// The ASA is created with defaultFrozen=true, total=1, decimals=0.
    /// The platform account signs the transaction (custodial model).
    /// </summary>
    /// <param name="recipientAddress">The Algorand address of the credential recipient</param>
    /// <param name="metadata">Metadata input for building the ASA parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the asset ID and transaction hash</returns>
    Task<Result<SoulboundAsaMintResult>> MintSoulboundASAAsync(
        string recipientAddress,
        CredentialMetadataInput metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Burns (clawbacks and destroys) a soulbound ASA.
    /// Uses the platform account's clawback authority to reclaim the asset,
    /// then destroys it using the manager authority.
    /// </summary>
    /// <param name="assetId">The Algorand ASA ID to burn</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the burn transaction hash</returns>
    Task<Result<string>> BurnASAAsync(string assetId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves information about an Algorand Standard Asset from the indexer.
    /// </summary>
    /// <param name="assetId">The Algorand ASA ID to query</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the ASA info DTO</returns>
    Task<Result<AsaInfoDto>> GetASAInfoAsync(string assetId, CancellationToken ct = default);

    /// <summary>
    /// Verifies that a specific Algorand address holds (opted into) a given ASA.
    /// </summary>
    /// <param name="assetId">The Algorand ASA ID to check</param>
    /// <param name="address">The Algorand address to verify ownership for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing true if the address holds the ASA</returns>
    Task<Result<bool>> VerifyOwnershipAsync(
        string assetId,
        string address,
        CancellationToken ct = default);

    /// <summary>
    /// Builds the ARC-19 metadata JSON string for a credential.
    /// The JSON follows the ARC-19 standard with credential-specific properties.
    /// </summary>
    /// <param name="credential">The credential metadata input</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the metadata JSON string</returns>
    Task<Result<string>> BuildARC19MetadataAsync(
        CredentialMetadataInput credential,
        CancellationToken ct = default);

    // ========================================================================
    // Fungible ASA Operations (Track 09 — Tokenomics)
    // ========================================================================

    /// <summary>
    /// Creates a new fungible Algorand Standard Asset (e.g., ARDA token, project tokens).
    /// The platform account is the creator and manager.
    /// </summary>
    /// <param name="name">The display name of the asset</param>
    /// <param name="unitName">Short unit name (e.g., "ARDA", "PROJ")</param>
    /// <param name="totalSupply">Total supply of the asset (in base units)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the asset ID and creation transaction hash</returns>
    Task<Result<FungibleAsaCreateResult>> CreateFungibleASAAsync(
        string name,
        string unitName,
        ulong totalSupply,
        CancellationToken ct = default);

    /// <summary>
    /// Transfers a fungible ASA from the platform account to a recipient.
    /// </summary>
    /// <param name="assetId">The ASA ID to transfer</param>
    /// <param name="recipientAddress">The recipient Algorand address</param>
    /// <param name="amount">Amount to transfer (in base units)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the transfer transaction hash</returns>
    Task<Result<string>> TransferASAAsync(
        string assetId,
        string recipientAddress,
        ulong amount,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the balance of a specific ASA held by an address.
    /// </summary>
    /// <param name="assetId">The ASA ID to check</param>
    /// <param name="address">The Algorand address to check balance for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the balance in base units</returns>
    Task<Result<ulong>> GetASABalanceAsync(
        string assetId,
        string address,
        CancellationToken ct = default);

    /// <summary>
    /// Clawbacks (force-transfers) a fungible ASA from an address back to the platform.
    /// Requires the platform account to be set as the clawback address on the ASA.
    /// </summary>
    /// <param name="assetId">The ASA ID to clawback</param>
    /// <param name="fromAddress">The address to clawback from</param>
    /// <param name="amount">Amount to clawback (in base units)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the clawback transaction hash</returns>
    Task<Result<string>> ClawbackASAAsync(
        string assetId,
        string fromAddress,
        ulong amount,
        CancellationToken ct = default);
}

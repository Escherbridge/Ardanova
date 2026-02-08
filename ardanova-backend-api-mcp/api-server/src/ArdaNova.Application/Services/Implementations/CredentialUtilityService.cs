namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates credential lifecycle operations by combining off-chain credential management
/// (IMembershipCredentialService) with on-chain Algorand operations (IAlgorandService).
/// Follows graceful degradation: if the blockchain is unavailable, credentials are still valid off-chain.
/// </summary>
public class CredentialUtilityService : ICredentialUtilityService
{
    private readonly IMembershipCredentialService _credentialService;
    private readonly IAlgorandService _algorandService;
    private readonly ILogger<CredentialUtilityService> _logger;

    /// <summary>
    /// Placeholder platform address used as the recipient for custodial minting.
    /// In a production environment this would come from configuration.
    /// </summary>
    private const string PlatformAddress = "PLATFORM_ADDRESS_PLACEHOLDER";

    public CredentialUtilityService(
        IMembershipCredentialService credentialService,
        IAlgorandService algorandService,
        ILogger<CredentialUtilityService> logger)
    {
        _credentialService = credentialService;
        _algorandService = algorandService;
        _logger = logger;
    }

    public async Task<Result<MembershipCredentialDto>> GrantAndMintAsync(
        GrantMembershipCredentialDto dto,
        CancellationToken ct = default)
    {
        // 1. Grant credential off-chain
        var grantResult = await _credentialService.GrantAsync(dto, ct);
        if (grantResult.IsFailure)
            return grantResult;

        var credential = grantResult.Value!;

        // 2. Determine scope
        var scope = !string.IsNullOrEmpty(dto.ProjectId) ? "PROJECT" : "GUILD";
        var scopeId = !string.IsNullOrEmpty(dto.ProjectId) ? dto.ProjectId! : dto.GuildId!;

        // 3. Build ARC-19 metadata
        var metadataInput = new CredentialMetadataInput
        {
            CredentialId = credential.Id,
            Scope = scope,
            ScopeId = scopeId,
            ScopeName = scopeId, // Use ID as name fallback
            UserId = credential.UserId,
            Tier = credential.Tier,
            GrantedVia = credential.GrantedVia,
            GrantedAt = credential.CreatedAt
        };

        var metadataResult = await _algorandService.BuildARC19MetadataAsync(metadataInput, ct);
        if (metadataResult.IsFailure)
        {
            _logger.LogWarning("Failed to build ARC-19 metadata for credential {CredentialId}: {Error}",
                credential.Id, metadataResult.Error);
            return Result<MembershipCredentialDto>.Success(credential);
        }

        // 4. Mint soulbound ASA
        var mintResult = await _algorandService.MintSoulboundASAAsync(PlatformAddress, metadataInput, ct);
        if (mintResult.IsFailure)
        {
            _logger.LogWarning("Failed to mint soulbound ASA for credential {CredentialId}: {Error}. Credential is valid off-chain.",
                credential.Id, mintResult.Error);
            return Result<MembershipCredentialDto>.Success(credential);
        }

        // 5. Update credential with mint info
        var mintInfo = mintResult.Value!;
        var updateMintDto = new UpdateMembershipCredentialMintDto
        {
            MintTxHash = mintInfo.TxHash,
            AssetId = mintInfo.AssetId,
            MetadataUri = metadataResult.Value
        };

        var updateResult = await _credentialService.UpdateMintInfoAsync(credential.Id, updateMintDto, ct);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning("Failed to update mint info for credential {CredentialId}: {Error}",
                credential.Id, updateResult.Error);
            return Result<MembershipCredentialDto>.Success(credential);
        }

        return updateResult;
    }

    public async Task<Result<MembershipCredentialDto>> RevokeAndBurnAsync(
        string id,
        CancellationToken ct = default)
    {
        // 1. Get credential
        var getResult = await _credentialService.GetByIdAsync(id, ct);
        if (getResult.IsFailure)
            return getResult;

        var credential = getResult.Value!;
        string? burnTxHash = null;

        // 2. If credential has an assetId, burn the ASA
        if (!string.IsNullOrEmpty(credential.AssetId))
        {
            var burnResult = await _algorandService.BurnASAAsync(credential.AssetId, ct);
            if (burnResult.IsSuccess)
            {
                burnTxHash = burnResult.Value;
            }
            else
            {
                _logger.LogWarning("Failed to burn ASA {AssetId} for credential {CredentialId}: {Error}. Revoking off-chain only.",
                    credential.AssetId, id, burnResult.Error);
            }
        }

        // 3. Revoke credential off-chain
        var revokeDto = new RevokeMembershipCredentialDto { RevokeTxHash = burnTxHash };
        return await _credentialService.RevokeAsync(id, revokeDto, ct);
    }

    public async Task<Result<MembershipCredentialDto>> UpgradeTierAsync(
        string id,
        string newTier,
        CancellationToken ct = default)
    {
        // 1. Validate the new tier is a valid enum value
        if (!Enum.TryParse<UserTier>(newTier, true, out var parsedNewTier))
            return Result<MembershipCredentialDto>.ValidationError($"Invalid tier: {newTier}");

        // 2. Get credential
        var getResult = await _credentialService.GetByIdAsync(id, ct);
        if (getResult.IsFailure)
            return getResult;

        var credential = getResult.Value!;

        // 3. Validate tier progression (can only go up)
        if (credential.Tier is not null)
        {
            if (!Enum.TryParse<UserTier>(credential.Tier, true, out var currentTier))
                return Result<MembershipCredentialDto>.ValidationError($"Credential has invalid current tier: {credential.Tier}");

            if (parsedNewTier <= currentTier)
                return Result<MembershipCredentialDto>.ValidationError(
                    $"Tier can only go up. Current: {credential.Tier}, Requested: {newTier}");
        }

        // 4. Update tier
        var tierDto = new UpdateCredentialTierDto { Tier = newTier };
        return await _credentialService.UpdateTierAsync(id, tierDto, ct);
    }

    public async Task<Result<MembershipCredentialDto?>> CheckAndAutoGrantAsync(
        string userId,
        string? projectId,
        string? guildId,
        CancellationToken ct = default)
    {
        // 1. Check eligibility
        var eligibilityResult = await _credentialService.CheckEligibilityAsync(userId, projectId, guildId, ct);
        if (eligibilityResult.IsFailure)
            return Result<MembershipCredentialDto?>.Failure(eligibilityResult.Error!);

        var eligibility = eligibilityResult.Value!;
        if (!eligibility.IsEligible)
        {
            // Already has credential or not eligible — return null
            return Result<MembershipCredentialDto?>.Success(null);
        }

        // 2. Build grant DTO
        var grantDto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            GuildId = guildId,
            UserId = userId,
            GrantedVia = "CONTRIBUTION_THRESHOLD"
        };

        // 3. Grant and mint
        var grantResult = await GrantAndMintAsync(grantDto, ct);
        if (grantResult.IsFailure)
            return Result<MembershipCredentialDto?>.Failure(grantResult.Error!);

        return Result<MembershipCredentialDto?>.Success(grantResult.Value);
    }

    public async Task<Result<MembershipCredentialDto>> RetryMintAsync(
        string id,
        CancellationToken ct = default)
    {
        // 1. Get credential
        var getResult = await _credentialService.GetByIdAsync(id, ct);
        if (getResult.IsFailure)
            return getResult;

        var credential = getResult.Value!;

        // 2. Validate: must not already be minted
        if (!string.IsNullOrEmpty(credential.AssetId))
            return Result<MembershipCredentialDto>.ValidationError("Credential is already minted on-chain");

        // 3. Validate: must be ACTIVE
        if (credential.Status != "ACTIVE")
            return Result<MembershipCredentialDto>.ValidationError("Only ACTIVE credentials can be retried for minting");

        // 4. Determine scope
        var scope = !string.IsNullOrEmpty(credential.ProjectId) ? "PROJECT" : "GUILD";
        var scopeId = !string.IsNullOrEmpty(credential.ProjectId) ? credential.ProjectId! : credential.GuildId!;

        // 5. Build metadata and mint
        var metadataInput = new CredentialMetadataInput
        {
            CredentialId = credential.Id,
            Scope = scope,
            ScopeId = scopeId,
            ScopeName = scopeId,
            UserId = credential.UserId,
            Tier = credential.Tier,
            GrantedVia = credential.GrantedVia,
            GrantedAt = credential.CreatedAt
        };

        var metadataResult = await _algorandService.BuildARC19MetadataAsync(metadataInput, ct);
        if (metadataResult.IsFailure)
            return Result<MembershipCredentialDto>.Failure($"Failed to build metadata: {metadataResult.Error}");

        var mintResult = await _algorandService.MintSoulboundASAAsync(PlatformAddress, metadataInput, ct);
        if (mintResult.IsFailure)
            return Result<MembershipCredentialDto>.Failure($"Mint retry failed: {mintResult.Error}");

        // 6. Update credential with mint info
        var mintInfo = mintResult.Value!;
        var updateMintDto = new UpdateMembershipCredentialMintDto
        {
            MintTxHash = mintInfo.TxHash,
            AssetId = mintInfo.AssetId,
            MetadataUri = metadataResult.Value
        };

        return await _credentialService.UpdateMintInfoAsync(id, updateMintDto, ct);
    }

    public async Task<Result<CredentialWithChainDataDto>> GetCredentialWithChainDataAsync(
        string id,
        CancellationToken ct = default)
    {
        // 1. Get credential
        var getResult = await _credentialService.GetByIdAsync(id, ct);
        if (getResult.IsFailure)
            return Result<CredentialWithChainDataDto>.NotFound(getResult.Error!);

        var credential = getResult.Value!;
        AsaInfoDto? asaInfo = null;
        bool chainVerified = false;
        bool isOnChain = !string.IsNullOrEmpty(credential.AssetId);

        // 2. If credential has an assetId, fetch on-chain data
        if (isOnChain)
        {
            var asaResult = await _algorandService.GetASAInfoAsync(credential.AssetId!, ct);
            if (asaResult.IsSuccess)
            {
                asaInfo = asaResult.Value;
            }
            else
            {
                _logger.LogWarning("Failed to fetch ASA info for asset {AssetId}: {Error}",
                    credential.AssetId, asaResult.Error);
            }

            // 3. Verify ownership
            var ownershipResult = await _algorandService.VerifyOwnershipAsync(
                credential.AssetId!, PlatformAddress, ct);
            if (ownershipResult.IsSuccess)
            {
                chainVerified = ownershipResult.Value;
            }
            else
            {
                _logger.LogWarning("Failed to verify ownership for asset {AssetId}: {Error}",
                    credential.AssetId, ownershipResult.Error);
            }
        }

        var result = new CredentialWithChainDataDto
        {
            Credential = credential,
            AsaInfo = asaInfo,
            IsOnChain = isOnChain,
            ChainVerified = chainVerified
        };

        return Result<CredentialWithChainDataDto>.Success(result);
    }
}

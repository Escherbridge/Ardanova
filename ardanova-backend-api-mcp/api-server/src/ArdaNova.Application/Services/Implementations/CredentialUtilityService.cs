namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates credential lifecycle operations by combining off-chain credential management
/// (IMembershipCredentialService) with on-chain Algorand operations (IAlgorandService).
/// Follows graceful degradation: if the blockchain is unavailable, credentials are still valid off-chain.
/// </summary>
public class CredentialUtilityService : ICredentialUtilityService
{
    /// <summary>Idempotency-key prefix for issuance; one credential is issued at most once.</summary>
    private const string IssuanceKeyPrefix = "membership-credential-issue";

    /// <summary>The stated refusal when no issuance transport is wired; also the retry conflict.</summary>
    public const string IssuanceTransportUnavailable =
        "The credential issuance transport is not configured, so this credential is recorded but " +
        "not yet independently verifiable. Register an IAzoaCredentialIssuanceNode adapter for " +
        "POST /api/credential/issue/{recipientUserId}.";

    private readonly IMembershipCredentialService _credentialService;
    private readonly IAlgorandService _algorandService;
    private readonly ILogger<CredentialUtilityService> _logger;
    private readonly string _platformAddress;
    private readonly AlgorandProviderCapabilities _capabilities;
    private readonly IAzoaCredentialIssuanceNode? _issuanceNode;
    private readonly IAzoaCustodialAccountService? _custodialAccounts;

    public CredentialUtilityService(
        IMembershipCredentialService credentialService,
        IAlgorandService algorandService,
        IConfiguration configuration,
        ILogger<CredentialUtilityService> logger,
        AlgorandProviderCapabilities capabilities,
        IAzoaCredentialIssuanceNode? issuanceNode = null,
        IAzoaCustodialAccountService? custodialAccounts = null)
    {
        _credentialService = credentialService;
        _algorandService = algorandService;
        _logger = logger;
        _capabilities = capabilities;
        _issuanceNode = issuanceNode;
        _custodialAccounts = custodialAccounts;
        _platformAddress = configuration["Algorand:PlatformAddress"] ?? string.Empty;
    }

    public CredentialUtilityService(
        IMembershipCredentialService credentialService,
        IAlgorandService algorandService,
        IConfiguration configuration,
        ILogger<CredentialUtilityService> logger)
        : this(
            credentialService,
            algorandService,
            configuration,
            logger,
            new AlgorandProviderCapabilities("Test", false, true))
    {
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

        // The real issuance rail, and the ONLY one. AZOA's issuer-facing door is recipient-keyed,
        // so it is the only path that can put a credential in the MEMBER's hands.
        if (_issuanceNode is not null)
            return await IssueThroughNodeAsync(credential, ct);

        // No transport, no proof, and no mint. The address-based fallback that used to run here
        // minted to the PLATFORM's own address: under the Legacy provider that is a real asset
        // proving nothing about the member, and under the shipped Simulated provider the asset id
        // and transaction hash were fabricated outright - while the member's card reads any
        // recorded reference as "Verified - independently verifiable outside ArdaNova".
        // Recording nothing leaves the card reading "Verification pending", which is the truth.
        _logger.LogError(
            "No credential issuance transport is registered for credential {CredentialId}. {Reason}",
            credential.Id,
            IssuanceTransportUnavailable);

        return Result<MembershipCredentialDto>.Success(credential);
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
            if (!_capabilities.SupportsAddressBasedCredentialLifecycle)
            {
                return Result<MembershipCredentialDto>.Conflict(
                    $"Provider {_capabilities.Provider} cannot burn the existing address-based credential asset; revocation was not changed.");
            }

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
        // Verification comes from the issuance node or from nowhere. With no transport there is
        // nothing to retry: the address-based mint that used to run here recorded a reference the
        // member could not verify, so re-running it would only manufacture the false claim again.
        if (_issuanceNode is null)
        {
            _logger.LogError(
                "Verification was re-requested for credential {CredentialId} with no issuance transport. {Reason}",
                id,
                IssuanceTransportUnavailable);
            return Result<MembershipCredentialDto>.Conflict(IssuanceTransportUnavailable);
        }

        var pending = await _credentialService.GetByIdAsync(id, ct);
        if (pending.IsFailure)
            return pending;

        if (!string.IsNullOrEmpty(pending.Value!.AssetId))
            return Result<MembershipCredentialDto>.ValidationError("This credential is already verified.");

        if (pending.Value!.Status != "ACTIVE")
            return Result<MembershipCredentialDto>.ValidationError(
                "Only an active credential can be re-submitted for verification.");

        // Unlike the grant path a retry must surface its failure: the caller asked for exactly
        // this one thing, so a silent "still pending" would be indistinguishable from success.
        return await IssueThroughNodeAsync(pending.Value!, ct, surfaceFailure: true);
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

        // The identifier alone decides NOTHING here. A credential predating the issuance-node
        // path carries an assetId written by the removed address-based mint (which minted to
        // _platformAddress) or fabricated by the simulated provider; reading it as "on record"
        // and then checking _platformAddress's ownership of it would return true and let the
        // detail page render "Verified" over a credential that was never issued to anybody.
        // Same gate as the member-facing screens use, asked HERE so the server never emits the
        // claim in the first place rather than relying on a client to throw it away.
        bool isOnChain = credential.IsIndependentlyVerifiable
            && !string.IsNullOrEmpty(credential.AssetId);

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
                credential.AssetId!, _platformAddress, ct);
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

    /// <summary>Issues the credential to its holder through AZOA's issuer-facing credential door.</summary>
    /// <remarks>
    /// <para>Route: <c>POST /api/credential/issue/{recipientUserId}</c>, called with ArdaNova's
    /// tenant <c>dapp:manager</c> credential. ArdaNova is the ISSUER of record, and that is what
    /// makes the credential withdrawable later: the node refuses an issuance whose issuer equals
    /// its holder, because "only the issuer may revoke" would otherwise let the holder revoke
    /// their own credential.</para>
    /// <para>Failure is <b>pending, never verified</b>. The off-chain grant already succeeded, so
    /// the member keeps their membership; what they do not get is a proof they do not have. Every
    /// exit below either records a real issuance reference or leaves <c>AssetId</c> null, and this
    /// method is the ONLY writer of that field - which is what lets a screen treat a recorded
    /// reference as evidence of issuance rather than of a provider having invented one.</para>
    /// </remarks>
    private async Task<Result<MembershipCredentialDto>> IssueThroughNodeAsync(
        MembershipCredentialDto credential,
        CancellationToken ct,
        bool surfaceFailure = false)
    {
        Result<MembershipCredentialDto> Pending(string reason)
        {
            _logger.LogWarning(
                "Membership credential {CredentialId} is recorded but not verified: {Reason}",
                credential.Id,
                reason);

            // A retry asked for exactly this one thing, so silence would read as success.
            return surfaceFailure
                ? Result<MembershipCredentialDto>.Failure(reason)
                : Result<MembershipCredentialDto>.Success(credential);
        }

        if (_custodialAccounts is null)
            return Pending(IssuanceTransportUnavailable);

        var account = await _custodialAccounts.GetStatusAsync(credential.UserId, ct);
        if (account.IsFailure)
            return Pending($"The member's account could not be read: {account.Error}");

        // No provisioned account means the node has no holder to issue to. Refusing here is the
        // same rule the node enforces itself; anticipating it keeps the reason readable.
        if (!Guid.TryParse(account.Value!.AvatarId, out var recipientAvatarId) || recipientAvatarId == Guid.Empty)
            return Pending("The member does not have a provisioned account yet, so there is nothing to issue to.");

        var scope = !string.IsNullOrEmpty(credential.ProjectId) ? "PROJECT" : "GUILD";
        var scopeId = !string.IsNullOrEmpty(credential.ProjectId) ? credential.ProjectId! : credential.GuildId!;

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
            return Pending($"The credential description could not be prepared: {metadataResult.Error}");

        var issueRequest = new MembershipCredentialIssueRequest(
            CredentialId: credential.Id,
            Scope: scope,
            ScopeId: scopeId,
            RecipientUserId: credential.UserId,
            Tier: credential.Tier,
            GrantedVia: credential.GrantedVia,
            MetadataUri: metadataResult.Value!);

        // Stable per-credential key: a redelivered grant issues once, not twice.
        var issued = await _issuanceNode!.IssueAsync(
            recipientAvatarId,
            issueRequest,
            $"{IssuanceKeyPrefix}:{credential.Id}",
            ct);

        if (issued.IsFailure)
            return Pending($"Verification could not be completed: {issued.Error}");

        var update = await _credentialService.UpdateMintInfoAsync(
            credential.Id,
            new UpdateMembershipCredentialMintDto
            {
                // Legacy column names. MintTxHash carries the node's issuance reference and
                // AssetId the proof reference the member can show; neither is rendered as chain
                // vocabulary anywhere a member can see. Renaming the columns is tracked separately.
                MintTxHash = issued.Value!.IssuanceReference,
                AssetId = issued.Value!.ProofReference,
                MetadataUri = issued.Value!.MetadataUri ?? metadataResult.Value,
                // The ONLY place this origin is stated, and so the only way a credential comes
                // to read as independently verifiable. See MembershipCredentialDto.
                Origin = CredentialProofOrigin.IssuanceNode,
            },
            ct);

        return update.IsFailure
            ? Pending($"The credential was issued but the proof could not be recorded: {update.Error}")
            : update;
    }
}

/// <summary>What ArdaNova asks the node to issue, in the node's own recipient-keyed shape.</summary>
public sealed record MembershipCredentialIssueRequest(
    string CredentialId,
    string Scope,
    string ScopeId,
    string RecipientUserId,
    string? Tier,
    string GrantedVia,
    string MetadataUri);

/// <summary>What the node reports back after issuing one credential.</summary>
/// <param name="IssuanceReference">The node's handle on the issuance itself.</param>
/// <param name="ProofReference">The holder-visible proof reference; null when none was assigned.</param>
/// <param name="MetadataUri">Where the credential's description was published, when the node moved it.</param>
/// <param name="IssuedAt">When the node recorded the issuance.</param>
public sealed record MembershipCredentialIssuance(
    string IssuanceReference,
    string? ProofReference,
    string? MetadataUri,
    DateTimeOffset IssuedAt);

/// <summary>
/// Application-layer PORT onto AZOA's issuer-facing credential door
/// (<c>POST /api/credential/issue/{recipientUserId}</c>), called with ArdaNova's tenant
/// <c>dapp:manager</c> credential.
/// </summary>
/// <remarks>
/// Declared here, next to its only consumer, for the same reason as <c>IAzoaExchangeNode</c> and
/// <c>IAzoaProjectTokenNodeGateway</c>: the Application layer names the door it needs and
/// Infrastructure supplies the transport, so no caller assembles its own node request. Until an
/// adapter is registered, <see cref="CredentialUtilityService"/> issues nothing and every
/// credential it grants stays "Verification pending" - it never claims a proof it does not hold.
/// </remarks>
public interface IAzoaCredentialIssuanceNode
{
    /// <summary>Issues one credential to <paramref name="recipientAvatarId"/>. Idempotent on the key.</summary>
    Task<Result<MembershipCredentialIssuance>> IssueAsync(
        Guid recipientAvatarId,
        MembershipCredentialIssueRequest request,
        string idempotencyKey,
        CancellationToken ct = default);
}

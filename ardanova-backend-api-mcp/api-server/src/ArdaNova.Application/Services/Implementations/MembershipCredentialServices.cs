namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

/// <summary>
/// Membership credentials, and the single place a credential's tier is written.
/// </summary>
/// <remarks>
/// <para><b>One tier, not two columns.</b> <c>User.tier</c> and
/// <c>MembershipCredential.tier</c> used to be independently writable, so a project could
/// assert GOLD on a credential held by a BRONZE member and nothing anywhere would notice.
/// There is now ONE derivation — <see cref="IXPEventService.CalculateTier"/> over the
/// holder's earned XP — and the credential carries a mirror of it, never a value of its own.
/// The mirror is enforced at every seam that can move either number:</para>
/// <list type="bullet">
/// <item><see cref="GrantAsync"/> stamps the derived tier onto a new (or re-issued) credential.</item>
/// <item><see cref="UpdateTierAsync"/> REFUSES a tier that is not the holder's derived tier.</item>
/// <item><c>XPEventService.AwardXPAsync</c> re-stamps every live credential when XP moves the tier.</item>
/// </list>
/// <para>The two derivation collaborators are optional ONLY so the pre-existing unit-test
/// constructor keeps compiling; production DI always supplies them. When they are absent the
/// tier is left unknown rather than guessed — an unknown tier is honest, an invented one is
/// the defect this class exists to close.</para>
/// </remarks>
public class MembershipCredentialService : IMembershipCredentialService
{
    private readonly IRepository<MembershipCredential> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IKycGateService _kycGateService;
    private readonly IRepository<User>? _userRepository;
    private readonly IXPEventService? _xpEvents;

    public MembershipCredentialService(
        IRepository<MembershipCredential> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IKycGateService kycGateService,
        IRepository<User>? userRepository = null,
        IXPEventService? xpEvents = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kycGateService = kycGateService;
        _userRepository = userRepository;
        _xpEvents = xpEvents;
    }

    /// <summary>The one tier a member holds: earned from XP, never asserted by a caller.</summary>
    private async Task<UserTier?> ResolveHolderTierAsync(string userId, CancellationToken ct)
    {
        if (_userRepository is null || _xpEvents is null)
            return null;

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return null;

        // Recomputed from totalXP rather than read off User.tier: the stored column is a
        // projection and a stale one must not be copied onto a credential.
        return _xpEvents.CalculateTier(user.totalXP);
    }

    public async Task<Result<MembershipCredentialDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var credential = await _repository.GetByIdAsync(id, ct);
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"MembershipCredential with id {id} not found");
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.projectId == projectId, ct);
        return Result<IReadOnlyList<MembershipCredentialDto>>.Success(_mapper.Map<IReadOnlyList<MembershipCredentialDto>>(credentials));
    }

    public async Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.userId == userId, ct);
        return Result<IReadOnlyList<MembershipCredentialDto>>.Success(_mapper.Map<IReadOnlyList<MembershipCredentialDto>>(credentials));
    }

    public async Task<Result<MembershipCredentialDto>> GetByProjectAndUserAsync(string projectId, string userId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.projectId == projectId && c.userId == userId, ct);
        var credential = credentials.FirstOrDefault();
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"No MembershipCredential found for user {userId} in project {projectId}");
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.projectId == projectId && c.status == MembershipCredentialStatus.ACTIVE, ct);
        return Result<IReadOnlyList<MembershipCredentialDto>>.Success(_mapper.Map<IReadOnlyList<MembershipCredentialDto>>(credentials));
    }

    // ========================================================================
    // Guild Queries
    // ========================================================================

    public async Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.guildId == guildId, ct);
        return Result<IReadOnlyList<MembershipCredentialDto>>.Success(_mapper.Map<IReadOnlyList<MembershipCredentialDto>>(credentials));
    }

    public async Task<Result<MembershipCredentialDto>> GetByGuildAndUserAsync(string guildId, string userId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.guildId == guildId && c.userId == userId, ct);
        var credential = credentials.FirstOrDefault();
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"No MembershipCredential found for user {userId} in guild {guildId}");
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetActiveByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var credentials = await _repository.FindAsync(c => c.guildId == guildId && c.status == MembershipCredentialStatus.ACTIVE, ct);
        return Result<IReadOnlyList<MembershipCredentialDto>>.Success(_mapper.Map<IReadOnlyList<MembershipCredentialDto>>(credentials));
    }

    // ========================================================================
    // Tier Management
    // ========================================================================

    public async Task<Result<MembershipCredentialDto>> UpdateTierAsync(string id, UpdateCredentialTierDto dto, CancellationToken ct = default)
    {
        var credential = await _repository.GetByIdAsync(id, ct);
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"MembershipCredential with id {id} not found");

        if (credential.status != MembershipCredentialStatus.ACTIVE)
            return Result<MembershipCredentialDto>.ValidationError("Only active credentials can have their tier updated");

        if (!Enum.TryParse<UserTier>(dto.Tier, true, out var tier))
            return Result<MembershipCredentialDto>.ValidationError($"Invalid tier: {dto.Tier}");

        // The credential mirrors the holder's earned standing; it does not get to disagree with
        // it. A request naming a different tier is refused rather than silently corrected, so the
        // caller learns that tier is earned and not granted.
        var holderTier = await ResolveHolderTierAsync(credential.userId, ct);
        if (holderTier is not null && holderTier.Value != tier)
            return Result<MembershipCredentialDto>.Conflict(
                $"This member's standing is {holderTier.Value}, not {tier}. A membership credential " +
                "mirrors the tier the member has earned; it cannot be set independently.");

        credential.tier = holderTier ?? tier;
        credential.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    // ========================================================================
    // Eligibility Check
    // ========================================================================

    public async Task<Result<CredentialEligibilityDto>> CheckEligibilityAsync(string userId, string? projectId, string? guildId, CancellationToken ct = default)
    {
        if (projectId is null && guildId is null)
            return Result<CredentialEligibilityDto>.ValidationError("Must provide either projectId or guildId");

        // Check for existing active credential
        IReadOnlyList<MembershipCredential> existing;
        if (projectId is not null)
        {
            existing = await _repository.FindAsync(c =>
                c.projectId == projectId &&
                c.userId == userId &&
                c.status == MembershipCredentialStatus.ACTIVE, ct);
        }
        else
        {
            existing = await _repository.FindAsync(c =>
                c.guildId == guildId &&
                c.userId == userId &&
                c.status == MembershipCredentialStatus.ACTIVE, ct);
        }

        if (existing.Any())
        {
            return Result<CredentialEligibilityDto>.Success(new CredentialEligibilityDto
            {
                IsEligible = false,
                Reason = "User already has an active credential for this scope"
            });
        }

        return Result<CredentialEligibilityDto>.Success(new CredentialEligibilityDto
        {
            IsEligible = true
        });
    }

    // ========================================================================
    // Grant (with XOR validation for projectId/guildId)
    // ========================================================================

    public async Task<Result<MembershipCredentialDto>> GrantAsync(GrantMembershipCredentialDto dto, CancellationToken ct = default)
    {
        // XOR validation: must have exactly one of projectId or guildId
        var hasProject = !string.IsNullOrEmpty(dto.ProjectId);
        var hasGuild = !string.IsNullOrEmpty(dto.GuildId);
        if (hasProject == hasGuild) // both true or both false
            return Result<MembershipCredentialDto>.ValidationError("Grant must have exactly one of projectId or guildId (not both, not neither)");

        // KYC gate: require PRO verification to receive credentials
        var gateResult = await _kycGateService.RequireProAsync(dto.UserId, ct);
        if (!gateResult.IsSuccess)
        {
            // Propagate, never flatten: forcing every refusal to Forbidden would undo the
            // gate's distinction between "could not confirm" and "not allowed" right here.
            return ValueEligibilityFailure.Propagate<bool, MembershipCredentialDto>(gateResult);
        }

        // Validate grantedVia enum
        if (!Enum.TryParse<MembershipGrantType>(dto.GrantedVia, true, out var grantType))
            return Result<MembershipCredentialDto>.ValidationError($"Invalid grant type: {dto.GrantedVia}");

        // Validate that grantedByProposalId is only set for DAO_VOTE
        if (dto.GrantedByProposalId != null && grantType != MembershipGrantType.DAO_VOTE)
            return Result<MembershipCredentialDto>.ValidationError("grantedByProposalId can only be set when grantedVia is DAO_VOTE");

        // Check if credential already exists for this user + project/guild
        MembershipCredential? existing;
        if (hasProject)
        {
            existing = (await _repository.FindAsync(c =>
                c.projectId == dto.ProjectId &&
                c.userId == dto.UserId, ct)).FirstOrDefault();
        }
        else
        {
            existing = (await _repository.FindAsync(c =>
                c.guildId == dto.GuildId &&
                c.userId == dto.UserId, ct)).FirstOrDefault();
        }

        // Stamped from the holder's earned standing, never from the request: the credential is a
        // mirror of User.tier, so the two can never be written to different values.
        var holderTier = await ResolveHolderTierAsync(dto.UserId, ct);

        if (existing is not null)
        {
            // Allow re-minting if the existing credential was REVOKED
            if (existing.status == MembershipCredentialStatus.REVOKED)
            {
                existing.status = MembershipCredentialStatus.ACTIVE;
                existing.isTransferable = false;
                existing.grantedVia = grantType;
                existing.grantedByProposalId = dto.GrantedByProposalId;
                existing.tier = holderTier;
                existing.mintedAt = DateTime.UtcNow;
                existing.revokedAt = null;
                existing.revokeTxHash = null;
                existing.assetId = null;
                existing.mintTxHash = null;
                existing.issuedThroughNodeAt = null;
                existing.metadataUri = null;
                existing.updatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(existing, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(existing));
            }

            var scopeLabel = hasProject ? "project" : "guild";
            return Result<MembershipCredentialDto>.ValidationError($"User already has an active membership credential for this {scopeLabel}");
        }

        var credential = new MembershipCredential
        {
            id = Guid.NewGuid().ToString(),
            projectId = hasProject ? dto.ProjectId : null,
            guildId = hasGuild ? dto.GuildId : null,
            userId = dto.UserId,
            status = MembershipCredentialStatus.ACTIVE,
            isTransferable = false,
            tier = holderTier,
            grantedVia = grantType,
            grantedByProposalId = dto.GrantedByProposalId,
            mintedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<MembershipCredentialDto>> RevokeAsync(string id, RevokeMembershipCredentialDto? dto = null, CancellationToken ct = default)
    {
        var credential = await _repository.GetByIdAsync(id, ct);
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"MembershipCredential with id {id} not found");

        if (credential.status == MembershipCredentialStatus.REVOKED)
            return Result<MembershipCredentialDto>.ValidationError("Credential is already revoked");

        credential.status = MembershipCredentialStatus.REVOKED;
        credential.revokedAt = DateTime.UtcNow;
        credential.revokeTxHash = dto?.RevokeTxHash;
        credential.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<MembershipCredentialDto>> SuspendAsync(string id, CancellationToken ct = default)
    {
        var credential = await _repository.GetByIdAsync(id, ct);
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"MembershipCredential with id {id} not found");

        if (credential.status != MembershipCredentialStatus.ACTIVE)
            return Result<MembershipCredentialDto>.ValidationError("Only active credentials can be suspended");

        credential.status = MembershipCredentialStatus.SUSPENDED;
        credential.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<MembershipCredentialDto>> ReactivateAsync(string id, CancellationToken ct = default)
    {
        var credential = await _repository.GetByIdAsync(id, ct);
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"MembershipCredential with id {id} not found");

        if (credential.status != MembershipCredentialStatus.SUSPENDED)
            return Result<MembershipCredentialDto>.ValidationError("Only suspended credentials can be reactivated");

        credential.status = MembershipCredentialStatus.ACTIVE;
        credential.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }

    public async Task<Result<MembershipCredentialDto>> UpdateMintInfoAsync(string id, UpdateMembershipCredentialMintDto dto, CancellationToken ct = default)
    {
        var credential = await _repository.GetByIdAsync(id, ct);
        if (credential is null)
            return Result<MembershipCredentialDto>.NotFound($"MembershipCredential with id {id} not found");

        credential.mintTxHash = dto.MintTxHash;
        if (dto.AssetId is not null) credential.assetId = dto.AssetId;
        if (dto.MetadataUri is not null) credential.metadataUri = dto.MetadataUri;
        credential.mintedAt = DateTime.UtcNow;

        // The one place issuedThroughNodeAt is written, and the only origin that sets it. Any
        // other origin CLEARS it: a platform-held mint recording over a node issuance would
        // otherwise leave the earlier stamp standing while the identifiers next to it changed.
        credential.issuedThroughNodeAt = dto.Origin == CredentialProofOrigin.IssuanceNode
            ? DateTime.UtcNow
            : null;
        credential.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }
}

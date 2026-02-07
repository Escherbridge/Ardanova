namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class MembershipCredentialService : IMembershipCredentialService
{
    private readonly IRepository<MembershipCredential> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MembershipCredentialService(IRepository<MembershipCredential> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

    public async Task<Result<MembershipCredentialDto>> GrantAsync(GrantMembershipCredentialDto dto, CancellationToken ct = default)
    {
        // Validate grantedVia enum
        if (!Enum.TryParse<MembershipGrantType>(dto.GrantedVia, true, out var grantType))
            return Result<MembershipCredentialDto>.ValidationError($"Invalid grant type: {dto.GrantedVia}");

        // Validate that grantedByProposalId is only set for DAO_VOTE
        if (dto.GrantedByProposalId != null && grantType != MembershipGrantType.DAO_VOTE)
            return Result<MembershipCredentialDto>.ValidationError("grantedByProposalId can only be set when grantedVia is DAO_VOTE");

        // Check if credential already exists for this user + project
        var existing = (await _repository.FindAsync(c =>
            c.projectId == dto.ProjectId &&
            c.userId == dto.UserId, ct)).FirstOrDefault();

        if (existing is not null)
        {
            // Allow re-minting if the existing credential was REVOKED
            if (existing.status == MembershipCredentialStatus.REVOKED)
            {
                existing.status = MembershipCredentialStatus.ACTIVE;
                existing.isTransferable = false;
                existing.grantedVia = grantType;
                existing.grantedByProposalId = dto.GrantedByProposalId;
                existing.mintedAt = DateTime.UtcNow;
                existing.revokedAt = null;
                existing.revokeTxHash = null;
                existing.updatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(existing, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(existing));
            }

            return Result<MembershipCredentialDto>.ValidationError("User already has an active membership credential for this project");
        }

        var credential = new MembershipCredential
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            status = MembershipCredentialStatus.ACTIVE,
            isTransferable = false,
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
        credential.mintedAt = DateTime.UtcNow;
        credential.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(credential, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MembershipCredentialDto>.Success(_mapper.Map<MembershipCredentialDto>(credential));
    }
}

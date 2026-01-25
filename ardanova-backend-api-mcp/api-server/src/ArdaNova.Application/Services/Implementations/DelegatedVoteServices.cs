namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

public class DelegatedVoteService : IDelegatedVoteService
{
    private readonly IRepository<DelegatedVote> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DelegatedVoteService(IRepository<DelegatedVote> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<DelegatedVoteDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var vote = await _repository.GetByIdAsync(id, ct);
        if (vote is null)
            return Result<DelegatedVoteDto>.NotFound($"DelegatedVote with id {id} not found");
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegatorIdAsync(string delegatorId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.delegatorId == delegatorId, ct);
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(votes));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegateeIdAsync(string delegateeId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.delegateeId == delegateeId, ct);
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(votes));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.projectId == projectId, ct);
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(votes));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.projectId == projectId && v.isActive, ct);
        // Filter out expired delegations
        var activeVotes = votes.Where(v => !IsExpired(v)).ToList();
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(activeVotes));
    }

    public async Task<Result<decimal>> GetTotalDelegatedPowerAsync(string delegateeId, string projectId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v =>
            v.delegateeId == delegateeId &&
            v.projectId == projectId &&
            v.isActive, ct);

        var total = votes.Where(v => !IsExpired(v)).Sum(v => v.amount);
        return Result<decimal>.Success(total);
    }

    public async Task<Result<DelegatedVoteDto>> CreateAsync(CreateDelegatedVoteDto dto, CancellationToken ct = default)
    {
        // Check if delegation already exists
        var exists = await _repository.ExistsAsync(v =>
            v.projectId == dto.ProjectId &&
            v.delegatorId == dto.DelegatorId &&
            v.tokenId == dto.TokenId &&
            v.isActive, ct);

        if (exists)
            return Result<DelegatedVoteDto>.ValidationError("Active delegation already exists for this token");

        var vote = new DelegatedVote
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            delegatorId = dto.DelegatorId,
            delegateeId = dto.DelegateeId,
            tokenId = dto.TokenId,
            amount = dto.Amount,
            isActive = true,
            createdAt = DateTime.UtcNow,
            expiresAt = dto.ExpiresAt
        };

        await _repository.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    public async Task<Result<DelegatedVoteDto>> UpdateAsync(string id, UpdateDelegatedVoteDto dto, CancellationToken ct = default)
    {
        var vote = await _repository.GetByIdAsync(id, ct);
        if (vote is null)
            return Result<DelegatedVoteDto>.NotFound($"DelegatedVote with id {id} not found");

        if (!vote.isActive)
            return Result<DelegatedVoteDto>.ValidationError("Cannot update revoked delegation");

        if (dto.Amount.HasValue)
            vote.amount = dto.Amount.Value;

        if (dto.ExpiresAt.HasValue)
            vote.expiresAt = dto.ExpiresAt.Value;

        await _repository.UpdateAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    public async Task<Result<DelegatedVoteDto>> RevokeAsync(string id, CancellationToken ct = default)
    {
        var vote = await _repository.GetByIdAsync(id, ct);
        if (vote is null)
            return Result<DelegatedVoteDto>.NotFound($"DelegatedVote with id {id} not found");

        if (!vote.isActive)
            return Result<DelegatedVoteDto>.ValidationError("Delegation is already revoked");

        vote.isActive = false;
        vote.revokedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    private static bool IsExpired(DelegatedVote vote)
    {
        return vote.expiresAt.HasValue && vote.expiresAt.Value < DateTime.UtcNow;
    }
}

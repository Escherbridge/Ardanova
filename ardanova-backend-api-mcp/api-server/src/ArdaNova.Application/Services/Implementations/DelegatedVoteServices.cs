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

    public async Task<Result<DelegatedVoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var vote = await _repository.GetByIdAsync(id, ct);
        if (vote is null)
            return Result<DelegatedVoteDto>.NotFound($"DelegatedVote with id {id} not found");
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegatorIdAsync(Guid delegatorId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.DelegatorId == delegatorId, ct);
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(votes));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegateeIdAsync(Guid delegateeId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.DelegateeId == delegateeId, ct);
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(votes));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.ProjectId == projectId, ct);
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(votes));
    }

    public async Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetActiveByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v => v.ProjectId == projectId && v.IsActive, ct);
        // Filter out expired delegations
        var activeVotes = votes.Where(v => !v.IsExpired()).ToList();
        return Result<IReadOnlyList<DelegatedVoteDto>>.Success(_mapper.Map<IReadOnlyList<DelegatedVoteDto>>(activeVotes));
    }

    public async Task<Result<decimal>> GetTotalDelegatedPowerAsync(Guid delegateeId, Guid projectId, CancellationToken ct = default)
    {
        var votes = await _repository.FindAsync(v =>
            v.DelegateeId == delegateeId &&
            v.ProjectId == projectId &&
            v.IsActive, ct);

        var total = votes.Where(v => !v.IsExpired()).Sum(v => v.Amount);
        return Result<decimal>.Success(total);
    }

    public async Task<Result<DelegatedVoteDto>> CreateAsync(CreateDelegatedVoteDto dto, CancellationToken ct = default)
    {
        // Check if delegation already exists
        var exists = await _repository.ExistsAsync(v =>
            v.ProjectId == dto.ProjectId &&
            v.DelegatorId == dto.DelegatorId &&
            v.TokenId == dto.TokenId &&
            v.IsActive, ct);

        if (exists)
            return Result<DelegatedVoteDto>.ValidationError("Active delegation already exists for this token");

        var vote = DelegatedVote.Create(
            dto.ProjectId,
            dto.DelegatorId,
            dto.DelegateeId,
            dto.TokenId,
            dto.Amount,
            dto.ExpiresAt
        );

        await _repository.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    public async Task<Result<DelegatedVoteDto>> UpdateAsync(Guid id, UpdateDelegatedVoteDto dto, CancellationToken ct = default)
    {
        var vote = await _repository.GetByIdAsync(id, ct);
        if (vote is null)
            return Result<DelegatedVoteDto>.NotFound($"DelegatedVote with id {id} not found");

        if (!vote.IsActive)
            return Result<DelegatedVoteDto>.ValidationError("Cannot update revoked delegation");

        if (dto.Amount.HasValue)
            vote.UpdateAmount(dto.Amount.Value);

        if (dto.ExpiresAt.HasValue)
            vote.Extend(dto.ExpiresAt.Value);

        await _repository.UpdateAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }

    public async Task<Result<DelegatedVoteDto>> RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var vote = await _repository.GetByIdAsync(id, ct);
        if (vote is null)
            return Result<DelegatedVoteDto>.NotFound($"DelegatedVote with id {id} not found");

        if (!vote.IsActive)
            return Result<DelegatedVoteDto>.ValidationError("Delegation is already revoked");

        vote.Revoke();
        await _repository.UpdateAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<DelegatedVoteDto>.Success(_mapper.Map<DelegatedVoteDto>(vote));
    }
}

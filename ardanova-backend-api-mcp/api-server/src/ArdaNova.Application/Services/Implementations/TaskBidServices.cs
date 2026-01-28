namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TaskBidService : ITaskBidService
{
    private readonly IRepository<TaskBid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaskBidService(IRepository<TaskBid> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TaskBidDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<IReadOnlyList<TaskBidDto>>> GetByTaskIdAsync(string taskId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.taskId == taskId, ct);
        return Result<IReadOnlyList<TaskBidDto>>.Success(_mapper.Map<IReadOnlyList<TaskBidDto>>(bids));
    }

    public async Task<Result<IReadOnlyList<TaskBidDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default)
    {
        var bids = await _repository.FindAsync(b => b.guildId == guildId, ct);
        return Result<IReadOnlyList<TaskBidDto>>.Success(_mapper.Map<IReadOnlyList<TaskBidDto>>(bids));
    }

    public async Task<Result<PagedResult<TaskBidDto>>> SearchAsync(string? taskId, string? guildId, TaskBidStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _repository.Query();

        if (!string.IsNullOrWhiteSpace(taskId))
            query = query.Where(b => b.taskId == taskId);

        if (!string.IsNullOrWhiteSpace(guildId))
            query = query.Where(b => b.guildId == guildId);

        if (status.HasValue)
            query = query.Where(b => b.status == status.Value);

        query = query.OrderByDescending(b => b.createdAt);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Result<PagedResult<TaskBidDto>>.Success(new PagedResult<TaskBidDto>(
            _mapper.Map<List<TaskBidDto>>(items),
            totalCount,
            page,
            pageSize));
    }

    public async Task<Result<TaskBidDto>> CreateAsync(CreateTaskBidDto dto, CancellationToken ct = default)
    {
        var bid = new TaskBid
        {
            id = Guid.NewGuid().ToString(),
            taskId = dto.TaskId,
            guildId = dto.GuildId,
            proposedAmount = dto.ProposedAmount,
            proposal = dto.Proposal,
            estimatedHours = dto.EstimatedHours,
            status = TaskBidStatus.SUBMITTED,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<TaskBidDto>> UpdateAsync(string id, UpdateTaskBidDto dto, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");

        if (dto.ProposedAmount.HasValue) bid.proposedAmount = dto.ProposedAmount;
        if (dto.Proposal is not null) bid.proposal = dto.Proposal;
        if (dto.EstimatedHours.HasValue) bid.estimatedHours = dto.EstimatedHours;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<bool>.NotFound($"Task bid with id {id} not found");

        await _repository.DeleteAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<TaskBidDto>> AcceptAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");

        bid.status = TaskBidStatus.ACCEPTED;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<TaskBidDto>> RejectAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");

        bid.status = TaskBidStatus.REJECTED;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<TaskBidDto>> WithdrawAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");

        bid.status = TaskBidStatus.WITHDRAWN;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<TaskBidDto>> CompleteAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");

        bid.status = TaskBidStatus.COMPLETED;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }

    public async Task<Result<TaskBidDto>> ReviewAsync(string id, CancellationToken ct = default)
    {
        var bid = await _repository.GetByIdAsync(id, ct);
        if (bid is null)
            return Result<TaskBidDto>.NotFound($"Task bid with id {id} not found");

        bid.status = TaskBidStatus.UNDER_REVIEW;
        bid.reviewedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(bid, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskBidDto>.Success(_mapper.Map<TaskBidDto>(bid));
    }
}

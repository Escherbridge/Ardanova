namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class TaskEscrowService : ITaskEscrowService
{
    private readonly IRepository<TaskEscrow> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaskEscrowService(IRepository<TaskEscrow> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TaskEscrowDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var escrow = await _repository.FindOneAsync(e => e.TaskId == taskId, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow for task {taskId} not found");
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<IReadOnlyList<TaskEscrowDto>>> GetByFunderIdAsync(Guid funderId, CancellationToken ct = default)
    {
        var escrows = await _repository.FindAsync(e => e.FunderId == funderId, ct);
        return Result<IReadOnlyList<TaskEscrowDto>>.Success(_mapper.Map<IReadOnlyList<TaskEscrowDto>>(escrows));
    }

    public async Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(e => e.TaskId == dto.TaskId, ct);
        if (exists)
            return Result<TaskEscrowDto>.ValidationError($"Escrow already exists for task {dto.TaskId}");

        var escrow = TaskEscrow.Create(dto.TaskId, dto.FunderId, dto.TokenId, dto.Amount);
        if (dto.TxHashFund is not null)
            escrow.SetFundTxHash(dto.TxHashFund);

        await _repository.AddAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> ReleaseAsync(Guid id, ReleaseEscrowDto dto, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.Status != EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot release escrow in status {escrow.Status}");

        escrow.Release(dto.TxHash);
        await _repository.UpdateAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> DisputeAsync(Guid id, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.Status != EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot dispute escrow in status {escrow.Status}");

        escrow.Dispute();
        await _repository.UpdateAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> RefundAsync(Guid id, RefundEscrowDto dto, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.Status != EscrowStatus.FUNDED && escrow.Status != EscrowStatus.DISPUTED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot refund escrow in status {escrow.Status}");

        escrow.Refund(dto.TxHash);
        await _repository.UpdateAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }
}

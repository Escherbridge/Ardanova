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
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaskEscrowService(
        IRepository<TaskEscrow> repository,
        IRepository<ProjectTask> taskRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TaskEscrowDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> GetByTaskIdAsync(string taskId, CancellationToken ct = default)
    {
        var escrow = await _repository.FindOneAsync(e => e.taskId == taskId, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow for task {taskId} not found");
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<IReadOnlyList<TaskEscrowDto>>> GetByFunderIdAsync(string funderId, CancellationToken ct = default)
    {
        var escrows = await _repository.FindAsync(e => e.funderId == funderId, ct);
        return Result<IReadOnlyList<TaskEscrowDto>>.Success(_mapper.Map<IReadOnlyList<TaskEscrowDto>>(escrows));
    }

    public async Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(e => e.taskId == dto.TaskId, ct);
        if (exists)
            return Result<TaskEscrowDto>.ValidationError($"Escrow already exists for task {dto.TaskId}");

        var escrow = new TaskEscrow
        {
            id = Guid.NewGuid().ToString(),
            taskId = dto.TaskId,
            funderId = dto.FunderId,
            shareId = dto.ShareId,
            amount = dto.Amount,
            status = EscrowStatus.NONE,
            txHashFund = dto.TxHashFund,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(escrow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> ReleaseAsync(string id, ReleaseEscrowDto dto, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status != EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot release escrow in status {escrow.status}");

        var task = await GetTaskAsync(escrow, ct);
        if (task is null)
            return Result<TaskEscrowDto>.NotFound($"Task with id {escrow.taskId} not found for escrow {escrow.id}");

        escrow.status = EscrowStatus.RELEASED;
        escrow.txHashRelease = dto.TxHash;
        escrow.releasedAt = DateTime.UtcNow;
        task.escrowStatus = EscrowStatus.RELEASED;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escrow, ct);
        await _taskRepository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> DisputeAsync(string id, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status != EscrowStatus.FUNDED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot dispute escrow in status {escrow.status}");

        var task = await GetTaskAsync(escrow, ct);
        if (task is null)
            return Result<TaskEscrowDto>.NotFound($"Task with id {escrow.taskId} not found for escrow {escrow.id}");

        escrow.status = EscrowStatus.DISPUTED;
        task.escrowStatus = EscrowStatus.DISPUTED;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escrow, ct);
        await _taskRepository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    public async Task<Result<TaskEscrowDto>> RefundAsync(string id, RefundEscrowDto dto, CancellationToken ct = default)
    {
        var escrow = await _repository.GetByIdAsync(id, ct);
        if (escrow is null)
            return Result<TaskEscrowDto>.NotFound($"TaskEscrow with id {id} not found");

        if (escrow.status != EscrowStatus.FUNDED && escrow.status != EscrowStatus.DISPUTED)
            return Result<TaskEscrowDto>.ValidationError($"Cannot refund escrow in status {escrow.status}");

        var task = await GetTaskAsync(escrow, ct);
        if (task is null)
            return Result<TaskEscrowDto>.NotFound($"Task with id {escrow.taskId} not found for escrow {escrow.id}");

        escrow.status = EscrowStatus.REFUNDED;
        escrow.txHashRefund = dto.TxHash;
        escrow.refundedAt = DateTime.UtcNow;
        task.escrowStatus = EscrowStatus.REFUNDED;
        task.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escrow, ct);
        await _taskRepository.UpdateAsync(task, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<TaskEscrowDto>.Success(_mapper.Map<TaskEscrowDto>(escrow));
    }

    private Task<ProjectTask?> GetTaskAsync(TaskEscrow escrow, CancellationToken ct)
        => _taskRepository.GetByIdAsync(escrow.taskId, ct);
}

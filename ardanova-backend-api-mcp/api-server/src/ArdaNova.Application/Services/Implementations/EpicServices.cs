namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class EpicService : IEpicService
{
    private readonly IRepository<Epic> _repository;
    private readonly IRepository<RoadmapPhase> _phaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EpicService(IRepository<Epic> repository, IRepository<RoadmapPhase> phaseRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _phaseRepository = phaseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<EpicDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var epic = await _repository.GetByIdAsync(id, ct);
        if (epic is null)
            return Result<EpicDto>.NotFound($"Epic with id {id} not found");
        return Result<EpicDto>.Success(_mapper.Map<EpicDto>(epic));
    }

    public async Task<Result<IReadOnlyList<EpicDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var phases = await _phaseRepository.FindAsync(p => p.Roadmap != null && p.Roadmap.projectId == projectId, ct);
        var phaseIds = phases.Select(p => p.id).ToHashSet();
        var epics = await _repository.FindAsync(e => phaseIds.Contains(e.phaseId), ct);
        return Result<IReadOnlyList<EpicDto>>.Success(_mapper.Map<IReadOnlyList<EpicDto>>(epics));
    }

    public async Task<Result<IReadOnlyList<EpicDto>>> GetByPhaseIdAsync(string phaseId, CancellationToken ct = default)
    {
        var epics = await _repository.FindAsync(e => e.phaseId == phaseId, ct);
        return Result<IReadOnlyList<EpicDto>>.Success(_mapper.Map<IReadOnlyList<EpicDto>>(epics));
    }

    public async Task<Result<IReadOnlyList<EpicDto>>> GetByRoadmapIdAsync(string roadmapId, CancellationToken ct = default)
    {
        var phases = await _phaseRepository.FindAsync(p => p.roadmapId == roadmapId, ct);
        var phaseIds = phases.Select(p => p.id).ToHashSet();
        var epics = await _repository.FindAsync(e => phaseIds.Contains(e.phaseId), ct);
        return Result<IReadOnlyList<EpicDto>>.Success(_mapper.Map<IReadOnlyList<EpicDto>>(epics));
    }

    public async Task<Result<EpicDto>> CreateAsync(CreateEpicDto dto, CancellationToken ct = default)
    {
        var epic = new Epic
        {
            id = Guid.NewGuid().ToString(),
            phaseId = dto.PhaseId,
            title = dto.Title,
            description = dto.Description,
            status = EpicStatus.PLANNED,
            priority = (Priority)dto.Priority,
            tokenBudget = dto.TokenBudget,
            progress = 0,
            startDate = dto.StartDate,
            targetDate = dto.TargetDate,
            assigneeId = dto.AssigneeId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(epic, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<EpicDto>.Success(_mapper.Map<EpicDto>(epic));
    }

    public async Task<Result<EpicDto>> UpdateAsync(string id, UpdateEpicDto dto, CancellationToken ct = default)
    {
        var epic = await _repository.GetByIdAsync(id, ct);
        if (epic is null)
            return Result<EpicDto>.NotFound($"Epic with id {id} not found");

        if (dto.Title is not null) epic.title = dto.Title;
        if (dto.Description is not null) epic.description = dto.Description;
        if (dto.Status.HasValue) epic.status = dto.Status.Value;
        if (dto.Priority.HasValue) epic.priority = (Priority)dto.Priority.Value;
        if (dto.TokenBudget.HasValue) epic.tokenBudget = dto.TokenBudget;
        if (dto.Progress.HasValue) epic.progress = (int)dto.Progress.Value;
        if (dto.StartDate.HasValue) epic.startDate = dto.StartDate;
        if (dto.TargetDate.HasValue) epic.targetDate = dto.TargetDate;
        if (dto.AssigneeId is not null) epic.assigneeId = dto.AssigneeId;
        epic.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(epic, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<EpicDto>.Success(_mapper.Map<EpicDto>(epic));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var epic = await _repository.GetByIdAsync(id, ct);
        if (epic is null)
            return Result<bool>.NotFound($"Epic with id {id} not found");

        await _repository.DeleteAsync(epic, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<EpicDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var epic = await _repository.GetByIdAsync(id, ct);
        if (epic is null)
            return Result<EpicDto>.NotFound($"Epic with id {id} not found");

        epic.assigneeId = userId;
        epic.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(epic, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<EpicDto>.Success(_mapper.Map<EpicDto>(epic));
    }

    public async Task<Result<EpicDto>> UpdateStatusAsync(string id, EpicStatus status, CancellationToken ct = default)
    {
        var epic = await _repository.GetByIdAsync(id, ct);
        if (epic is null)
            return Result<EpicDto>.NotFound($"Epic with id {id} not found");

        epic.status = status;
        epic.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(epic, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<EpicDto>.Success(_mapper.Map<EpicDto>(epic));
    }

    public async Task<Result<EpicDto>> UpdatePriorityAsync(string id, TaskPriority priority, CancellationToken ct = default)
    {
        var epic = await _repository.GetByIdAsync(id, ct);
        if (epic is null)
            return Result<EpicDto>.NotFound($"Epic with id {id} not found");

        epic.priority = (Priority)priority;
        epic.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(epic, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<EpicDto>.Success(_mapper.Map<EpicDto>(epic));
    }

    public async Task<Result<bool>> ReorderAsync(string phaseId, IReadOnlyList<string> epicIds, CancellationToken ct = default)
    {
        // Epic doesn't have an order field currently, so this is a no-op
        // Would need to add an order field to Epic entity to support reordering
        return Result<bool>.Success(true);
    }
}

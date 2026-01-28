namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class SprintService : ISprintService
{
    private readonly IRepository<Sprint> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SprintService(IRepository<Sprint> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<SprintDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"Sprint with id {id} not found");
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<IReadOnlyList<SprintDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var sprints = await _repository.FindAsync(s => s.projectId == projectId, ct);
        return Result<IReadOnlyList<SprintDto>>.Success(_mapper.Map<IReadOnlyList<SprintDto>>(sprints));
    }

    public async Task<Result<SprintDto>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var sprint = await _repository.FindOneAsync(s => s.projectId == projectId && s.status == SprintStatus.ACTIVE, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"No active sprint found for project {projectId}");
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<SprintDto>> CreateAsync(CreateSprintDto dto, CancellationToken ct = default)
    {
        var sprint = new Sprint
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            name = dto.Name,
            goal = dto.Goal,
            startDate = dto.StartDate ?? DateTime.UtcNow,
            endDate = dto.EndDate ?? DateTime.UtcNow.AddDays(14),
            tokenBudget = dto.TokenBudget,
            assigneeId = dto.AssigneeId,
            status = SprintStatus.PLANNED,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<SprintDto>> UpdateAsync(string id, UpdateSprintDto dto, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"Sprint with id {id} not found");

        if (dto.Name is not null) sprint.name = dto.Name;
        if (dto.Goal is not null) sprint.goal = dto.Goal;
        if (dto.StartDate.HasValue) sprint.startDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) sprint.endDate = dto.EndDate.Value;
        if (dto.TokenBudget.HasValue) sprint.tokenBudget = dto.TokenBudget;
        if (dto.Velocity.HasValue) sprint.velocity = (int?)dto.Velocity;
        if (dto.Status.HasValue) sprint.status = dto.Status.Value;
        if (dto.AssigneeId is not null) sprint.assigneeId = dto.AssigneeId;
        sprint.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<bool>.NotFound($"Sprint with id {id} not found");

        await _repository.DeleteAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<SprintDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"Sprint with id {id} not found");

        sprint.assigneeId = userId;
        sprint.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<SprintDto>> UpdateStatusAsync(string id, SprintStatus status, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"Sprint with id {id} not found");

        sprint.status = status;
        sprint.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<SprintDto>> StartAsync(string id, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"Sprint with id {id} not found");

        sprint.status = SprintStatus.ACTIVE;
        sprint.startDate = DateTime.UtcNow;
        sprint.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }

    public async Task<Result<SprintDto>> CompleteAsync(string id, CancellationToken ct = default)
    {
        var sprint = await _repository.GetByIdAsync(id, ct);
        if (sprint is null)
            return Result<SprintDto>.NotFound($"Sprint with id {id} not found");

        sprint.status = SprintStatus.COMPLETED;
        sprint.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(sprint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(_mapper.Map<SprintDto>(sprint));
    }
}

public class SprintItemService : ISprintItemService
{
    private readonly IRepository<SprintItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SprintItemService(IRepository<SprintItem> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<SprintItemDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<SprintItemDto>.NotFound($"Sprint item with id {id} not found");
        return Result<SprintItemDto>.Success(_mapper.Map<SprintItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<SprintItemDto>>> GetBySprintIdAsync(string sprintId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.sprintId == sprintId, ct);
        var orderedItems = items.OrderBy(i => i.order).ToList();
        return Result<IReadOnlyList<SprintItemDto>>.Success(_mapper.Map<IReadOnlyList<SprintItemDto>>(orderedItems));
    }

    public async Task<Result<SprintItemDto>> AddTaskToSprintAsync(AddSprintItemDto dto, CancellationToken ct = default)
    {
        var item = new SprintItem
        {
            id = Guid.NewGuid().ToString(),
            sprintId = dto.SprintId,
            taskId = dto.TaskId,
            order = dto.Order,
            addedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SprintItemDto>.Success(_mapper.Map<SprintItemDto>(item));
    }

    public async Task<Result<bool>> RemoveTaskFromSprintAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<bool>.NotFound($"Sprint item with id {id} not found");

        await _repository.DeleteAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ReorderAsync(string sprintId, IReadOnlyList<string> itemIds, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.sprintId == sprintId, ct);
        var itemDict = items.ToDictionary(i => i.id);

        for (int i = 0; i < itemIds.Count; i++)
        {
            if (itemDict.TryGetValue(itemIds[i], out var item))
            {
                item.order = i;
                await _repository.UpdateAsync(item, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

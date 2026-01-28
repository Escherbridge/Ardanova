namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class ProductBacklogItemService : IProductBacklogItemService
{
    private readonly IRepository<ProductBacklogItem> _repository;
    private readonly IRepository<Epic> _epicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductBacklogItemService(IRepository<ProductBacklogItem> repository, IRepository<Epic> epicRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _epicRepository = epicRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProductBacklogItemDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ProductBacklogItemDto>.NotFound($"Product backlog item with id {id} not found");
        return Result<ProductBacklogItemDto>.Success(_mapper.Map<ProductBacklogItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<ProductBacklogItemDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        // Get all epics for the project's roadmap phases, then get all PBIs for those epics
        var epics = await _epicRepository.FindAsync(e => e.Phase != null && e.Phase.Roadmap != null && e.Phase.Roadmap.projectId == projectId, ct);
        var epicIds = epics.Select(e => e.id).ToHashSet();
        var items = await _repository.FindAsync(i => epicIds.Contains(i.epicId), ct);
        return Result<IReadOnlyList<ProductBacklogItemDto>>.Success(_mapper.Map<IReadOnlyList<ProductBacklogItemDto>>(items));
    }

    public async Task<Result<ProductBacklogItemDto>> CreateAsync(CreateProductBacklogItemDto dto, CancellationToken ct = default)
    {
        var item = new ProductBacklogItem
        {
            id = Guid.NewGuid().ToString(),
            epicId = dto.EpicId,
            title = dto.Title,
            description = dto.Description,
            type = dto.Type,
            storyPoints = dto.StoryPoints,
            status = PBIStatus.NEW,
            acceptanceCriteria = dto.AcceptanceCriteria,
            priority = (Priority)dto.Priority,
            assigneeId = dto.AssigneeId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductBacklogItemDto>.Success(_mapper.Map<ProductBacklogItemDto>(item));
    }

    public async Task<Result<ProductBacklogItemDto>> UpdateAsync(string id, UpdateProductBacklogItemDto dto, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ProductBacklogItemDto>.NotFound($"Product backlog item with id {id} not found");

        if (dto.Title is not null) item.title = dto.Title;
        if (dto.Description is not null) item.description = dto.Description;
        if (dto.Type.HasValue) item.type = dto.Type.Value;
        if (dto.StoryPoints.HasValue) item.storyPoints = dto.StoryPoints;
        if (dto.Status.HasValue) item.status = dto.Status.Value;
        if (dto.AcceptanceCriteria is not null) item.acceptanceCriteria = dto.AcceptanceCriteria;
        if (dto.Priority.HasValue) item.priority = (Priority)dto.Priority.Value;
        if (dto.AssigneeId is not null) item.assigneeId = dto.AssigneeId;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductBacklogItemDto>.Success(_mapper.Map<ProductBacklogItemDto>(item));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<bool>.NotFound($"Product backlog item with id {id} not found");

        await _repository.DeleteAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProductBacklogItemDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ProductBacklogItemDto>.NotFound($"Product backlog item with id {id} not found");

        item.assigneeId = userId;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductBacklogItemDto>.Success(_mapper.Map<ProductBacklogItemDto>(item));
    }

    public async Task<Result<ProductBacklogItemDto>> UpdateStatusAsync(string id, PBIStatus status, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ProductBacklogItemDto>.NotFound($"Product backlog item with id {id} not found");

        item.status = status;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductBacklogItemDto>.Success(_mapper.Map<ProductBacklogItemDto>(item));
    }

    public async Task<Result<bool>> ReorderAsync(string projectId, IReadOnlyList<string> itemIds, CancellationToken ct = default)
    {
        // Reordering would require an order field on ProductBacklogItem - currently not present
        // This is a no-op for now
        return Result<bool>.Success(true);
    }
}

public class BacklogItemService : IBacklogItemService
{
    private readonly IRepository<BacklogItem> _repository;
    private readonly IRepository<ProductBacklogItem> _pbiRepository;
    private readonly IRepository<SprintItem> _sprintItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BacklogItemService(
        IRepository<BacklogItem> repository,
        IRepository<ProductBacklogItem> pbiRepository,
        IRepository<SprintItem> sprintItemRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _pbiRepository = pbiRepository;
        _sprintItemRepository = sprintItemRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<BacklogItemDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<BacklogItemDto>.NotFound($"Backlog item with id {id} not found");
        return Result<BacklogItemDto>.Success(_mapper.Map<BacklogItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<BacklogItemDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        // Get all PBIs for the project, then get all backlog items for those PBIs
        var pbis = await _pbiRepository.FindAsync(p => p.Epic != null && p.Epic.Phase != null && p.Epic.Phase.Roadmap != null && p.Epic.Phase.Roadmap.projectId == projectId, ct);
        var pbiIds = pbis.Select(p => p.id).ToHashSet();
        var items = await _repository.FindAsync(i => pbiIds.Contains(i.pbiId), ct);
        return Result<IReadOnlyList<BacklogItemDto>>.Success(_mapper.Map<IReadOnlyList<BacklogItemDto>>(items));
    }

    public async Task<Result<IReadOnlyList<BacklogItemDto>>> GetBySprintIdAsync(string sprintId, CancellationToken ct = default)
    {
        // Get all sprint items for the sprint, then get the associated tasks
        var sprintItems = await _sprintItemRepository.FindAsync(si => si.sprintId == sprintId, ct);
        var taskIds = sprintItems.Select(si => si.taskId).ToHashSet();

        // BacklogItem has ProjectTasks collection - find backlog items that have tasks in this sprint
        var items = await _repository.FindAsync(bi => bi.ProjectTasks.Any(t => taskIds.Contains(t.id)), ct);
        return Result<IReadOnlyList<BacklogItemDto>>.Success(_mapper.Map<IReadOnlyList<BacklogItemDto>>(items));
    }

    public async Task<Result<BacklogItemDto>> CreateAsync(CreateBacklogItemDto dto, CancellationToken ct = default)
    {
        var item = new BacklogItem
        {
            id = Guid.NewGuid().ToString(),
            pbiId = dto.PbiId,
            title = dto.Title,
            description = dto.Description,
            type = dto.Type,
            status = BacklogStatus.NEW,
            estimate = dto.Estimate,
            assigneeId = dto.AssigneeId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BacklogItemDto>.Success(_mapper.Map<BacklogItemDto>(item));
    }

    public async Task<Result<BacklogItemDto>> UpdateAsync(string id, UpdateBacklogItemDto dto, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<BacklogItemDto>.NotFound($"Backlog item with id {id} not found");

        if (dto.Title is not null) item.title = dto.Title;
        if (dto.Description is not null) item.description = dto.Description;
        if (dto.Type.HasValue) item.type = dto.Type.Value;
        if (dto.Status.HasValue) item.status = dto.Status.Value;
        if (dto.Estimate.HasValue) item.estimate = dto.Estimate;
        if (dto.AssigneeId is not null) item.assigneeId = dto.AssigneeId;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BacklogItemDto>.Success(_mapper.Map<BacklogItemDto>(item));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<bool>.NotFound($"Backlog item with id {id} not found");

        await _repository.DeleteAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<BacklogItemDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<BacklogItemDto>.NotFound($"Backlog item with id {id} not found");

        item.assigneeId = userId;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BacklogItemDto>.Success(_mapper.Map<BacklogItemDto>(item));
    }

    public async Task<Result<BacklogItemDto>> UpdateStatusAsync(string id, BacklogStatus status, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<BacklogItemDto>.NotFound($"Backlog item with id {id} not found");

        item.status = status;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BacklogItemDto>.Success(_mapper.Map<BacklogItemDto>(item));
    }

    public async Task<Result<bool>> ReorderAsync(string projectId, IReadOnlyList<string> itemIds, CancellationToken ct = default)
    {
        // Reordering would require an order field on BacklogItem - currently not present
        // This is a no-op for now
        return Result<bool>.Success(true);
    }
}

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
    private readonly IRepository<Feature> _featureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductBacklogItemService(IRepository<ProductBacklogItem> repository, IRepository<Feature> featureRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _featureRepository = featureRepository;
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
        // Get all features that belong to sprints → epics → milestones for this project
        var features = await _featureRepository.FindAsync(f => f.Sprint != null && f.Sprint.Epic != null && f.Sprint.Epic.Milestone != null && f.Sprint.Epic.Milestone.projectId == projectId, ct);
        var featureIds = features.Select(f => f.id).ToHashSet();
        var items = await _repository.FindAsync(i => featureIds.Contains(i.featureId), ct);
        return Result<IReadOnlyList<ProductBacklogItemDto>>.Success(_mapper.Map<IReadOnlyList<ProductBacklogItemDto>>(items));
    }

    public async Task<Result<ProductBacklogItemDto>> CreateAsync(CreateProductBacklogItemDto dto, CancellationToken ct = default)
    {
        var item = new ProductBacklogItem
        {
            id = Guid.NewGuid().ToString(),
            featureId = dto.FeatureId,
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

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

    public async Task<Result<IReadOnlyList<ProductBacklogItemDto>>> GetByFeatureIdAsync(string featureId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.featureId == featureId, ct);
        return Result<IReadOnlyList<ProductBacklogItemDto>>.Success(_mapper.Map<IReadOnlyList<ProductBacklogItemDto>>(items));
    }

    public async Task<Result<IReadOnlyList<ProductBacklogItemDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.projectId == projectId, ct);
        return Result<IReadOnlyList<ProductBacklogItemDto>>.Success(_mapper.Map<IReadOnlyList<ProductBacklogItemDto>>(items));
    }

    public async Task<Result<ProductBacklogItemDto>> CreateAsync(CreateProductBacklogItemDto dto, CancellationToken ct = default)
    {
        var item = new ProductBacklogItem
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            featureId = dto.FeatureId,
            sprintId = dto.SprintId,
            epicId = dto.EpicId,
            milestoneId = dto.MilestoneId,
            guildId = dto.GuildId,
            title = dto.Title,
            description = dto.Description,
            type = dto.Type,
            storyPoints = dto.StoryPoints,
            status = PBIStatus.NEW,
            acceptanceCriteria = dto.AcceptanceCriteria,
            priority = dto.Priority,
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
        if (dto.Priority.HasValue) item.priority = dto.Priority.Value;
        if (dto.AssigneeId is not null) item.assigneeId = dto.AssigneeId;
        if (dto.GuildId is not null) item.guildId = dto.GuildId;
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

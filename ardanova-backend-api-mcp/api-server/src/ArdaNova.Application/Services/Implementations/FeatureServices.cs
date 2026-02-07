namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class FeatureService : IFeatureService
{
    private readonly IRepository<Feature> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FeatureService(IRepository<Feature> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<FeatureDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var feature = await _repository.GetByIdAsync(id, ct);
        if (feature is null)
            return Result<FeatureDto>.NotFound($"Feature with id {id} not found");
        return Result<FeatureDto>.Success(_mapper.Map<FeatureDto>(feature));
    }

    public async Task<Result<IReadOnlyList<FeatureDto>>> GetBySprintIdAsync(string sprintId, CancellationToken ct = default)
    {
        var features = await _repository.FindAsync(f => f.sprintId == sprintId, ct);
        return Result<IReadOnlyList<FeatureDto>>.Success(_mapper.Map<IReadOnlyList<FeatureDto>>(features));
    }

    public async Task<Result<FeatureDto>> CreateAsync(CreateFeatureDto dto, CancellationToken ct = default)
    {
        var feature = new Feature
        {
            id = Guid.NewGuid().ToString(),
            sprintId = dto.SprintId,
            title = dto.Title,
            description = dto.Description,
            status = FeatureStatus.PLANNED,
            priority = dto.Priority,
            order = dto.Order,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(feature, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<FeatureDto>.Success(_mapper.Map<FeatureDto>(feature));
    }

    public async Task<Result<FeatureDto>> UpdateAsync(string id, UpdateFeatureDto dto, CancellationToken ct = default)
    {
        var feature = await _repository.GetByIdAsync(id, ct);
        if (feature is null)
            return Result<FeatureDto>.NotFound($"Feature with id {id} not found");

        if (dto.Title is not null) feature.title = dto.Title;
        if (dto.Description is not null) feature.description = dto.Description;
        if (dto.Status.HasValue) feature.status = dto.Status.Value;
        if (dto.Priority.HasValue) feature.priority = dto.Priority.Value;
        if (dto.Order.HasValue) feature.order = dto.Order.Value;
        if (dto.AssigneeId is not null) feature.assigneeId = dto.AssigneeId;
        feature.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(feature, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<FeatureDto>.Success(_mapper.Map<FeatureDto>(feature));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var feature = await _repository.GetByIdAsync(id, ct);
        if (feature is null)
            return Result<bool>.NotFound($"Feature with id {id} not found");

        await _repository.DeleteAsync(feature, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<FeatureDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var feature = await _repository.GetByIdAsync(id, ct);
        if (feature is null)
            return Result<FeatureDto>.NotFound($"Feature with id {id} not found");

        feature.assigneeId = userId;
        feature.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(feature, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<FeatureDto>.Success(_mapper.Map<FeatureDto>(feature));
    }

    public async Task<Result<FeatureDto>> UpdateStatusAsync(string id, FeatureStatus status, CancellationToken ct = default)
    {
        var feature = await _repository.GetByIdAsync(id, ct);
        if (feature is null)
            return Result<FeatureDto>.NotFound($"Feature with id {id} not found");

        feature.status = status;
        feature.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(feature, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<FeatureDto>.Success(_mapper.Map<FeatureDto>(feature));
    }

    public async Task<Result<FeatureDto>> UpdatePriorityAsync(string id, Priority priority, CancellationToken ct = default)
    {
        var feature = await _repository.GetByIdAsync(id, ct);
        if (feature is null)
            return Result<FeatureDto>.NotFound($"Feature with id {id} not found");

        feature.priority = priority;
        feature.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(feature, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<FeatureDto>.Success(_mapper.Map<FeatureDto>(feature));
    }
}

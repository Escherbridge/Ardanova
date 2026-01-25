namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

public class ActivityService : IActivityService
{
    private readonly IRepository<Activity> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ActivityService(IRepository<Activity> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ActivityDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var activity = await _repository.GetByIdAsync(id, ct);
        if (activity is null)
            return Result<ActivityDto>.NotFound($"Activity with id {id} not found");
        return Result<ActivityDto>.Success(_mapper.Map<ActivityDto>(activity));
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var activities = await _repository.FindAsync(a => a.userId == userId, ct);
        var ordered = activities.OrderByDescending(a => a.createdAt).ToList();
        return Result<IReadOnlyList<ActivityDto>>.Success(_mapper.Map<IReadOnlyList<ActivityDto>>(ordered));
    }

    public async Task<Result<PagedResult<ActivityDto>>> GetByUserIdPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, a => a.userId == userId, ct);
        return Result<PagedResult<ActivityDto>>.Success(result.Map(_mapper.Map<ActivityDto>));
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var activities = await _repository.FindAsync(a => a.projectId == projectId, ct);
        var ordered = activities.OrderByDescending(a => a.createdAt).ToList();
        return Result<IReadOnlyList<ActivityDto>>.Success(_mapper.Map<IReadOnlyList<ActivityDto>>(ordered));
    }

    public async Task<Result<PagedResult<ActivityDto>>> GetByProjectIdPagedAsync(string projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, a => a.projectId == projectId, ct);
        return Result<PagedResult<ActivityDto>>.Success(result.Map(_mapper.Map<ActivityDto>));
    }

    public async Task<Result<ActivityDto>> CreateAsync(CreateActivityDto dto, CancellationToken ct = default)
    {
        var activity = new Activity
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            type = dto.Type,
            entityType = dto.EntityType,
            entityId = dto.EntityId,
            action = dto.Action,
            projectId = dto.ProjectId,
            metadata = dto.Metadata,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(activity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ActivityDto>.Success(_mapper.Map<ActivityDto>(activity));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var activity = await _repository.GetByIdAsync(id, ct);
        if (activity is null)
            return Result<bool>.NotFound($"Activity with id {id} not found");

        await _repository.DeleteAsync(activity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

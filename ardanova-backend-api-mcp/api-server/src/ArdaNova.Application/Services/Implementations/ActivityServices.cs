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

    public async Task<Result<ActivityDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var activity = await _repository.GetByIdAsync(id, ct);
        if (activity is null)
            return Result<ActivityDto>.NotFound($"Activity with id {id} not found");
        return Result<ActivityDto>.Success(_mapper.Map<ActivityDto>(activity));
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var activities = await _repository.FindAsync(a => a.UserId == userId, ct);
        var ordered = activities.OrderByDescending(a => a.CreatedAt).ToList();
        return Result<IReadOnlyList<ActivityDto>>.Success(_mapper.Map<IReadOnlyList<ActivityDto>>(ordered));
    }

    public async Task<Result<PagedResult<ActivityDto>>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, a => a.UserId == userId, ct);
        return Result<PagedResult<ActivityDto>>.Success(result.Map(_mapper.Map<ActivityDto>));
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var activities = await _repository.FindAsync(a => a.ProjectId == projectId, ct);
        var ordered = activities.OrderByDescending(a => a.CreatedAt).ToList();
        return Result<IReadOnlyList<ActivityDto>>.Success(_mapper.Map<IReadOnlyList<ActivityDto>>(ordered));
    }

    public async Task<Result<PagedResult<ActivityDto>>> GetByProjectIdPagedAsync(Guid projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, a => a.ProjectId == projectId, ct);
        return Result<PagedResult<ActivityDto>>.Success(result.Map(_mapper.Map<ActivityDto>));
    }

    public async Task<Result<ActivityDto>> CreateAsync(CreateActivityDto dto, CancellationToken ct = default)
    {
        var activity = Activity.Create(
            dto.UserId,
            dto.Type,
            dto.EntityType,
            dto.EntityId,
            dto.Action,
            dto.ProjectId,
            dto.Metadata
        );

        await _repository.AddAsync(activity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ActivityDto>.Success(_mapper.Map<ActivityDto>(activity));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var activity = await _repository.GetByIdAsync(id, ct);
        if (activity is null)
            return Result<bool>.NotFound($"Activity with id {id} not found");

        await _repository.DeleteAsync(activity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

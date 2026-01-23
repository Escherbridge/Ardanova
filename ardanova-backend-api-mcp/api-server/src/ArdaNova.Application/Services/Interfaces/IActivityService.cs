namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IActivityService
{
    Task<Result<ActivityDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ActivityDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<PagedResult<ActivityDto>>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ActivityDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<PagedResult<ActivityDto>>> GetByProjectIdPagedAsync(Guid projectId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ActivityDto>> CreateAsync(CreateActivityDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

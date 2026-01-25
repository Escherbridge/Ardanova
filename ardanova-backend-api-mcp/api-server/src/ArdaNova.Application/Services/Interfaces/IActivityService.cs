namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IActivityService
{
    Task<Result<ActivityDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ActivityDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<PagedResult<ActivityDto>>> GetByUserIdPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ActivityDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<PagedResult<ActivityDto>>> GetByProjectIdPagedAsync(string projectId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ActivityDto>> CreateAsync(CreateActivityDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

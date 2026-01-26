namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface ITaskService
{
    Task<Result<TaskDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<TaskDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<TaskDto>>> SearchAsync(
        string? searchTerm,
        TaskStatus? status,
        TaskPriority? priority,
        TaskType? taskType,
        string? projectId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateAsync(string id, UpdateTaskDto dto, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateStatusAsync(string id, TaskStatus status, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Enums;

public interface ITaskService
{
    Task<Result<TaskDto>> GetByIdAsync(string id, CancellationToken ct = default);
    /// <summary>Every task on a project this viewer may see; a DRAFT project's tasks are as private as the draft.</summary>
    Task<Result<IReadOnlyList<TaskDto>>> GetAllAsync(ProjectViewer viewer, CancellationToken ct = default);
    Task<Result<PagedResult<TaskDto>>> GetPagedAsync(ProjectViewer viewer, int page, int pageSize, CancellationToken ct = default);
    Task<Result<PagedResult<TaskDto>>> SearchAsync(
        ProjectViewer viewer,
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
    Task<Result<IReadOnlyList<TaskDto>>> GetByPbiIdAsync(string pbiId, CancellationToken ct = default);
    Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateAsync(string id, UpdateTaskDto dto, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateStatusAsync(string id, TaskStatus status, CancellationToken ct = default);
    /// <summary>Sets or clears the assignee; refused once an accepted bid or escrow owns who does the work.</summary>
    Task<Result<TaskDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

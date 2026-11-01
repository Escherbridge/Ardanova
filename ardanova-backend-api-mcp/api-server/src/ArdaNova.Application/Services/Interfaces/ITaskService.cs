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
    /// <param name="askableOnly">Only tasks nobody holds that are not COMPLETED: the ones a HELP_WANTED may name.</param>
    Task<Result<PagedResult<TaskDto>>> SearchAsync(
        ProjectViewer viewer,
        string? searchTerm,
        TaskStatus? status,
        TaskPriority? priority,
        TaskType? taskType,
        string? projectId,
        int page,
        int pageSize,
        bool askableOnly = false,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskDto>>> GetByPbiIdAsync(string pbiId, CancellationToken ct = default);
    Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateAsync(string id, UpdateTaskDto dto, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateStatusAsync(string id, TaskStatus status, CancellationToken ct = default);
    /// <summary>Sets or clears the assignee; refused once an accepted bid or escrow owns who does the work.</summary>
    /// <remarks>A manager's path: check-then-write. A member taking a task for themselves goes through <see cref="ClaimAsync"/>.</remarks>
    Task<Result<TaskDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);

    /// <summary>
    /// A member takes an open task for themselves through one conditional write; a 409 with a
    /// <c>TaskClaimCodes</c> code when it is taken, done or under agreement. Membership and paid-task
    /// readiness are the caller's checks, made before this. See Implementations/AGENTS.md §Task assignment.
    /// </summary>
    Task<Result<TaskDto>> ClaimAsync(string id, string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

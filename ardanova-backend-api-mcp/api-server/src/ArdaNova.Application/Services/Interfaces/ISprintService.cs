namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface ISprintService
{
    Task<Result<SprintDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SprintDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<SprintDto>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<SprintDto>> CreateAsync(CreateSprintDto dto, CancellationToken ct = default);
    Task<Result<SprintDto>> UpdateAsync(string id, UpdateSprintDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<SprintDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<SprintDto>> UpdateStatusAsync(string id, SprintStatus status, CancellationToken ct = default);
    Task<Result<SprintDto>> StartAsync(string id, CancellationToken ct = default);
    Task<Result<SprintDto>> CompleteAsync(string id, CancellationToken ct = default);
}

public interface ISprintItemService
{
    Task<Result<SprintItemDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SprintItemDto>>> GetBySprintIdAsync(string sprintId, CancellationToken ct = default);
    Task<Result<SprintItemDto>> AddTaskToSprintAsync(AddSprintItemDto dto, CancellationToken ct = default);
    Task<Result<bool>> RemoveTaskFromSprintAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> ReorderAsync(string sprintId, IReadOnlyList<string> itemIds, CancellationToken ct = default);
}

namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IEpicService
{
    Task<Result<EpicDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EpicDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EpicDto>>> GetByPhaseIdAsync(string phaseId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EpicDto>>> GetByRoadmapIdAsync(string roadmapId, CancellationToken ct = default);
    Task<Result<EpicDto>> CreateAsync(CreateEpicDto dto, CancellationToken ct = default);
    Task<Result<EpicDto>> UpdateAsync(string id, UpdateEpicDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<EpicDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<EpicDto>> UpdateStatusAsync(string id, EpicStatus status, CancellationToken ct = default);
    Task<Result<EpicDto>> UpdatePriorityAsync(string id, TaskPriority priority, CancellationToken ct = default);
    Task<Result<bool>> ReorderAsync(string phaseId, IReadOnlyList<string> epicIds, CancellationToken ct = default);
}

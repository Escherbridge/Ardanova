namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IRoadmapService
{
    Task<Result<RoadmapDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<RoadmapDto>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<RoadmapDto>> CreateAsync(CreateRoadmapDto dto, CancellationToken ct = default);
    Task<Result<RoadmapDto>> UpdateAsync(string id, UpdateRoadmapDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<RoadmapDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<RoadmapDto>> UpdateStatusAsync(string id, RoadmapStatus status, CancellationToken ct = default);
}

public interface IRoadmapPhaseService
{
    Task<Result<RoadmapPhaseDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RoadmapPhaseDto>>> GetByRoadmapIdAsync(string roadmapId, CancellationToken ct = default);
    Task<Result<RoadmapPhaseDto>> CreateAsync(CreateRoadmapPhaseDto dto, CancellationToken ct = default);
    Task<Result<RoadmapPhaseDto>> UpdateAsync(string id, UpdateRoadmapPhaseDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<RoadmapPhaseDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<RoadmapPhaseDto>> UpdateStatusAsync(string id, PhaseStatus status, CancellationToken ct = default);
    Task<Result<bool>> ReorderAsync(string roadmapId, IReadOnlyList<string> phaseIds, CancellationToken ct = default);
}

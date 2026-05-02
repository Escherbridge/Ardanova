namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IFeatureService
{
    Task<Result<FeatureDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FeatureDto>>> GetBySprintIdAsync(string sprintId, CancellationToken ct = default);
    Task<Result<FeatureDto>> CreateAsync(CreateFeatureDto dto, CancellationToken ct = default);
    Task<Result<FeatureDto>> UpdateAsync(string id, UpdateFeatureDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<FeatureDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<FeatureDto>> UpdateStatusAsync(string id, FeatureStatus status, CancellationToken ct = default);
    Task<Result<FeatureDto>> UpdatePriorityAsync(string id, Priority priority, CancellationToken ct = default);
}

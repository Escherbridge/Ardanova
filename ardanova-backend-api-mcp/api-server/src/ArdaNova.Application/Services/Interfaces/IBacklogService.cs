namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IProductBacklogItemService
{
    Task<Result<ProductBacklogItemDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProductBacklogItemDto>>> GetByFeatureIdAsync(string featureId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProductBacklogItemDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<ProductBacklogItemDto>> CreateAsync(CreateProductBacklogItemDto dto, CancellationToken ct = default);
    Task<Result<ProductBacklogItemDto>> UpdateAsync(string id, UpdateProductBacklogItemDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProductBacklogItemDto>> AssignAsync(string id, string? userId, CancellationToken ct = default);
    Task<Result<ProductBacklogItemDto>> UpdateStatusAsync(string id, PBIStatus status, CancellationToken ct = default);
    Task<Result<bool>> ReorderAsync(string projectId, IReadOnlyList<string> itemIds, CancellationToken ct = default);
}


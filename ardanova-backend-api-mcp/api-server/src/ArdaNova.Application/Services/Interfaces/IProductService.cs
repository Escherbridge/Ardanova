namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.DTOs;
using ArdaNova.Application.Common.Results;

public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProductDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<PagedResult<ProductDto>>> GetPagedByProjectIdAsync(string projectId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ProductDto>> GetBySkuAsync(string projectId, string sku, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(string id, UpdateProductDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ProductDto>> ToggleActiveAsync(string id, CancellationToken ct = default);
}

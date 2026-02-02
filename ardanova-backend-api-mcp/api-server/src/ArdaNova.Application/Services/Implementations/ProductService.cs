namespace ArdaNova.Application.Services.Implementations;

using AutoMapper;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;

public class ProductService : IProductService
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IRepository<Product> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with id {id} not found");
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var products = await _repository.FindAsync(p => p.projectId == projectId, ct);
        return Result<IReadOnlyList<ProductDto>>.Success(_mapper.Map<IReadOnlyList<ProductDto>>(products));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetPagedByProjectIdAsync(string projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, p => p.projectId == projectId, ct);
        return Result<PagedResult<ProductDto>>.Success(result.Map(_mapper.Map<ProductDto>));
    }

    public async Task<Result<ProductDto>> GetBySkuAsync(string projectId, string sku, CancellationToken ct = default)
    {
        var product = await _repository.FindOneAsync(p => p.projectId == projectId && p.sku == sku, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with SKU {sku} not found");
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            userId = dto.UserId,
            name = dto.Name,
            description = dto.Description,
            sku = dto.Sku,
            price = dto.Price,
            cost = dto.Cost,
            category = dto.Category,
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(string id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with id {id} not found");

        if (dto.Name is not null) product.name = dto.Name;
        if (dto.Description is not null) product.description = dto.Description;
        if (dto.Sku is not null) product.sku = dto.Sku;
        if (dto.Price.HasValue) product.price = dto.Price.Value;
        if (dto.Cost.HasValue) product.cost = dto.Cost;
        if (dto.Category is not null) product.category = dto.Category;
        if (dto.IsActive.HasValue) product.isActive = dto.IsActive.Value;
        product.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<bool>.NotFound($"Product with id {id} not found");

        await _repository.DeleteAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProductDto>> ToggleActiveAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with id {id} not found");

        product.isActive = !product.isActive;
        product.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }
}

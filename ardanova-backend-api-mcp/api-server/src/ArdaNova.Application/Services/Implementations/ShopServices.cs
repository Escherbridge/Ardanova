namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class ShopService : IShopService
{
    private readonly IRepository<Shop> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopService(IRepository<Shop> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task<Result<ShopDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var shop = await _repository.GetByIdAsync(id, ct);
        if (shop is null)
            return Result<ShopDto>.NotFound($"Shop with id {id} not found");
        return Result<ShopDto>.Success(_mapper.Map<ShopDto>(shop));
    }

    public async Task<Result<ShopDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var shop = await _repository.FindOneAsync(s => s.slug == slug, ct);
        if (shop is null)
            return Result<ShopDto>.NotFound($"Shop with slug {slug} not found");
        return Result<ShopDto>.Success(_mapper.Map<ShopDto>(shop));
    }

    public async Task<Result<IReadOnlyList<ShopDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var shops = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<ShopDto>>.Success(_mapper.Map<IReadOnlyList<ShopDto>>(shops));
    }

    public async Task<Result<PagedResult<ShopDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<ShopDto>>.Success(result.Map(_mapper.Map<ShopDto>));
    }

    public async Task<Result<PagedResult<ShopDto>>> SearchAsync(string? searchTerm, ShopCategory? category, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _repository.Query();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s => s.name.ToLower().Contains(term) ||
                (s.description != null && s.description.ToLower().Contains(term)));
        }

        if (category.HasValue)
            query = query.Where(s => s.category == category.Value);

        query = query.OrderByDescending(s => s.createdAt);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Result<PagedResult<ShopDto>>.Success(new PagedResult<ShopDto>(
            _mapper.Map<List<ShopDto>>(items),
            totalCount,
            page,
            pageSize));
    }

    public async Task<Result<IReadOnlyList<ShopDto>>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
    {
        var shops = await _repository.FindAsync(s => s.ownerId == ownerId, ct);
        return Result<IReadOnlyList<ShopDto>>.Success(_mapper.Map<IReadOnlyList<ShopDto>>(shops));
    }

    public async Task<Result<ShopDto>> CreateAsync(CreateShopDto dto, CancellationToken ct = default)
    {
        var shop = new Shop
        {
            id = Guid.NewGuid().ToString(),
            ownerId = dto.OwnerId,
            name = dto.Name,
            slug = GenerateSlug(dto.Name),
            description = dto.Description,
            category = dto.Category,
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(shop, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopDto>.Success(_mapper.Map<ShopDto>(shop));
    }

    public async Task<Result<ShopDto>> UpdateAsync(string id, UpdateShopDto dto, CancellationToken ct = default)
    {
        var shop = await _repository.GetByIdAsync(id, ct);
        if (shop is null)
            return Result<ShopDto>.NotFound($"Shop with id {id} not found");

        if (dto.Name is not null) shop.name = dto.Name;
        if (dto.Description is not null) shop.description = dto.Description;
        if (dto.Address is not null) shop.address = dto.Address;
        if (dto.Phone is not null) shop.phone = dto.Phone;
        if (dto.Email is not null) shop.email = dto.Email;
        if (dto.Website is not null) shop.website = dto.Website;
        if (dto.Logo is not null) shop.logo = dto.Logo;
        shop.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(shop, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopDto>.Success(_mapper.Map<ShopDto>(shop));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var shop = await _repository.GetByIdAsync(id, ct);
        if (shop is null)
            return Result<bool>.NotFound($"Shop with id {id} not found");

        await _repository.DeleteAsync(shop, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ShopDto>> UpgradePlanAsync(string id, SubscriptionPlan plan, CancellationToken ct = default)
    {
        var shop = await _repository.GetByIdAsync(id, ct);
        if (shop is null)
            return Result<ShopDto>.NotFound($"Shop with id {id} not found");

        return Result<ShopDto>.Success(_mapper.Map<ShopDto>(shop));
    }

    public async Task<Result<ShopDto>> ToggleActiveAsync(string id, CancellationToken ct = default)
    {
        var shop = await _repository.GetByIdAsync(id, ct);
        if (shop is null)
            return Result<ShopDto>.NotFound($"Shop with id {id} not found");

        shop.isActive = !shop.isActive;
        shop.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(shop, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopDto>.Success(_mapper.Map<ShopDto>(shop));
    }
}

public class ShopCustomerService : IShopCustomerService
{
    private readonly IRepository<User> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopCustomerService(IRepository<User> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopCustomerDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<ShopCustomerDto>.NotFound($"Customer with id {id} not found");
        return Result<ShopCustomerDto>.Success(_mapper.Map<ShopCustomerDto>(user));
    }

    public async Task<Result<IReadOnlyList<ShopCustomerDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var users = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<ShopCustomerDto>>.Success(_mapper.Map<IReadOnlyList<ShopCustomerDto>>(users.Take(0).ToList()));
    }

    public async Task<Result<PagedResult<ShopCustomerDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<ShopCustomerDto>>.Success(result.Map(_mapper.Map<ShopCustomerDto>));
    }

    public async Task<Result<ShopCustomerDto>> CreateAsync(CreateShopCustomerDto dto, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(dto.UserId, ct);
        if (user is null)
            return Result<ShopCustomerDto>.NotFound($"User with id {dto.UserId} not found");
        return Result<ShopCustomerDto>.Success(_mapper.Map<ShopCustomerDto>(user));
    }

    public async Task<Result<ShopCustomerDto>> UpdateAsync(string id, UpdateShopCustomerDto dto, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<ShopCustomerDto>.NotFound($"Customer with id {id} not found");

        if (dto.Name is not null) user.name = dto.Name;
        if (dto.Phone is not null) user.phone = dto.Phone;
        user.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopCustomerDto>.Success(_mapper.Map<ShopCustomerDto>(user));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        return Result<bool>.Success(true);
    }
}

public class ShopProductService : IShopProductService
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopProductService(IRepository<Product> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopProductDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ShopProductDto>.NotFound($"Product with id {id} not found");
        return Result<ShopProductDto>.Success(_mapper.Map<ShopProductDto>(product));
    }

    public async Task<Result<IReadOnlyList<ShopProductDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var products = await _repository.FindAsync(p => p.shopId == shopId, ct);
        return Result<IReadOnlyList<ShopProductDto>>.Success(_mapper.Map<IReadOnlyList<ShopProductDto>>(products));
    }

    public async Task<Result<PagedResult<ShopProductDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, p => p.shopId == shopId, ct);
        return Result<PagedResult<ShopProductDto>>.Success(result.Map(_mapper.Map<ShopProductDto>));
    }

    public async Task<Result<ShopProductDto>> GetBySkuAsync(string shopId, string sku, CancellationToken ct = default)
    {
        var product = await _repository.FindOneAsync(p => p.shopId == shopId && p.sku == sku, ct);
        if (product is null)
            return Result<ShopProductDto>.NotFound($"Product with SKU {sku} not found");
        return Result<ShopProductDto>.Success(_mapper.Map<ShopProductDto>(product));
    }

    public async Task<Result<ShopProductDto>> CreateAsync(CreateShopProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            id = Guid.NewGuid().ToString(),
            shopId = dto.ShopId,
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
        return Result<ShopProductDto>.Success(_mapper.Map<ShopProductDto>(product));
    }

    public async Task<Result<ShopProductDto>> UpdateAsync(string id, UpdateShopProductDto dto, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ShopProductDto>.NotFound($"Product with id {id} not found");

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
        return Result<ShopProductDto>.Success(_mapper.Map<ShopProductDto>(product));
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

    public async Task<Result<ShopProductDto>> ToggleActiveAsync(string id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ShopProductDto>.NotFound($"Product with id {id} not found");

        product.isActive = !product.isActive;
        product.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopProductDto>.Success(_mapper.Map<ShopProductDto>(product));
    }
}

public class ShopAnalyticsService : IShopAnalyticsService
{
    private readonly IRepository<ShopAnalytics> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopAnalyticsService(IRepository<ShopAnalytics> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopAnalyticsDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var analytics = await _repository.GetByIdAsync(id, ct);
        if (analytics is null)
            return Result<ShopAnalyticsDto>.NotFound($"Analytics with id {id} not found");
        return Result<ShopAnalyticsDto>.Success(_mapper.Map<ShopAnalyticsDto>(analytics));
    }

    public async Task<Result<IReadOnlyList<ShopAnalyticsDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var analytics = await _repository.FindAsync(a => a.shopId == shopId, ct);
        return Result<IReadOnlyList<ShopAnalyticsDto>>.Success(_mapper.Map<IReadOnlyList<ShopAnalyticsDto>>(analytics));
    }

    public async Task<Result<IReadOnlyList<ShopAnalyticsDto>>> GetByDateRangeAsync(string shopId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var analytics = await _repository.FindAsync(a => a.shopId == shopId && a.date >= startDate.Date && a.date <= endDate.Date, ct);
        return Result<IReadOnlyList<ShopAnalyticsDto>>.Success(_mapper.Map<IReadOnlyList<ShopAnalyticsDto>>(analytics));
    }

    public async Task<Result<ShopAnalyticsDto>> GetByDateAsync(string shopId, DateTime date, CancellationToken ct = default)
    {
        var analytics = await _repository.FindOneAsync(a => a.shopId == shopId && a.date == date.Date, ct);
        if (analytics is null)
            return Result<ShopAnalyticsDto>.NotFound($"Analytics for date {date:yyyy-MM-dd} not found");
        return Result<ShopAnalyticsDto>.Success(_mapper.Map<ShopAnalyticsDto>(analytics));
    }

    public async Task<Result<ShopAnalyticsDto>> CreateAsync(CreateShopAnalyticsDto dto, CancellationToken ct = default)
    {
        var analytics = new ShopAnalytics
        {
            id = Guid.NewGuid().ToString(),
            shopId = dto.ShopId,
            date = dto.Date.Date,
            revenue = dto.Revenue,
            expenses = dto.Expenses,
            profit = dto.Revenue - dto.Expenses,
            salesCount = dto.SalesCount,
            newCustomers = dto.NewCustomers,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(analytics, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopAnalyticsDto>.Success(_mapper.Map<ShopAnalyticsDto>(analytics));
    }

    public async Task<Result<ShopAnalyticsDto>> UpdateAsync(string id, UpdateShopAnalyticsDto dto, CancellationToken ct = default)
    {
        var analytics = await _repository.GetByIdAsync(id, ct);
        if (analytics is null)
            return Result<ShopAnalyticsDto>.NotFound($"Analytics with id {id} not found");

        if (dto.Revenue.HasValue) analytics.revenue = dto.Revenue.Value;
        if (dto.Expenses.HasValue) analytics.expenses = dto.Expenses.Value;
        if (dto.SalesCount.HasValue) analytics.salesCount = dto.SalesCount.Value;
        if (dto.NewCustomers.HasValue) analytics.newCustomers = dto.NewCustomers.Value;
        analytics.profit = analytics.revenue - analytics.expenses;

        await _repository.UpdateAsync(analytics, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopAnalyticsDto>.Success(_mapper.Map<ShopAnalyticsDto>(analytics));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var analytics = await _repository.GetByIdAsync(id, ct);
        if (analytics is null)
            return Result<bool>.NotFound($"Analytics with id {id} not found");

        await _repository.DeleteAsync(analytics, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

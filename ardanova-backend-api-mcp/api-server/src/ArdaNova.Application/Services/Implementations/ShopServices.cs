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

public class ShopInvoiceService : IShopInvoiceService
{
    private readonly IRepository<Invoice> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopInvoiceService(IRepository<Invoice> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopInvoiceDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<ShopInvoiceDto>.NotFound($"Invoice with id {id} not found");
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }

    public async Task<Result<IReadOnlyList<ShopInvoiceDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var invoices = await _repository.FindAsync(i => i.shopId == shopId, ct);
        return Result<IReadOnlyList<ShopInvoiceDto>>.Success(_mapper.Map<IReadOnlyList<ShopInvoiceDto>>(invoices));
    }

    public async Task<Result<IReadOnlyList<ShopInvoiceDto>>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        var invoices = await _repository.FindAsync(i => i.buyerId == customerId, ct);
        return Result<IReadOnlyList<ShopInvoiceDto>>.Success(_mapper.Map<IReadOnlyList<ShopInvoiceDto>>(invoices));
    }

    public async Task<Result<PagedResult<ShopInvoiceDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, i => i.shopId == shopId, ct);
        return Result<PagedResult<ShopInvoiceDto>>.Success(result.Map(_mapper.Map<ShopInvoiceDto>));
    }

    public async Task<Result<ShopInvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        var invoice = await _repository.FindOneAsync(i => i.invoiceNumber == invoiceNumber, ct);
        if (invoice is null)
            return Result<ShopInvoiceDto>.NotFound($"Invoice with number {invoiceNumber} not found");
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }

    public async Task<Result<ShopInvoiceDto>> CreateAsync(CreateShopInvoiceDto dto, CancellationToken ct = default)
    {
        var total = dto.Amount + (dto.Tax ?? 0) - (dto.Discount ?? 0);
        var invoice = new Invoice
        {
            id = Guid.NewGuid().ToString(),
            shopId = dto.ShopId,
            buyerId = dto.CustomerId,
            userId = dto.UserId,
            invoiceNumber = dto.InvoiceNumber,
            amount = dto.Amount,
            tax = dto.Tax,
            discount = dto.Discount,
            total = total,
            status = InvoiceStatus.DRAFT,
            dueDate = dto.DueDate,
            notes = dto.Notes,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }

    public async Task<Result<ShopInvoiceDto>> UpdateAsync(string id, UpdateShopInvoiceDto dto, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<ShopInvoiceDto>.NotFound($"Invoice with id {id} not found");

        if (dto.Amount.HasValue) invoice.amount = dto.Amount.Value;
        if (dto.Tax.HasValue) invoice.tax = dto.Tax;
        if (dto.Discount.HasValue) invoice.discount = dto.Discount;
        if (dto.DueDate.HasValue) invoice.dueDate = dto.DueDate.Value;
        if (dto.Notes is not null) invoice.notes = dto.Notes;
        invoice.total = invoice.amount + (invoice.tax ?? 0) - (invoice.discount ?? 0);
        invoice.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<bool>.NotFound($"Invoice with id {id} not found");

        await _repository.DeleteAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ShopInvoiceDto>> SendAsync(string id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<ShopInvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.status = InvoiceStatus.SENT;
        invoice.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }

    public async Task<Result<ShopInvoiceDto>> MarkPaidAsync(string id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<ShopInvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.status = InvoiceStatus.PAID;
        invoice.paidAt = DateTime.UtcNow;
        invoice.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }

    public async Task<Result<ShopInvoiceDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<ShopInvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.status = InvoiceStatus.CANCELLED;
        invoice.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInvoiceDto>.Success(_mapper.Map<ShopInvoiceDto>(invoice));
    }
}

public class ShopSaleService : IShopSaleService
{
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SaleItem> _saleItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopSaleService(IRepository<Sale> saleRepository, IRepository<SaleItem> saleItemRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _saleItemRepository = saleItemRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopSaleDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, ct);
        if (sale is null)
            return Result<ShopSaleDto>.NotFound($"Sale with id {id} not found");
        return Result<ShopSaleDto>.Success(_mapper.Map<ShopSaleDto>(sale));
    }

    public async Task<Result<IReadOnlyList<ShopSaleDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var sales = await _saleRepository.FindAsync(s => s.shopId == shopId, ct);
        return Result<IReadOnlyList<ShopSaleDto>>.Success(_mapper.Map<IReadOnlyList<ShopSaleDto>>(sales));
    }

    public async Task<Result<PagedResult<ShopSaleDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _saleRepository.GetPagedAsync(page, pageSize, s => s.shopId == shopId, ct);
        return Result<PagedResult<ShopSaleDto>>.Success(result.Map(_mapper.Map<ShopSaleDto>));
    }

    public async Task<Result<IReadOnlyList<ShopSaleDto>>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        var sales = await _saleRepository.FindAsync(s => s.buyerId == customerId, ct);
        return Result<IReadOnlyList<ShopSaleDto>>.Success(_mapper.Map<IReadOnlyList<ShopSaleDto>>(sales));
    }

    public async Task<Result<ShopSaleDto>> CreateAsync(CreateShopSaleDto dto, CancellationToken ct = default)
    {
        var sale = new Sale
        {
            id = Guid.NewGuid().ToString(),
            shopId = dto.ShopId,
            buyerId = dto.CustomerId,
            userId = dto.UserId,
            total = dto.Total,
            tax = dto.Tax,
            discount = dto.Discount,
            paymentMethod = dto.PaymentMethod,
            notes = dto.Notes,
            createdAt = DateTime.UtcNow
        };

        await _saleRepository.AddAsync(sale, ct);

        if (dto.Items is not null)
        {
            foreach (var item in dto.Items)
            {
                var saleItem = new SaleItem
                {
                    id = Guid.NewGuid().ToString(),
                    saleId = sale.id,
                    productId = item.ProductId,
                    quantity = item.Quantity,
                    price = item.Price,
                    total = item.Quantity * item.Price
                };
                await _saleItemRepository.AddAsync(saleItem, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopSaleDto>.Success(_mapper.Map<ShopSaleDto>(sale));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, ct);
        if (sale is null)
            return Result<bool>.NotFound($"Sale with id {id} not found");

        await _saleRepository.DeleteAsync(sale, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class ShopSaleItemService : IShopSaleItemService
{
    private readonly IRepository<SaleItem> _repository;
    private readonly IMapper _mapper;

    public ShopSaleItemService(IRepository<SaleItem> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<ShopSaleItemDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ShopSaleItemDto>.NotFound($"Sale item with id {id} not found");
        return Result<ShopSaleItemDto>.Success(_mapper.Map<ShopSaleItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<ShopSaleItemDto>>> GetBySaleIdAsync(string saleId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.saleId == saleId, ct);
        return Result<IReadOnlyList<ShopSaleItemDto>>.Success(_mapper.Map<IReadOnlyList<ShopSaleItemDto>>(items));
    }
}

public class ShopInventoryItemService : IShopInventoryItemService
{
    private readonly IRepository<InventoryItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopInventoryItemService(IRepository<InventoryItem> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopInventoryItemDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ShopInventoryItemDto>.NotFound($"Inventory item with id {id} not found");
        return Result<ShopInventoryItemDto>.Success(_mapper.Map<ShopInventoryItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<ShopInventoryItemDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.shopId == shopId, ct);
        return Result<IReadOnlyList<ShopInventoryItemDto>>.Success(_mapper.Map<IReadOnlyList<ShopInventoryItemDto>>(items));
    }

    public async Task<Result<ShopInventoryItemDto>> GetByProductIdAsync(string productId, CancellationToken ct = default)
    {
        var item = await _repository.FindOneAsync(i => i.productId == productId, ct);
        if (item is null)
            return Result<ShopInventoryItemDto>.NotFound($"Inventory item for product {productId} not found");
        return Result<ShopInventoryItemDto>.Success(_mapper.Map<ShopInventoryItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<ShopInventoryItemDto>>> GetLowStockAsync(string shopId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.shopId == shopId && i.currentStock <= i.minStock, ct);
        return Result<IReadOnlyList<ShopInventoryItemDto>>.Success(_mapper.Map<IReadOnlyList<ShopInventoryItemDto>>(items));
    }

    public async Task<Result<ShopInventoryItemDto>> CreateAsync(CreateShopInventoryItemDto dto, CancellationToken ct = default)
    {
        var item = new InventoryItem
        {
            id = Guid.NewGuid().ToString(),
            shopId = dto.ShopId,
            productId = dto.ProductId,
            userId = dto.UserId,
            currentStock = dto.CurrentStock,
            minStock = dto.MinStock,
            maxStock = dto.MaxStock,
            reorderPoint = dto.ReorderPoint,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInventoryItemDto>.Success(_mapper.Map<ShopInventoryItemDto>(item));
    }

    public async Task<Result<ShopInventoryItemDto>> UpdateAsync(string id, UpdateShopInventoryItemDto dto, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ShopInventoryItemDto>.NotFound($"Inventory item with id {id} not found");

        if (dto.CurrentStock.HasValue) item.currentStock = dto.CurrentStock.Value;
        if (dto.MinStock.HasValue) item.minStock = dto.MinStock.Value;
        if (dto.MaxStock.HasValue) item.maxStock = dto.MaxStock;
        if (dto.ReorderPoint.HasValue) item.reorderPoint = dto.ReorderPoint;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInventoryItemDto>.Success(_mapper.Map<ShopInventoryItemDto>(item));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<bool>.NotFound($"Inventory item with id {id} not found");

        await _repository.DeleteAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ShopInventoryItemDto>> AddStockAsync(string id, int quantity, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ShopInventoryItemDto>.NotFound($"Inventory item with id {id} not found");

        item.currentStock += quantity;
        item.lastRestocked = DateTime.UtcNow;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInventoryItemDto>.Success(_mapper.Map<ShopInventoryItemDto>(item));
    }

    public async Task<Result<ShopInventoryItemDto>> RemoveStockAsync(string id, int quantity, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<ShopInventoryItemDto>.NotFound($"Inventory item with id {id} not found");

        if (item.currentStock < quantity)
            return Result<ShopInventoryItemDto>.Failure("Insufficient stock");

        item.currentStock -= quantity;
        item.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopInventoryItemDto>.Success(_mapper.Map<ShopInventoryItemDto>(item));
    }
}

public class ShopMarketingCampaignService : IShopMarketingCampaignService
{
    private readonly IRepository<MarketingCampaign> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShopMarketingCampaignService(IRepository<MarketingCampaign> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ShopMarketingCampaignDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<ShopMarketingCampaignDto>.NotFound($"Campaign with id {id} not found");
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }

    public async Task<Result<IReadOnlyList<ShopMarketingCampaignDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default)
    {
        var campaigns = await _repository.FindAsync(c => c.shopId == shopId, ct);
        return Result<IReadOnlyList<ShopMarketingCampaignDto>>.Success(_mapper.Map<IReadOnlyList<ShopMarketingCampaignDto>>(campaigns));
    }

    public async Task<Result<PagedResult<ShopMarketingCampaignDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, c => c.shopId == shopId, ct);
        return Result<PagedResult<ShopMarketingCampaignDto>>.Success(result.Map(_mapper.Map<ShopMarketingCampaignDto>));
    }

    public async Task<Result<ShopMarketingCampaignDto>> CreateAsync(CreateShopMarketingCampaignDto dto, CancellationToken ct = default)
    {
        var campaign = new MarketingCampaign
        {
            id = Guid.NewGuid().ToString(),
            shopId = dto.ShopId,
            userId = dto.UserId,
            name = dto.Name,
            description = dto.Description,
            platform = dto.Platform,
            content = dto.Content,
            mediaUrls = dto.MediaUrls,
            scheduledAt = dto.ScheduledAt,
            status = CampaignStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }

    public async Task<Result<ShopMarketingCampaignDto>> UpdateAsync(string id, UpdateShopMarketingCampaignDto dto, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<ShopMarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        if (dto.Name is not null) campaign.name = dto.Name;
        if (dto.Description is not null) campaign.description = dto.Description;
        if (dto.Platform is not null) campaign.platform = dto.Platform;
        if (dto.Content is not null) campaign.content = dto.Content;
        if (dto.MediaUrls is not null) campaign.mediaUrls = dto.MediaUrls;
        if (dto.ScheduledAt.HasValue) campaign.scheduledAt = dto.ScheduledAt;
        campaign.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<bool>.NotFound($"Campaign with id {id} not found");

        await _repository.DeleteAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ShopMarketingCampaignDto>> ScheduleAsync(string id, DateTime scheduledAt, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<ShopMarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.scheduledAt = scheduledAt;
        campaign.status = CampaignStatus.SCHEDULED;
        campaign.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }

    public async Task<Result<ShopMarketingCampaignDto>> ActivateAsync(string id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<ShopMarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.status = CampaignStatus.ACTIVE;
        campaign.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }

    public async Task<Result<ShopMarketingCampaignDto>> CompleteAsync(string id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<ShopMarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.status = CampaignStatus.COMPLETED;
        campaign.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }

    public async Task<Result<ShopMarketingCampaignDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<ShopMarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.status = CampaignStatus.CANCELLED;
        campaign.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ShopMarketingCampaignDto>.Success(_mapper.Map<ShopMarketingCampaignDto>(campaign));
    }
}

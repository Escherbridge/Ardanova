namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class BusinessService : IBusinessService
{
    private readonly IRepository<Business> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BusinessService(IRepository<Business> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<BusinessDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var business = await _repository.GetByIdAsync(id, ct);
        if (business is null)
            return Result<BusinessDto>.NotFound($"Business with id {id} not found");
        return Result<BusinessDto>.Success(_mapper.Map<BusinessDto>(business));
    }

    public async Task<Result<IReadOnlyList<BusinessDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var businesses = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<BusinessDto>>.Success(_mapper.Map<IReadOnlyList<BusinessDto>>(businesses));
    }

    public async Task<Result<PagedResult<BusinessDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<BusinessDto>>.Success(result.Map(_mapper.Map<BusinessDto>));
    }

    public async Task<Result<IReadOnlyList<BusinessDto>>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
    {
        var businesses = await _repository.FindAsync(b => b.OwnerId == ownerId, ct);
        return Result<IReadOnlyList<BusinessDto>>.Success(_mapper.Map<IReadOnlyList<BusinessDto>>(businesses));
    }

    public async Task<Result<BusinessDto>> CreateAsync(CreateBusinessDto dto, CancellationToken ct = default)
    {
        var business = Business.Create(dto.OwnerId, dto.Name, dto.Description, dto.Industry);
        await _repository.AddAsync(business, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BusinessDto>.Success(_mapper.Map<BusinessDto>(business));
    }

    public async Task<Result<BusinessDto>> UpdateAsync(Guid id, UpdateBusinessDto dto, CancellationToken ct = default)
    {
        var business = await _repository.GetByIdAsync(id, ct);
        if (business is null)
            return Result<BusinessDto>.NotFound($"Business with id {id} not found");

        business.Update(
            dto.Name ?? business.Name,
            dto.Description ?? business.Description,
            dto.Industry ?? business.Industry,
            dto.Address ?? business.Address,
            dto.Phone ?? business.Phone,
            dto.Email ?? business.Email,
            dto.Website ?? business.Website
        );

        if (dto.Logo is not null) business.SetLogo(dto.Logo);
        if (dto.Plan.HasValue) business.UpgradePlan(dto.Plan.Value);

        await _repository.UpdateAsync(business, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BusinessDto>.Success(_mapper.Map<BusinessDto>(business));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var business = await _repository.GetByIdAsync(id, ct);
        if (business is null)
            return Result<bool>.NotFound($"Business with id {id} not found");

        await _repository.DeleteAsync(business, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<BusinessDto>> UpgradePlanAsync(Guid id, SubscriptionPlan plan, CancellationToken ct = default)
    {
        var business = await _repository.GetByIdAsync(id, ct);
        if (business is null)
            return Result<BusinessDto>.NotFound($"Business with id {id} not found");

        business.UpgradePlan(plan);
        await _repository.UpdateAsync(business, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BusinessDto>.Success(_mapper.Map<BusinessDto>(business));
    }

    public async Task<Result<BusinessDto>> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var business = await _repository.GetByIdAsync(id, ct);
        if (business is null)
            return Result<BusinessDto>.NotFound($"Business with id {id} not found");

        if (business.IsActive) business.Deactivate();
        else business.Activate();

        await _repository.UpdateAsync(business, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BusinessDto>.Success(_mapper.Map<BusinessDto>(business));
    }
}

public class CustomerService : ICustomerService
{
    private readonly IRepository<Customer> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CustomerService(IRepository<Customer> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer is null)
            return Result<CustomerDto>.NotFound($"Customer with id {id} not found");
        return Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var customers = await _repository.FindAsync(c => c.BusinessId == businessId, ct);
        return Result<IReadOnlyList<CustomerDto>>.Success(_mapper.Map<IReadOnlyList<CustomerDto>>(customers));
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, c => c.BusinessId == businessId, ct);
        return Result<PagedResult<CustomerDto>>.Success(result.Map(_mapper.Map<CustomerDto>));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
    {
        var customer = Customer.Create(dto.BusinessId, dto.UserId, dto.Name, dto.Email, dto.Phone, dto.Address);
        await _repository.AddAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer is null)
            return Result<CustomerDto>.NotFound($"Customer with id {id} not found");

        customer.Update(
            dto.Name ?? customer.Name,
            dto.Email ?? customer.Email,
            dto.Phone ?? customer.Phone,
            dto.Address ?? customer.Address
        );

        await _repository.UpdateAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<CustomerDto>.Success(_mapper.Map<CustomerDto>(customer));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer is null)
            return Result<bool>.NotFound($"Customer with id {id} not found");

        await _repository.DeleteAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

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

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with id {id} not found");
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var products = await _repository.FindAsync(p => p.BusinessId == businessId, ct);
        return Result<IReadOnlyList<ProductDto>>.Success(_mapper.Map<IReadOnlyList<ProductDto>>(products));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, p => p.BusinessId == businessId, ct);
        return Result<PagedResult<ProductDto>>.Success(result.Map(_mapper.Map<ProductDto>));
    }

    public async Task<Result<ProductDto>> GetBySkuAsync(Guid businessId, string sku, CancellationToken ct = default)
    {
        var product = await _repository.FindOneAsync(p => p.BusinessId == businessId && p.Sku == sku, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with SKU {sku} not found");
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = Product.Create(dto.BusinessId, dto.UserId, dto.Name, dto.Price, dto.Description, dto.Sku, dto.Cost, dto.Category);
        await _repository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with id {id} not found");

        product.Update(
            dto.Name ?? product.Name,
            dto.Description ?? product.Description,
            dto.Sku ?? product.Sku,
            dto.Price ?? product.Price,
            dto.Cost ?? product.Cost,
            dto.Category ?? product.Category
        );

        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value) product.Activate();
            else product.Deactivate();
        }

        await _repository.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<bool>.NotFound($"Product with id {id} not found");

        await _repository.DeleteAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<ProductDto>> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound($"Product with id {id} not found");

        if (product.IsActive) product.Deactivate();
        else product.Activate();

        await _repository.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ProductDto>.Success(_mapper.Map<ProductDto>(product));
    }
}

public class InvoiceService : IInvoiceService
{
    private readonly IRepository<Invoice> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InvoiceService(IRepository<Invoice> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<InvoiceDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<InvoiceDto>.NotFound($"Invoice with id {id} not found");
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<IReadOnlyList<InvoiceDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var invoices = await _repository.FindAsync(i => i.BusinessId == businessId, ct);
        return Result<IReadOnlyList<InvoiceDto>>.Success(_mapper.Map<IReadOnlyList<InvoiceDto>>(invoices));
    }

    public async Task<Result<IReadOnlyList<InvoiceDto>>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var invoices = await _repository.FindAsync(i => i.CustomerId == customerId, ct);
        return Result<IReadOnlyList<InvoiceDto>>.Success(_mapper.Map<IReadOnlyList<InvoiceDto>>(invoices));
    }

    public async Task<Result<PagedResult<InvoiceDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, i => i.BusinessId == businessId, ct);
        return Result<PagedResult<InvoiceDto>>.Success(result.Map(_mapper.Map<InvoiceDto>));
    }

    public async Task<Result<InvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        var invoice = await _repository.FindOneAsync(i => i.InvoiceNumber == invoiceNumber, ct);
        if (invoice is null)
            return Result<InvoiceDto>.NotFound($"Invoice with number {invoiceNumber} not found");
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceDto dto, CancellationToken ct = default)
    {
        var invoice = Invoice.Create(dto.BusinessId, dto.CustomerId, dto.UserId, dto.InvoiceNumber, dto.Amount, dto.DueDate, dto.Tax, dto.Discount, dto.Notes);
        await _repository.AddAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<InvoiceDto>> UpdateAsync(Guid id, UpdateInvoiceDto dto, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<InvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.Update(
            dto.Amount ?? invoice.Amount,
            dto.Tax ?? invoice.Tax,
            dto.Discount ?? invoice.Discount,
            dto.DueDate ?? invoice.DueDate,
            dto.Notes ?? invoice.Notes
        );

        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<bool>.NotFound($"Invoice with id {id} not found");

        await _repository.DeleteAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<InvoiceDto>> SendAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<InvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.Send();
        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<InvoiceDto>> MarkPaidAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<InvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.MarkPaid();
        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<InvoiceDto>> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Result<InvoiceDto>.NotFound($"Invoice with id {id} not found");

        invoice.Cancel();
        await _repository.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }
}

public class SaleService : ISaleService
{
    private readonly IRepository<Sale> _repository;
    private readonly IRepository<SaleItem> _saleItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SaleService(IRepository<Sale> repository, IRepository<SaleItem> saleItemRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _saleItemRepository = saleItemRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _repository.GetByIdAsync(id, ct);
        if (sale is null)
            return Result<SaleDto>.NotFound($"Sale with id {id} not found");
        return Result<SaleDto>.Success(_mapper.Map<SaleDto>(sale));
    }

    public async Task<Result<IReadOnlyList<SaleDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var sales = await _repository.FindAsync(s => s.BusinessId == businessId, ct);
        return Result<IReadOnlyList<SaleDto>>.Success(_mapper.Map<IReadOnlyList<SaleDto>>(sales));
    }

    public async Task<Result<PagedResult<SaleDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, s => s.BusinessId == businessId, ct);
        return Result<PagedResult<SaleDto>>.Success(result.Map(_mapper.Map<SaleDto>));
    }

    public async Task<Result<IReadOnlyList<SaleDto>>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var sales = await _repository.FindAsync(s => s.CustomerId == customerId, ct);
        return Result<IReadOnlyList<SaleDto>>.Success(_mapper.Map<IReadOnlyList<SaleDto>>(sales));
    }

    public async Task<Result<SaleDto>> CreateAsync(CreateSaleDto dto, CancellationToken ct = default)
    {
        var sale = Sale.Create(dto.BusinessId, dto.UserId, dto.Total, dto.PaymentMethod, dto.CustomerId, dto.Tax, dto.Discount, dto.Notes);
        await _repository.AddAsync(sale, ct);

        if (dto.Items is not null)
        {
            foreach (var itemDto in dto.Items)
            {
                var saleItem = SaleItem.Create(sale.Id, itemDto.ProductId, itemDto.Quantity, itemDto.Price);
                await _saleItemRepository.AddAsync(saleItem, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<SaleDto>.Success(_mapper.Map<SaleDto>(sale));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sale = await _repository.GetByIdAsync(id, ct);
        if (sale is null)
            return Result<bool>.NotFound($"Sale with id {id} not found");

        await _repository.DeleteAsync(sale, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class SaleItemService : ISaleItemService
{
    private readonly IRepository<SaleItem> _repository;
    private readonly IMapper _mapper;

    public SaleItemService(IRepository<SaleItem> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<SaleItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<SaleItemDto>.NotFound($"Sale item with id {id} not found");
        return Result<SaleItemDto>.Success(_mapper.Map<SaleItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<SaleItemDto>>> GetBySaleIdAsync(Guid saleId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.SaleId == saleId, ct);
        return Result<IReadOnlyList<SaleItemDto>>.Success(_mapper.Map<IReadOnlyList<SaleItemDto>>(items));
    }
}

public class InventoryItemService : IInventoryItemService
{
    private readonly IRepository<InventoryItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InventoryItemService(IRepository<InventoryItem> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<InventoryItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<InventoryItemDto>.NotFound($"Inventory item with id {id} not found");
        return Result<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.BusinessId == businessId, ct);
        return Result<IReadOnlyList<InventoryItemDto>>.Success(_mapper.Map<IReadOnlyList<InventoryItemDto>>(items));
    }

    public async Task<Result<InventoryItemDto>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        var item = await _repository.FindOneAsync(i => i.ProductId == productId, ct);
        if (item is null)
            return Result<InventoryItemDto>.NotFound($"Inventory item for product {productId} not found");
        return Result<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(item));
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetLowStockAsync(Guid businessId, CancellationToken ct = default)
    {
        var items = await _repository.FindAsync(i => i.BusinessId == businessId && i.CurrentStock <= i.MinStock, ct);
        return Result<IReadOnlyList<InventoryItemDto>>.Success(_mapper.Map<IReadOnlyList<InventoryItemDto>>(items));
    }

    public async Task<Result<InventoryItemDto>> CreateAsync(CreateInventoryItemDto dto, CancellationToken ct = default)
    {
        var item = InventoryItem.Create(dto.BusinessId, dto.ProductId, dto.UserId, dto.CurrentStock, dto.MinStock, dto.MaxStock, dto.ReorderPoint);
        await _repository.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(item));
    }

    public async Task<Result<InventoryItemDto>> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<InventoryItemDto>.NotFound($"Inventory item with id {id} not found");

        if (dto.CurrentStock.HasValue) item.UpdateStock(dto.CurrentStock.Value);
        item.SetStockLevels(dto.MinStock ?? item.MinStock, dto.MaxStock ?? item.MaxStock, dto.ReorderPoint ?? item.ReorderPoint);

        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(item));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<bool>.NotFound($"Inventory item with id {id} not found");

        await _repository.DeleteAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<InventoryItemDto>> AddStockAsync(Guid id, int quantity, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<InventoryItemDto>.NotFound($"Inventory item with id {id} not found");

        item.AddStock(quantity);
        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(item));
    }

    public async Task<Result<InventoryItemDto>> RemoveStockAsync(Guid id, int quantity, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null)
            return Result<InventoryItemDto>.NotFound($"Inventory item with id {id} not found");

        item.RemoveStock(quantity);
        await _repository.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(item));
    }
}

public class MarketingCampaignService : IMarketingCampaignService
{
    private readonly IRepository<MarketingCampaign> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MarketingCampaignService(IRepository<MarketingCampaign> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<MarketingCampaignDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<MarketingCampaignDto>.NotFound($"Campaign with id {id} not found");
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }

    public async Task<Result<IReadOnlyList<MarketingCampaignDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var campaigns = await _repository.FindAsync(c => c.BusinessId == businessId, ct);
        return Result<IReadOnlyList<MarketingCampaignDto>>.Success(_mapper.Map<IReadOnlyList<MarketingCampaignDto>>(campaigns));
    }

    public async Task<Result<PagedResult<MarketingCampaignDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, c => c.BusinessId == businessId, ct);
        return Result<PagedResult<MarketingCampaignDto>>.Success(result.Map(_mapper.Map<MarketingCampaignDto>));
    }

    public async Task<Result<MarketingCampaignDto>> CreateAsync(CreateMarketingCampaignDto dto, CancellationToken ct = default)
    {
        var campaign = MarketingCampaign.Create(dto.BusinessId, dto.UserId, dto.Name, dto.Platform, dto.Content, dto.Description, dto.MediaUrls, dto.ScheduledAt);
        await _repository.AddAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }

    public async Task<Result<MarketingCampaignDto>> UpdateAsync(Guid id, UpdateMarketingCampaignDto dto, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<MarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.Update(
            dto.Name ?? campaign.Name,
            dto.Description ?? campaign.Description,
            dto.Platform ?? campaign.Platform,
            dto.Content ?? campaign.Content,
            dto.MediaUrls ?? campaign.MediaUrls
        );

        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<bool>.NotFound($"Campaign with id {id} not found");

        await _repository.DeleteAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<MarketingCampaignDto>> ScheduleAsync(Guid id, DateTime scheduledAt, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<MarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.Schedule(scheduledAt);
        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }

    public async Task<Result<MarketingCampaignDto>> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<MarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.Activate();
        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }

    public async Task<Result<MarketingCampaignDto>> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<MarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.Complete();
        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }

    public async Task<Result<MarketingCampaignDto>> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _repository.GetByIdAsync(id, ct);
        if (campaign is null)
            return Result<MarketingCampaignDto>.NotFound($"Campaign with id {id} not found");

        campaign.Cancel();
        await _repository.UpdateAsync(campaign, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<MarketingCampaignDto>.Success(_mapper.Map<MarketingCampaignDto>(campaign));
    }
}

public class BusinessAnalyticsService : IBusinessAnalyticsService
{
    private readonly IRepository<BusinessAnalytics> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BusinessAnalyticsService(IRepository<BusinessAnalytics> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<BusinessAnalyticsDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var analytics = await _repository.GetByIdAsync(id, ct);
        if (analytics is null)
            return Result<BusinessAnalyticsDto>.NotFound($"Analytics with id {id} not found");
        return Result<BusinessAnalyticsDto>.Success(_mapper.Map<BusinessAnalyticsDto>(analytics));
    }

    public async Task<Result<IReadOnlyList<BusinessAnalyticsDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
    {
        var analytics = await _repository.FindAsync(a => a.BusinessId == businessId, ct);
        return Result<IReadOnlyList<BusinessAnalyticsDto>>.Success(_mapper.Map<IReadOnlyList<BusinessAnalyticsDto>>(analytics));
    }

    public async Task<Result<IReadOnlyList<BusinessAnalyticsDto>>> GetByDateRangeAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var analytics = await _repository.FindAsync(a => a.BusinessId == businessId && a.Date >= startDate.Date && a.Date <= endDate.Date, ct);
        return Result<IReadOnlyList<BusinessAnalyticsDto>>.Success(_mapper.Map<IReadOnlyList<BusinessAnalyticsDto>>(analytics));
    }

    public async Task<Result<BusinessAnalyticsDto>> GetByDateAsync(Guid businessId, DateTime date, CancellationToken ct = default)
    {
        var analytics = await _repository.FindOneAsync(a => a.BusinessId == businessId && a.Date == date.Date, ct);
        if (analytics is null)
            return Result<BusinessAnalyticsDto>.NotFound($"Analytics for date {date:yyyy-MM-dd} not found");
        return Result<BusinessAnalyticsDto>.Success(_mapper.Map<BusinessAnalyticsDto>(analytics));
    }

    public async Task<Result<BusinessAnalyticsDto>> CreateAsync(CreateBusinessAnalyticsDto dto, CancellationToken ct = default)
    {
        var analytics = BusinessAnalytics.Create(dto.BusinessId, dto.Date, dto.Revenue, dto.Expenses, dto.SalesCount, dto.NewCustomers);
        await _repository.AddAsync(analytics, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BusinessAnalyticsDto>.Success(_mapper.Map<BusinessAnalyticsDto>(analytics));
    }

    public async Task<Result<BusinessAnalyticsDto>> UpdateAsync(Guid id, UpdateBusinessAnalyticsDto dto, CancellationToken ct = default)
    {
        var analytics = await _repository.GetByIdAsync(id, ct);
        if (analytics is null)
            return Result<BusinessAnalyticsDto>.NotFound($"Analytics with id {id} not found");

        analytics.Update(
            dto.Revenue ?? analytics.Revenue,
            dto.Expenses ?? analytics.Expenses,
            dto.SalesCount ?? analytics.SalesCount,
            dto.NewCustomers ?? analytics.NewCustomers
        );

        await _repository.UpdateAsync(analytics, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<BusinessAnalyticsDto>.Success(_mapper.Map<BusinessAnalyticsDto>(analytics));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var analytics = await _repository.GetByIdAsync(id, ct);
        if (analytics is null)
            return Result<bool>.NotFound($"Analytics with id {id} not found");

        await _repository.DeleteAsync(analytics, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

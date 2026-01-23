namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IBusinessService
{
    Task<Result<BusinessDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BusinessDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<BusinessDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BusinessDto>>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
    Task<Result<BusinessDto>> CreateAsync(CreateBusinessDto dto, CancellationToken ct = default);
    Task<Result<BusinessDto>> UpdateAsync(Guid id, UpdateBusinessDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<BusinessDto>> UpgradePlanAsync(Guid id, SubscriptionPlan plan, CancellationToken ct = default);
    Task<Result<BusinessDto>> ToggleActiveAsync(Guid id, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CustomerDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<PagedResult<CustomerDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProductDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<PagedResult<ProductDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ProductDto>> GetBySkuAsync(Guid businessId, string sku, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ProductDto>> ToggleActiveAsync(Guid id, CancellationToken ct = default);
}

public interface IInvoiceService
{
    Task<Result<InvoiceDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<InvoiceDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<InvoiceDto>>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<PagedResult<InvoiceDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<InvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceDto dto, CancellationToken ct = default);
    Task<Result<InvoiceDto>> UpdateAsync(Guid id, UpdateInvoiceDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<InvoiceDto>> SendAsync(Guid id, CancellationToken ct = default);
    Task<Result<InvoiceDto>> MarkPaidAsync(Guid id, CancellationToken ct = default);
    Task<Result<InvoiceDto>> CancelAsync(Guid id, CancellationToken ct = default);
}

public interface ISaleService
{
    Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SaleDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<PagedResult<SaleDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SaleDto>>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<SaleDto>> CreateAsync(CreateSaleDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ISaleItemService
{
    Task<Result<SaleItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SaleItemDto>>> GetBySaleIdAsync(Guid saleId, CancellationToken ct = default);
}

public interface IInventoryItemService
{
    Task<Result<InventoryItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetLowStockAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> CreateAsync(CreateInventoryItemDto dto, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> AddStockAsync(Guid id, int quantity, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> RemoveStockAsync(Guid id, int quantity, CancellationToken ct = default);
}

public interface IMarketingCampaignService
{
    Task<Result<MarketingCampaignDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MarketingCampaignDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<PagedResult<MarketingCampaignDto>>> GetPagedByBusinessIdAsync(Guid businessId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<MarketingCampaignDto>> CreateAsync(CreateMarketingCampaignDto dto, CancellationToken ct = default);
    Task<Result<MarketingCampaignDto>> UpdateAsync(Guid id, UpdateMarketingCampaignDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<MarketingCampaignDto>> ScheduleAsync(Guid id, DateTime scheduledAt, CancellationToken ct = default);
    Task<Result<MarketingCampaignDto>> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<Result<MarketingCampaignDto>> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<MarketingCampaignDto>> CancelAsync(Guid id, CancellationToken ct = default);
}

public interface IBusinessAnalyticsService
{
    Task<Result<BusinessAnalyticsDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BusinessAnalyticsDto>>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BusinessAnalyticsDto>>> GetByDateRangeAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Result<BusinessAnalyticsDto>> GetByDateAsync(Guid businessId, DateTime date, CancellationToken ct = default);
    Task<Result<BusinessAnalyticsDto>> CreateAsync(CreateBusinessAnalyticsDto dto, CancellationToken ct = default);
    Task<Result<BusinessAnalyticsDto>> UpdateAsync(Guid id, UpdateBusinessAnalyticsDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

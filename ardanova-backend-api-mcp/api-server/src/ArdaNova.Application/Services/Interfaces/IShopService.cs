namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface IShopService
{
    Task<Result<ShopDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<ShopDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopDto>>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default);
    Task<Result<ShopDto>> CreateAsync(CreateShopDto dto, CancellationToken ct = default);
    Task<Result<ShopDto>> UpdateAsync(string id, UpdateShopDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ShopDto>> UpgradePlanAsync(string id, SubscriptionPlan plan, CancellationToken ct = default);
    Task<Result<ShopDto>> ToggleActiveAsync(string id, CancellationToken ct = default);
}

public interface IShopCustomerService
{
    Task<Result<ShopCustomerDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopCustomerDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<PagedResult<ShopCustomerDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ShopCustomerDto>> CreateAsync(CreateShopCustomerDto dto, CancellationToken ct = default);
    Task<Result<ShopCustomerDto>> UpdateAsync(string id, UpdateShopCustomerDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IShopProductService
{
    Task<Result<ShopProductDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopProductDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<PagedResult<ShopProductDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ShopProductDto>> GetBySkuAsync(string shopId, string sku, CancellationToken ct = default);
    Task<Result<ShopProductDto>> CreateAsync(CreateShopProductDto dto, CancellationToken ct = default);
    Task<Result<ShopProductDto>> UpdateAsync(string id, UpdateShopProductDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ShopProductDto>> ToggleActiveAsync(string id, CancellationToken ct = default);
}

public interface IShopInvoiceService
{
    Task<Result<ShopInvoiceDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopInvoiceDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopInvoiceDto>>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default);
    Task<Result<PagedResult<ShopInvoiceDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ShopInvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<Result<ShopInvoiceDto>> CreateAsync(CreateShopInvoiceDto dto, CancellationToken ct = default);
    Task<Result<ShopInvoiceDto>> UpdateAsync(string id, UpdateShopInvoiceDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ShopInvoiceDto>> SendAsync(string id, CancellationToken ct = default);
    Task<Result<ShopInvoiceDto>> MarkPaidAsync(string id, CancellationToken ct = default);
    Task<Result<ShopInvoiceDto>> CancelAsync(string id, CancellationToken ct = default);
}

public interface IShopSaleService
{
    Task<Result<ShopSaleDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopSaleDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<PagedResult<ShopSaleDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopSaleDto>>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default);
    Task<Result<ShopSaleDto>> CreateAsync(CreateShopSaleDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IShopSaleItemService
{
    Task<Result<ShopSaleItemDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopSaleItemDto>>> GetBySaleIdAsync(string saleId, CancellationToken ct = default);
}

public interface IShopInventoryItemService
{
    Task<Result<ShopInventoryItemDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopInventoryItemDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<ShopInventoryItemDto>> GetByProductIdAsync(string productId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopInventoryItemDto>>> GetLowStockAsync(string shopId, CancellationToken ct = default);
    Task<Result<ShopInventoryItemDto>> CreateAsync(CreateShopInventoryItemDto dto, CancellationToken ct = default);
    Task<Result<ShopInventoryItemDto>> UpdateAsync(string id, UpdateShopInventoryItemDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ShopInventoryItemDto>> AddStockAsync(string id, int quantity, CancellationToken ct = default);
    Task<Result<ShopInventoryItemDto>> RemoveStockAsync(string id, int quantity, CancellationToken ct = default);
}

public interface IShopMarketingCampaignService
{
    Task<Result<ShopMarketingCampaignDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopMarketingCampaignDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<PagedResult<ShopMarketingCampaignDto>>> GetPagedByShopIdAsync(string shopId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ShopMarketingCampaignDto>> CreateAsync(CreateShopMarketingCampaignDto dto, CancellationToken ct = default);
    Task<Result<ShopMarketingCampaignDto>> UpdateAsync(string id, UpdateShopMarketingCampaignDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<ShopMarketingCampaignDto>> ScheduleAsync(string id, DateTime scheduledAt, CancellationToken ct = default);
    Task<Result<ShopMarketingCampaignDto>> ActivateAsync(string id, CancellationToken ct = default);
    Task<Result<ShopMarketingCampaignDto>> CompleteAsync(string id, CancellationToken ct = default);
    Task<Result<ShopMarketingCampaignDto>> CancelAsync(string id, CancellationToken ct = default);
}

public interface IShopAnalyticsService
{
    Task<Result<ShopAnalyticsDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopAnalyticsDto>>> GetByShopIdAsync(string shopId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ShopAnalyticsDto>>> GetByDateRangeAsync(string shopId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Result<ShopAnalyticsDto>> GetByDateAsync(string shopId, DateTime date, CancellationToken ct = default);
    Task<Result<ShopAnalyticsDto>> CreateAsync(CreateShopAnalyticsDto dto, CancellationToken ct = default);
    Task<Result<ShopAnalyticsDto>> UpdateAsync(string id, UpdateShopAnalyticsDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record ShopDto
{
    public string Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }
    public ShopCategory Category { get; init; }
    public string? Industry { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? Logo { get; init; }
    public SubscriptionPlan Plan { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string OwnerId { get; init; }
}

public record CreateShopDto
{
    public required string OwnerId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ShopCategory Category { get; init; }
    public string? Industry { get; init; }
}

public record UpdateShopDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public ShopCategory? Category { get; init; }
    public string? Industry { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? Logo { get; init; }
    public SubscriptionPlan? Plan { get; init; }
}

public record ShopCustomerDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UserId { get; init; }
}

public record CreateShopCustomerDto
{
    public required string ShopId { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
}

public record UpdateShopCustomerDto
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
}

public record ShopProductDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UserId { get; init; }
}

public record CreateShopProductDto
{
    public required string ShopId { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
}

public record UpdateShopProductDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal? Price { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
}

public record ShopInvoiceDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public string CustomerId { get; init; }
    public string InvoiceNumber { get; init; } = null!;
    public decimal Amount { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public decimal Total { get; init; }
    public InvoiceStatus Status { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UserId { get; init; }
}

public record CreateShopInvoiceDto
{
    public required string ShopId { get; init; }
    public required string CustomerId { get; init; }
    public required string UserId { get; init; }
    public required string InvoiceNumber { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime DueDate { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public string? Notes { get; init; }
}

public record UpdateShopInvoiceDto
{
    public decimal? Amount { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public DateTime? DueDate { get; init; }
    public string? Notes { get; init; }
}

public record ShopSaleDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public string? CustomerId { get; init; }
    public decimal Total { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public string UserId { get; init; }
}

public record CreateShopSaleDto
{
    public required string ShopId { get; init; }
    public required string UserId { get; init; }
    public required decimal Total { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public string? CustomerId { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public string? Notes { get; init; }
    public List<CreateShopSaleItemDto>? Items { get; init; }
}

public record ShopSaleItemDto
{
    public string Id { get; init; }
    public string SaleId { get; init; }
    public string ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal Total { get; init; }
}

public record CreateShopSaleItemDto
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
}

public record ShopInventoryItemDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public string ProductId { get; init; }
    public int CurrentStock { get; init; }
    public int MinStock { get; init; }
    public int? MaxStock { get; init; }
    public int? ReorderPoint { get; init; }
    public DateTime? LastRestocked { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UserId { get; init; }
}

public record CreateShopInventoryItemDto
{
    public required string ShopId { get; init; }
    public required string ProductId { get; init; }
    public required string UserId { get; init; }
    public int CurrentStock { get; init; } = 0;
    public int MinStock { get; init; } = 0;
    public int? MaxStock { get; init; }
    public int? ReorderPoint { get; init; }
}

public record UpdateShopInventoryItemDto
{
    public int? CurrentStock { get; init; }
    public int? MinStock { get; init; }
    public int? MaxStock { get; init; }
    public int? ReorderPoint { get; init; }
}

public record ShopMarketingCampaignDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Platform { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? MediaUrls { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public CampaignStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UserId { get; init; }
}

public record CreateShopMarketingCampaignDto
{
    public required string ShopId { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required string Platform { get; init; }
    public required string Content { get; init; }
    public string? Description { get; init; }
    public string? MediaUrls { get; init; }
    public DateTime? ScheduledAt { get; init; }
}

public record UpdateShopMarketingCampaignDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Platform { get; init; }
    public string? Content { get; init; }
    public string? MediaUrls { get; init; }
    public DateTime? ScheduledAt { get; init; }
}

public record ShopAnalyticsDto
{
    public string Id { get; init; }
    public string ShopId { get; init; }
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public decimal Expenses { get; init; }
    public decimal Profit { get; init; }
    public int SalesCount { get; init; }
    public int NewCustomers { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateShopAnalyticsDto
{
    public required string ShopId { get; init; }
    public required DateTime Date { get; init; }
    public required decimal Revenue { get; init; }
    public required decimal Expenses { get; init; }
    public required int SalesCount { get; init; }
    public required int NewCustomers { get; init; }
}

public record UpdateShopAnalyticsDto
{
    public decimal? Revenue { get; init; }
    public decimal? Expenses { get; init; }
    public int? SalesCount { get; init; }
    public int? NewCustomers { get; init; }
}

namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record BusinessDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
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
    public Guid OwnerId { get; init; }
}

public record CreateBusinessDto
{
    public required Guid OwnerId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Industry { get; init; }
}

public record UpdateBusinessDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Industry { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? Logo { get; init; }
    public SubscriptionPlan? Plan { get; init; }
}

public record CustomerDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UserId { get; init; }
}

public record CreateCustomerDto
{
    public required Guid BusinessId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
}

public record UpdateCustomerDto
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
}

public record ProductDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UserId { get; init; }
}

public record CreateProductDto
{
    public required Guid BusinessId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
}

public record UpdateProductDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal? Price { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
}

public record InvoiceDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid CustomerId { get; init; }
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
    public Guid UserId { get; init; }
}

public record CreateInvoiceDto
{
    public required Guid BusinessId { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid UserId { get; init; }
    public required string InvoiceNumber { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime DueDate { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public string? Notes { get; init; }
}

public record UpdateInvoiceDto
{
    public decimal? Amount { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public DateTime? DueDate { get; init; }
    public string? Notes { get; init; }
}

public record SaleDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid? CustomerId { get; init; }
    public decimal Total { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid UserId { get; init; }
}

public record CreateSaleDto
{
    public required Guid BusinessId { get; init; }
    public required Guid UserId { get; init; }
    public required decimal Total { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public Guid? CustomerId { get; init; }
    public decimal? Tax { get; init; }
    public decimal? Discount { get; init; }
    public string? Notes { get; init; }
    public List<CreateSaleItemDto>? Items { get; init; }
}

public record SaleItemDto
{
    public Guid Id { get; init; }
    public Guid SaleId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal Total { get; init; }
}

public record CreateSaleItemDto
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
}

public record InventoryItemDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid ProductId { get; init; }
    public int CurrentStock { get; init; }
    public int MinStock { get; init; }
    public int? MaxStock { get; init; }
    public int? ReorderPoint { get; init; }
    public DateTime? LastRestocked { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UserId { get; init; }
}

public record CreateInventoryItemDto
{
    public required Guid BusinessId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid UserId { get; init; }
    public int CurrentStock { get; init; } = 0;
    public int MinStock { get; init; } = 0;
    public int? MaxStock { get; init; }
    public int? ReorderPoint { get; init; }
}

public record UpdateInventoryItemDto
{
    public int? CurrentStock { get; init; }
    public int? MinStock { get; init; }
    public int? MaxStock { get; init; }
    public int? ReorderPoint { get; init; }
}

public record MarketingCampaignDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Platform { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? MediaUrls { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public CampaignStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UserId { get; init; }
}

public record CreateMarketingCampaignDto
{
    public required Guid BusinessId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Platform { get; init; }
    public required string Content { get; init; }
    public string? Description { get; init; }
    public string? MediaUrls { get; init; }
    public DateTime? ScheduledAt { get; init; }
}

public record UpdateMarketingCampaignDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Platform { get; init; }
    public string? Content { get; init; }
    public string? MediaUrls { get; init; }
    public DateTime? ScheduledAt { get; init; }
}

public record BusinessAnalyticsDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public decimal Expenses { get; init; }
    public decimal Profit { get; init; }
    public int SalesCount { get; init; }
    public int NewCustomers { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateBusinessAnalyticsDto
{
    public required Guid BusinessId { get; init; }
    public required DateTime Date { get; init; }
    public required decimal Revenue { get; init; }
    public required decimal Expenses { get; init; }
    public required int SalesCount { get; init; }
    public required int NewCustomers { get; init; }
}

public record UpdateBusinessAnalyticsDto
{
    public decimal? Revenue { get; init; }
    public decimal? Expenses { get; init; }
    public int? SalesCount { get; init; }
    public int? NewCustomers { get; init; }
}

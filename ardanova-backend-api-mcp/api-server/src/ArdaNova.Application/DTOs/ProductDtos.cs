namespace ArdaNova.Application.DTOs;

public record ProductDto
{
    public string Id { get; init; } = null!;
    public string ProjectId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public decimal? Cost { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string UserId { get; init; } = null!;
}

public record CreateProductDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
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

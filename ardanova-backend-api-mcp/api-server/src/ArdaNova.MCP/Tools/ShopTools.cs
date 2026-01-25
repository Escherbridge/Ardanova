namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ModelContextProtocol.Server;

[McpServerToolType]
public class ShopTools
{
    private readonly IShopService _shopService;

    public ShopTools(IShopService shopService)
    {
        _shopService = shopService;
    }

    [McpServerTool(Name = "shop_get_by_id")]
    [Description("Retrieves a shop by its unique identifier")]
    public async Task<ShopDto?> GetShopById(
        [Description("The unique identifier of the shop")] string id,
        CancellationToken ct = default)
    {
        var result = await _shopService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "shop_get_paged")]
    [Description("Retrieves shops with pagination")]
    public async Task<object?> GetPagedShops(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _shopService.GetPagedAsync(page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "shop_get_by_owner")]
    [Description("Retrieves shops by their owner's user ID")]
    public async Task<IReadOnlyList<ShopDto>?> GetShopsByOwner(
        [Description("The unique identifier of the owner user")] string ownerId,
        CancellationToken ct = default)
    {
        var result = await _shopService.GetByOwnerIdAsync(ownerId, ct);
        return result.IsSuccess ? result.Value : null;
    }
}

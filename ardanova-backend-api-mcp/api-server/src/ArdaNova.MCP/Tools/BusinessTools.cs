namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ModelContextProtocol.Server;

[McpServerToolType]
public class BusinessTools
{
    private readonly IBusinessService _businessService;

    public BusinessTools(IBusinessService businessService)
    {
        _businessService = businessService;
    }

    [McpServerTool(Name = "business_get_by_id")]
    [Description("Retrieves a business by its unique identifier")]
    public async Task<BusinessDto?> GetBusinessById(
        [Description("The unique identifier of the business")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _businessService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "business_get_paged")]
    [Description("Retrieves businesses with pagination")]
    public async Task<object?> GetPagedBusinesses(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _businessService.GetPagedAsync(page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "business_get_by_owner")]
    [Description("Retrieves businesses by their owner's user ID")]
    public async Task<IReadOnlyList<BusinessDto>?> GetBusinessesByOwner(
        [Description("The unique identifier of the owner user")] Guid ownerId,
        CancellationToken ct = default)
    {
        var result = await _businessService.GetByOwnerIdAsync(ownerId, ct);
        return result.IsSuccess ? result.Value : null;
    }


}
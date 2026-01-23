namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class AgencyTools
{
    private readonly IAgencyService _agencyService;

    public AgencyTools(IAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    [McpServerTool(Name = "agency_get_by_id")]
    [Description("Retrieves an agency by its unique identifier")]
    public async Task<AgencyDto?> GetAgencyById(
        [Description("The unique identifier of the agency")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _agencyService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "agency_get_by_slug")]
    [Description("Retrieves an agency by its URL slug")]
    public async Task<AgencyDto?> GetAgencyBySlug(
        [Description("The URL-friendly slug of the agency")] string slug,
        CancellationToken ct = default)
    {
        var result = await _agencyService.GetBySlugAsync(slug, ct);
        return result.IsSuccess ? result.Value : null;
    } 

    [McpServerTool(Name = "agency_get_paged")]
    [Description("Retrieves agencies with pagination")]
    public async Task<object?> GetPagedAgencies(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _agencyService.GetPagedAsync(page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "agency_get_by_owner")]
    [Description("Retrieves an agency by its owner's user ID")]
    public async Task<AgencyDto?> GetAgencyByOwner(
        [Description("The unique identifier of the owner user")] Guid ownerId,
        CancellationToken ct = default)
    {
        var result = await _agencyService.GetByOwnerIdAsync(ownerId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "agency_get_verified")]
    [Description("Retrieves all verified agencies")]
    public async Task<IReadOnlyList<AgencyDto>?> GetVerifiedAgencies(CancellationToken ct = default)
    {
        var result = await _agencyService.GetVerifiedAsync(ct);
        return result.IsSuccess ? result.Value : null;
    }

}

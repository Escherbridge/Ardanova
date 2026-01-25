namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class GuildTools
{
    private readonly IGuildService _guildService;

    public GuildTools(IGuildService guildService)
    {
        _guildService = guildService;
    }

    [McpServerTool(Name = "guild_get_by_id")]
    [Description("Retrieves a guild by its unique identifier")]
    public async Task<GuildDto?> GetGuildById(
        [Description("The unique identifier of the guild")] string id,
        CancellationToken ct = default)
    {
        var result = await _guildService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "guild_get_by_slug")]
    [Description("Retrieves a guild by its URL slug")]
    public async Task<GuildDto?> GetGuildBySlug(
        [Description("The URL-friendly slug of the guild")] string slug,
        CancellationToken ct = default)
    {
        var result = await _guildService.GetBySlugAsync(slug, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "guild_get_paged")]
    [Description("Retrieves guilds with pagination")]
    public async Task<object?> GetPagedGuilds(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _guildService.GetPagedAsync(page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "guild_get_by_owner")]
    [Description("Retrieves a guild by its owner's user ID")]
    public async Task<GuildDto?> GetGuildByOwner(
        [Description("The unique identifier of the owner user")] string ownerId,
        CancellationToken ct = default)
    {
        var result = await _guildService.GetByOwnerIdAsync(ownerId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "guild_get_verified")]
    [Description("Retrieves all verified guilds")]
    public async Task<IReadOnlyList<GuildDto>?> GetVerifiedGuilds(CancellationToken ct = default)
    {
        var result = await _guildService.GetVerifiedAsync(ct);
        return result.IsSuccess ? result.Value : null;
    }
}

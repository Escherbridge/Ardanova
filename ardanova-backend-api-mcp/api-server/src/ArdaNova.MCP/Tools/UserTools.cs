namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class UserTools
{
    private readonly IUserService _userService;

    public UserTools(IUserService userService)
    {
        _userService = userService;
    }

    [McpServerTool(Name = "user_get_by_id")]
    [Description("Retrieves a user by their unique identifier")]
    public async Task<UserDto?> GetUserById(
        [Description("The unique identifier of the user")] string id,
        CancellationToken ct = default)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "user_get_by_email")]
    [Description("Retrieves a user by their email address")]
    public async Task<UserDto?> GetUserByEmail(
        [Description("The email address of the user")] string email,
        CancellationToken ct = default)
    {
        var result = await _userService.GetByEmailAsync(email, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "user_get_paged")]
    [Description("Retrieves users with pagination")]
    public async Task<object?> GetPagedUsers(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, ct);
        return result.IsSuccess ? result.Value : null;
    }



}

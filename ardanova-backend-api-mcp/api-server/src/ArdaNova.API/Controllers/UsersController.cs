namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _userService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _userService.SearchAsync(query, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:minlength(10)}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetByEmail(string email, CancellationToken ct)
    {
        var result = await _userService.GetByEmailAsync(email, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var result = await _userService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        var result = await _userService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _userService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> Verify(string id, CancellationToken ct)
    {
        var result = await _userService.VerifyAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] AdminUpdateUserRoleDto dto, CancellationToken ct)
    {
        var result = await _userService.UpdateRoleAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/user-type")]
    public async Task<IActionResult> UpdateUserType(string id, [FromBody] AdminUpdateUserTypeDto dto, CancellationToken ct)
    {
        var result = await _userService.UpdateUserTypeAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/verification-level")]
    public async Task<IActionResult> UpdateVerificationLevel(string id, [FromBody] AdminUpdateVerificationLevelDto dto, CancellationToken ct)
    {
        var result = await _userService.UpdateVerificationLevelAsync(id, dto, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

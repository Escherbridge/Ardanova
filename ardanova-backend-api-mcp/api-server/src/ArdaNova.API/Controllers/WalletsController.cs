namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _walletService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _walletService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("address/{address}")]
    public async Task<IActionResult> GetByAddress(string address, CancellationToken ct)
    {
        var result = await _walletService.GetByAddressAsync(address, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/primary")]
    public async Task<IActionResult> GetPrimaryWallet(string userId, CancellationToken ct)
    {
        var result = await _walletService.GetPrimaryWalletAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletDto dto, CancellationToken ct)
    {
        var result = await _walletService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateWalletDto dto, CancellationToken ct)
    {
        var result = await _walletService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> Verify(string id, CancellationToken ct)
    {
        var result = await _walletService.VerifyAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/set-primary")]
    public async Task<IActionResult> SetPrimary(string id, CancellationToken ct)
    {
        var result = await _walletService.SetPrimaryAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _walletService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
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

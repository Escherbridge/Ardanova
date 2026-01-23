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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _walletService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken ct)
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

    [HttpGet("user/{userId:guid}/primary")]
    public async Task<IActionResult> GetPrimaryWallet(Guid userId, CancellationToken ct)
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWalletDto dto, CancellationToken ct)
    {
        var result = await _walletService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var result = await _walletService.VerifyAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken ct)
    {
        var result = await _walletService.SetPrimaryAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
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

namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IWalletVerificationService? _walletVerification;

    public WalletsController(IWalletService walletService)
        : this(walletService, null)
    {
    }

    public WalletsController(
        IWalletService walletService,
        IWalletVerificationService? walletVerification)
    {
        _walletService = walletService;
        _walletVerification = walletVerification;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _walletService.GetByIdAsync(id, ct);
        return IsActorWallet(result) ? ToActionResult(result) : Forbid();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _walletService.GetByUserIdAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("address/{address}")]
    public async Task<IActionResult> GetByAddress(string address, CancellationToken ct)
    {
        var result = await _walletService.GetByAddressAsync(address, ct);
        return IsActorWallet(result) ? ToActionResult(result) : Forbid();
    }

    [HttpGet("me/primary")]
    public async Task<IActionResult> GetMyPrimaryWallet(CancellationToken ct)
    {
        var result = await _walletService.GetPrimaryWalletAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletRequest dto, CancellationToken ct)
    {
        var result = await _walletService.CreateAsync(new CreateWalletDto
        {
            UserId = ActorId,
            Address = dto.Address,
            Provider = dto.Provider,
            Label = dto.Label,
            IsPrimary = dto.IsPrimary
        }, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateWalletDto dto, CancellationToken ct)
    {
        if (!await OwnsWalletAsync(id, ct))
            return Forbid();

        return ToActionResult(await _walletService.UpdateAsync(id, dto, ct));
    }

    [HttpPost("{id}/verification-challenge")]
    public async Task<IActionResult> IssueVerificationChallenge(string id, CancellationToken ct)
    {
        if (!await OwnsWalletAsync(id, ct))
            return Forbid();

        if (_walletVerification is null)
            return Problem("Wallet verification service is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);

        return ToActionResult(await _walletVerification.IssueAsync(ActorId, id, ct));
    }

    [HttpPost("{id}/verification-challenge/complete")]
    public async Task<IActionResult> CompleteVerificationChallenge(
        string id,
        [FromBody] CompleteWalletVerificationDto request,
        CancellationToken ct)
    {
        if (!await OwnsWalletAsync(id, ct))
            return Forbid();

        if (_walletVerification is null)
            return Problem("Wallet verification service is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);

        return ToActionResult(await _walletVerification.CompleteAsync(ActorId, id, request, ct));
    }

    [HttpPost("{id}/set-primary")]
    public async Task<IActionResult> SetPrimary(string id, CancellationToken ct)
    {
        if (!await OwnsWalletAsync(id, ct))
            return Forbid();

        return ToActionResult(await _walletService.SetPrimaryAsync(id, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (!await OwnsWalletAsync(id, ct))
            return Forbid();

        var result = await _walletService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<bool> OwnsWalletAsync(string id, CancellationToken ct)
    {
        var result = await _walletService.GetByIdAsync(id, ct);
        return IsActorWallet(result);
    }

    private bool IsActorWallet(Result<WalletDto> result)
        => result.IsSuccess && string.Equals(result.Value!.UserId, ActorId, StringComparison.Ordinal);

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    public sealed record CreateWalletRequest(
        string Address,
        ArdaNova.Domain.Models.Enums.WalletProvider Provider,
        string? Label,
        bool IsPrimary);
}

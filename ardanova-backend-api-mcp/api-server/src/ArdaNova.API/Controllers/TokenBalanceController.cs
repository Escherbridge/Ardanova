namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TokenBalanceController : ControllerBase
{
    private readonly ITokenBalanceService _tokenBalanceService;
    private readonly IExchangeService _exchangeService;

    public TokenBalanceController(
        ITokenBalanceService tokenBalanceService,
        IExchangeService exchangeService)
    {
        _tokenBalanceService = tokenBalanceService;
        _exchangeService = exchangeService;
    }

    [HttpGet("me/balance")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetBalance(
        [FromQuery] string projectTokenConfigId,
        [FromQuery] TokenHolderClass holderClass,
        CancellationToken ct)
    {
        var result = await _tokenBalanceService.GetBalanceAsync(ActorId, projectTokenConfigId, holderClass, ct);
        return ToActionResult(result);
    }

    [HttpGet("me/arda")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetArdaBalance(CancellationToken ct)
    {
        var result = await _tokenBalanceService.GetArdaBalanceAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("me/portfolio")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetPortfolio(CancellationToken ct)
    {
        var result = await _tokenBalanceService.GetPortfolioAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("me/liquidity")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> IsBalanceLiquid(
        [FromQuery] string projectTokenConfigId,
        [FromQuery] TokenHolderClass holderClass,
        CancellationToken ct)
    {
        var result = await _tokenBalanceService.IsBalanceLiquidAsync(ActorId, projectTokenConfigId, holderClass, ct);
        return ToActionResult(result);
    }

    // === Exchange ===

    [HttpGet("exchange/project-token-value/{configId}")]
    public async Task<IActionResult> GetProjectTokenValue(string configId, CancellationToken ct)
    {
        var result = await _exchangeService.GetProjectTokenValueAsync(configId, ct);
        return ToActionResult(result);
    }

    [HttpGet("exchange/arda-value")]
    public async Task<IActionResult> GetArdaValue(CancellationToken ct)
    {
        var result = await _exchangeService.GetArdaValueAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("exchange/conversion-preview")]
    public async Task<IActionResult> CalculateConversion(
        [FromQuery] string projectTokenConfigId,
        [FromQuery] int tokenAmount,
        CancellationToken ct)
    {
        var result = await _exchangeService.CalculateConversionAsync(projectTokenConfigId, tokenAmount, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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

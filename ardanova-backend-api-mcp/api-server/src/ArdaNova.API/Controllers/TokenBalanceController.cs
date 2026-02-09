namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
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

    [HttpGet("{userId}/balance")]
    public async Task<IActionResult> GetBalance(
        string userId,
        [FromQuery] string projectTokenConfigId,
        [FromQuery] TokenHolderClass holderClass,
        CancellationToken ct)
    {
        var result = await _tokenBalanceService.GetBalanceAsync(userId, projectTokenConfigId, holderClass, ct);
        return ToActionResult(result);
    }

    [HttpGet("{userId}/arda")]
    public async Task<IActionResult> GetArdaBalance(string userId, CancellationToken ct)
    {
        var result = await _tokenBalanceService.GetArdaBalanceAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{userId}/portfolio")]
    public async Task<IActionResult> GetPortfolio(string userId, CancellationToken ct)
    {
        var result = await _tokenBalanceService.GetPortfolioAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{userId}/liquidity")]
    public async Task<IActionResult> IsBalanceLiquid(
        string userId,
        [FromQuery] string projectTokenConfigId,
        [FromQuery] TokenHolderClass holderClass,
        CancellationToken ct)
    {
        var result = await _tokenBalanceService.IsBalanceLiquidAsync(userId, projectTokenConfigId, holderClass, ct);
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

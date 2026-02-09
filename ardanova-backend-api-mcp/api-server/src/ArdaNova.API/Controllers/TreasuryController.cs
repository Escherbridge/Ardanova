namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TreasuryController : ControllerBase
{
    private readonly ITreasuryService _treasuryService;
    private readonly IExchangeService _exchangeService;

    public TreasuryController(
        ITreasuryService treasuryService,
        IExchangeService exchangeService)
    {
        _treasuryService = treasuryService;
        _exchangeService = exchangeService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await _treasuryService.GetStatusAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var result = await _treasuryService.GetTransactionHistoryAsync(limit, ct);
        return ToActionResult(result);
    }

    [HttpPost("funding-inflow")]
    public async Task<IActionResult> ProcessFundingInflow(
        [FromQuery] double usdAmount,
        [FromQuery] string? projectId,
        CancellationToken ct)
    {
        var result = await _treasuryService.ProcessFundingInflowAsync(usdAmount, projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost("apply-index-return")]
    public async Task<IActionResult> ApplyIndexFundReturn(CancellationToken ct)
    {
        var result = await _treasuryService.ApplyIndexFundReturnAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost("rebalance")]
    public async Task<IActionResult> Rebalance([FromQuery] double requiredLiquid, CancellationToken ct)
    {
        var result = await _treasuryService.RebalanceIfNeededAsync(requiredLiquid, ct);
        return ToActionResult(result);
    }

    [HttpPost("reconcile")]
    public async Task<IActionResult> Reconcile(CancellationToken ct)
    {
        var result = await _treasuryService.ReconcileAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("exchange/treasury-status")]
    public async Task<IActionResult> GetTreasuryStatusFromExchange(CancellationToken ct)
    {
        var result = await _exchangeService.GetTreasuryStatusAsync(ct);
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

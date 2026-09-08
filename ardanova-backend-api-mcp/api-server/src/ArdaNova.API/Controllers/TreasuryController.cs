namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TreasuryController : ControllerBase
{
    private readonly ITreasuryService _treasuryService;
    private readonly IExchangeService _exchangeService;
    private readonly IProjectBuyBackReadModel? _projectBuyBack;

    /// <summary>Shown when the wired treasury service cannot answer a project-scoped read.</summary>
    private const string ProjectReadUnavailable =
        "Project-level treasury reads are not available on this deployment.";

    /// <param name="projectBuyBack">
    /// The project-scoped reads, registered off the SAME scoped treasury instance. Optional so
    /// a deployment wired to a different implementation gets an honest 503 rather than a wrong
    /// number, and so the type still constructs without a container.
    /// </param>
    public TreasuryController(
        ITreasuryService treasuryService,
        IExchangeService exchangeService,
        IProjectBuyBackReadModel? projectBuyBack = null)
    {
        _treasuryService = treasuryService;
        _exchangeService = exchangeService;
        _projectBuyBack = projectBuyBack;
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
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> ProcessFundingInflow(
        [FromQuery] double usdAmount,
        [FromQuery] string? projectId,
        CancellationToken ct)
    {
        var result = await _treasuryService.ProcessFundingInflowAsync(usdAmount, projectId, ct);
        return ToActionResult(result);
    }

    [HttpPost("apply-index-return")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> ApplyIndexFundReturn(CancellationToken ct)
    {
        var result = await _treasuryService.ApplyIndexFundReturnAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost("rebalance")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> Rebalance([FromQuery] double requiredLiquid, CancellationToken ct)
    {
        var result = await _treasuryService.RebalanceIfNeededAsync(requiredLiquid, ct);
        return ToActionResult(result);
    }

    [HttpPost("reconcile")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> Reconcile(CancellationToken ct)
    {
        var result = await _treasuryService.ReconcileAsync(ct);
        return ToActionResult(result);
    }

    /// <summary>One project's balance of record, with the platform's policy split applied to it.</summary>
    [HttpGet("projects/{projectId}/status")]
    public async Task<IActionResult> GetProjectStatus(string projectId, CancellationToken ct)
    {
        if (_projectBuyBack is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ProjectReadUnavailable });

        var result = await _projectBuyBack.GetProjectStatusAsync(projectId, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// What this project can pay for its own units right now, and the disclosure that says
    /// where that money comes from. The node's buy-back policy enforces these figures.
    /// </summary>
    [HttpGet("projects/{projectId}/buy-back")]
    public async Task<IActionResult> GetProjectBuyBackFigures(string projectId, CancellationToken ct)
    {
        if (_projectBuyBack is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ProjectReadUnavailable });

        var result = await _projectBuyBack.GetBuyBackFiguresAsync(projectId, ct);
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
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

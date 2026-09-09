namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.API.Middleware;
using ArdaNova.Domain.Models.Enums;
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
    /// <remarks>
    /// Scoped inside <see cref="TreasuryService.GetBuyBackFiguresAsync"/> from the verified
    /// <see cref="Viewer"/> this route passes, never from anything the client sends. A project
    /// this reader may not see answers NotFound, and membership branches the PAYLOAD: a
    /// non-member is quoted the declared terms with no read of the project's position at all.
    /// Nothing this route returns carries the node account those funds sit in (OI-89).
    /// </remarks>
    [HttpGet("projects/{projectId}/buy-back")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetProjectBuyBackFigures(string projectId, CancellationToken ct)
    {
        if (_projectBuyBack is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ProjectReadUnavailable });

        var result = await _projectBuyBack.GetBuyBackFiguresAsync(projectId, Viewer, ct);
        return ToActionResult(result);
    }

    [HttpGet("exchange/treasury-status")]
    public async Task<IActionResult> GetTreasuryStatusFromExchange(CancellationToken ct)
    {
        var result = await _exchangeService.GetTreasuryStatusAsync(ct);
        return ToActionResult(result);
    }

    /// <summary>The signed-in person, from the verified actor assertion. The only identity these routes trust.</summary>
    /// <remarks>
    /// Built the same way <c>ProjectsController.Viewer</c> is, so a project's buy-back terms are
    /// scoped by exactly the rule its own detail page is scoped by. An unverified request reads
    /// as the anonymous public, which the route's ActorAssertion policy already refuses.
    /// </remarks>
    private ProjectViewer Viewer => ActorAssertionMiddleware.TryGetActorId(User, out var actorId)
        ? new ProjectViewer(actorId, User.IsInRole(UserRole.ADMIN.ToString()))
        : ProjectViewer.Anonymous;

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            // The refusal carries a sentence a person can act on, so it has to reach them;
            // Forbid() would discard it. The ProjectLedgerController precedent.
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

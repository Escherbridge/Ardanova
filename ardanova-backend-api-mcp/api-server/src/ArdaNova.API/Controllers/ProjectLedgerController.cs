namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>One project's money, in the words a sponsor uses.</summary>
/// <remarks>
/// The balance is the project's own funds account as the node reports it; the entries only
/// explain it. The rules that keep those two apart live on <see cref="ProjectLedgerService"/>.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class ProjectLedgerController : ControllerBase
{
    private readonly IProjectLedgerService _projectLedger;

    /// <param name="registeredProjectLedger">
    /// The container's implementation, registered in
    /// <c>ArdaNova.Application/DependencyInjection.cs</c> next to the treasury services. That
    /// registration now exists, so this is the path every request actually takes; the hand
    /// composition below is only the fallback that keeps the route serving — rather than
    /// 503-ing — if the registration is ever removed.
    /// </param>
    public ProjectLedgerController(
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IRepository<FundingIntent> fundingIntentRepository,
        IRepository<EconomicSettlement> settlementRepository,
        IRepository<TokenAllocation> allocationRepository,
        IRepository<PayoutRequest> payoutRepository,
        IRepository<PlatformTreasuryTransaction> treasuryReceiptRepository,
        IRepository<ProjectInvestment> investmentRepository,
        IRepository<User> userRepository,
        IHierarchyAuthorizationService hierarchyAuthorization,
        IProjectBuyBackReadModel? projectTreasuryReadModel = null,
        IProjectLedgerService? registeredProjectLedger = null)
    {
        _projectLedger = registeredProjectLedger ?? new ProjectLedgerService(
            projectTokenConfigRepository,
            fundingIntentRepository,
            settlementRepository,
            allocationRepository,
            payoutRepository,
            treasuryReceiptRepository,
            investmentRepository,
            userRepository,
            hierarchyAuthorization,
            projectTreasuryReadModel);
    }

    /// <summary>
    /// What this project holds and what happened to it, as this caller is allowed to read it.
    /// </summary>
    /// <remarks>
    /// What comes back is decided in <see cref="ProjectLedgerService.GetProjectMoneyAsync"/> from
    /// the caller's verified identity, never from anything the client sends - and membership
    /// branches the PAYLOAD, not a name field. A project member receives the itemised movements;
    /// every other signed-in reader receives aggregate figures with an EMPTY entry list, so the
    /// per-sponsor amounts, release dates, award sizes and node operation ids never leave the
    /// server rather than being left for a page to not render.
    /// </remarks>
    [HttpGet("{projectId}/money")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetProjectMoney(string projectId, CancellationToken ct)
    {
        var result = await _projectLedger.GetProjectMoneyAsync(projectId, ActorId, ct);
        return ToActionResult(result);
    }

    /// <summary>The signed-in person, from the verified claim. The only identity this route trusts.</summary>
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

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
            // Forbid() would discard it.
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

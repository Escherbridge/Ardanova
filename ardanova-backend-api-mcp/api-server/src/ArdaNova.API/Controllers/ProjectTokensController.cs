namespace ArdaNova.API.Controllers;

using System.Linq;
using System.Security.Claims;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.API.Middleware;
using ArdaNova.Domain.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProjectTokensController : ControllerBase
{
    private readonly IProjectTokenService _projectTokenService;
    private readonly IProjectGateService _projectGateService;
    private readonly IProjectTokenProvisioning _projectTokenProvisioning;
    private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
    private readonly IRepository<ProjectInvestment> _investmentRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;

    /// <param name="projectTokenProvisioning">
    /// Funding setup. Registered in Application/DependencyInjection.cs as the same scoped
    /// instance under its own interface — the IProjectVisibility pattern — so this route
    /// depends on it explicitly instead of casting the service it was handed.
    /// </param>
    /// <param name="projectTokenConfigRepository">
    /// Read-only lookup for GetInvestors: it needs the config's owning project id to decide
    /// membership, a decision <see cref="IProjectTokenService"/> does not expose today.
    /// </param>
    /// <param name="investmentRepository">
    /// Read-only lookup for GetInvestors: it reads the per-pledge consent flag directly off
    /// the entity, since <c>ProjectInvestmentDto</c> does not carry it.
    /// </param>
    /// <param name="userRepository">
    /// Read-only lookup for GetInvestors: resolves a backer's display name for members, so the
    /// route never has to return their internal user id.
    /// </param>
    public ProjectTokensController(
        IProjectTokenService projectTokenService,
        IProjectGateService projectGateService,
        IProjectTokenProvisioning projectTokenProvisioning,
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IRepository<ProjectInvestment> investmentRepository,
        IRepository<User> userRepository,
        IHierarchyAuthorizationService hierarchyAuthorization)
    {
        _projectTokenService = projectTokenService;
        _projectGateService = projectGateService;
        _projectTokenProvisioning = projectTokenProvisioning
            ?? throw new ArgumentNullException(nameof(projectTokenProvisioning));
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _investmentRepository = investmentRepository;
        _userRepository = userRepository;
        _hierarchyAuthorization = hierarchyAuthorization;
    }

    // === Config CRUD ===

    [HttpPost("config")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> CreateConfig([FromBody] CreateProjectTokenConfigDto dto, CancellationToken ct)
    {
        var result = await _projectTokenService.CreateConfigAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetConfig), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("config/{id}")]
    public async Task<IActionResult> GetConfig(string id, CancellationToken ct)
    {
        var result = await _projectTokenService.GetConfigByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("config/metadata/batch")]
    public async Task<IActionResult> GetMetadata(
        [FromBody] ProjectTokenMetadataBatchRequestDto request,
        CancellationToken ct)
    {
        var result = await _projectTokenService.GetMetadataByIdsAsync(request.Ids, ct);
        return ToActionResult(result);
    }

    [HttpGet("config/by-project/{projectId}")]
    public async Task<IActionResult> GetConfigByProject(string projectId, CancellationToken ct)
    {
        var result = await _projectTokenService.GetConfigByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("config/{id}/supply")]
    public async Task<IActionResult> GetSupplyBreakdown(string id, CancellationToken ct)
    {
        var result = await _projectTokenService.GetSupplyBreakdownAsync(id, ct);
        return ToActionResult(result);
    }

    // === Funding setup (self-serve project stake) ===

    /// <summary>
    /// Runs the publish-time funding job for a project the caller owns. Ownership and
    /// identity readiness are decided in the service against the caller's verified id; the
    /// body carries the declaration only, never an owner.
    /// </summary>
    [HttpPost("funding/provision")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ProvisionFunding(
        [FromBody] ProvisionProjectFundingRequest request,
        CancellationToken ct)
    {
        var result = await _projectTokenProvisioning.ProvisionAsync(
            request.ProjectId,
            new ProjectTokenDeclaration
            {
                AssetName = request.AssetName,
                UnitName = request.UnitName,
                TotalSupply = request.TotalSupply,
                ContributorSupply = request.ContributorSupply,
                InvestorSupply = request.InvestorSupply,
                FundingGoal = request.FundingGoal,
                Disclosure = request.Disclosure,
                ReservedPercentage = request.ReservedPercentage,
                SuccessCriteria = request.SuccessCriteria,
            },
            ActorId,
            ct);

        return ToActionResult(result);
    }

    /// <summary>A project's funding setup as a person reads it; owner-only actions are decided
    /// from the caller's verified id, never from a query parameter.</summary>
    [HttpGet("funding/by-project/{projectId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetFunding(string projectId, CancellationToken ct)
    {
        var result = await _projectTokenProvisioning.GetFundingAsync(projectId, ActorId, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // === Allocations ===

    [HttpPost("{configId}/allocate/pbi")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public IActionResult AllocateToPbi(string configId, [FromBody] CreateTokenAllocationDto dto, CancellationToken ct)
    {
        _ = configId;
        _ = dto;
        _ = ct;
        return AllocationUnavailable();
    }

    [HttpPost("{configId}/allocate/investor")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public IActionResult AllocateToInvestor(string configId, [FromBody] CreateInvestorAllocationDto dto, CancellationToken ct)
    {
        _ = configId;
        _ = dto;
        _ = ct;
        return AllocationUnavailable();
    }

    [HttpPost("{configId}/allocate/founder")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public IActionResult AllocateToFounder(string configId, [FromBody] CreateFounderAllocationDto dto, CancellationToken ct)
    {
        _ = configId;
        _ = dto;
        _ = ct;
        return AllocationUnavailable();
    }

    [HttpPost("allocations/{allocationId}/distribute")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public IActionResult Distribute(string allocationId, [FromQuery] string recipientUserId, CancellationToken ct)
    {
        _ = allocationId;
        _ = recipientUserId;
        _ = ct;
        return Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Token distribution is unavailable",
            detail: "Atomic RESERVED-to-DISTRIBUTED allocation transition and durable idempotency are required before token distribution can be enabled.");
    }

    private IActionResult AllocationUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Project-unit allocation is unavailable",
            detail: "Provider-verified account eligibility, durable settlement, and idempotent allocation transitions are required before project-unit allocation can be enabled.");

    [HttpPost("allocations/{allocationId}/revoke")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> Revoke(string allocationId, CancellationToken ct)
    {
        var result = await _projectTokenService.RevokeAllocationAsync(allocationId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{configId}/allocations")]
    public async Task<IActionResult> GetAllocations(string configId, CancellationToken ct)
    {
        var result = await _projectTokenService.GetAllocationsByProjectAsync(configId, ct);
        return ToActionResult(result);
    }

    [HttpGet("allocations/by-pbi/{pbiId}")]
    public async Task<IActionResult> GetAllocationsByPbi(string pbiId, CancellationToken ct)
    {
        var result = await _projectTokenService.GetAllocationsByPbiAsync(pbiId, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// A project's backers, as a person is allowed to see them. Anyone signed in sees proof the
    /// project has support — a headcount and a total — with no per-pledge amount and no name.
    /// Project members (the creator or anyone on the roster, per
    /// <see cref="IHierarchyAuthorizationService.IsProjectMemberAsync"/>) see the itemised
    /// pledges instead, each carrying a display name — never the backer's internal user id —
    /// and that name appears only when the backer opted in via their per-pledge consent; every
    /// other row reads "Anonymous backer".
    /// </summary>
    [HttpGet("{configId}/investors")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetInvestors(string configId, CancellationToken ct)
    {
        var config = await _projectTokenConfigRepository.GetByIdAsync(configId, ct);
        if (config is null)
            return NotFound(new { error = $"Project token config {configId} not found" });

        var investments = await _investmentRepository.FindAsync(
            investment => investment.projectTokenConfigId == configId, ct);

        var isMember = await _hierarchyAuthorization.IsProjectMemberAsync(ActorId, config.projectId, ct);
        if (!isMember)
        {
            return Ok(new ProjectInvestorSummaryDto
            {
                BackerCount = investments.Count,
                TotalAmount = investments.Sum(investment => investment.usdAmount),
            });
        }

        var backerIds = investments.Select(investment => investment.userId).Distinct().ToList();
        var backers = await _userRepository.FindAsync(user => backerIds.Contains(user.id), ct);
        var displayNameByUserId = backers.ToDictionary(user => user.id, user => user.name);

        var lines = investments
            .OrderByDescending(investment => investment.usdAmount)
            .Select(investment => new ProjectInvestorLineDto
            {
                Id = investment.id,
                DisplayName = investment.showBackerName
                    && displayNameByUserId.TryGetValue(investment.userId, out var name)
                    && !string.IsNullOrWhiteSpace(name)
                        ? name!
                        : "Anonymous backer",
                Amount = investment.usdAmount,
                UnitsPledged = investment.tokenAmount,
                InvestedAt = investment.investedAt,
                ProtectionEligible = investment.protectionEligible,
                ProtectionPaidOut = investment.protectionPaidOut,
                ProtectionAmount = investment.protectionPaidOut ? investment.protectionAmount : null,
            })
            .ToList();

        return Ok(lines);
    }

    // === Gate Management ===

    [HttpGet("{configId}/gate")]
    public async Task<IActionResult> GetGateStatus(string configId, CancellationToken ct)
    {
        var result = await _projectGateService.GetGateStatusAsync(configId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/gate/evaluate")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> EvaluateGate1(string configId, CancellationToken ct)
    {
        var result = await _projectGateService.EvaluateGate1Async(configId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/gate/clear")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> ClearGate2(string configId, [FromQuery] string verifiedByUserId, CancellationToken ct)
    {
        var result = await _projectGateService.ClearGate2Async(configId, verifiedByUserId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/gate/fail")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> FailProject(string configId, [FromBody] FailProjectRequest request, CancellationToken ct)
    {
        var result = await _projectGateService.FailProjectAsync(configId, request.Reason, ct);
        return ToActionResult(result);
    }

    // === Failure Handling ===

    [HttpPost("{configId}/burn-founder")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> BurnFounderTokens(string configId, CancellationToken ct)
    {
        var result = await _projectTokenService.BurnFounderTokensAsync(configId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/trust-protection")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> ProcessTrustProtection(string configId, CancellationToken ct)
    {
        var result = await _projectTokenService.ProcessInvestorTrustProtectionAsync(configId, ct);
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
            // Funding setup refuses with a reason a person can act on ("finish verifying your
            // identity"), so the refusal has to reach them. Forbid() would discard it.
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

/// <summary>What a project owner declares at publish. Carries no owner id: the owner is the
/// caller, resolved from the verified actor claim.</summary>
public record ProvisionProjectFundingRequest
{
    public required string ProjectId { get; init; }

    public required string AssetName { get; init; }

    public required string UnitName { get; init; }

    public required int TotalSupply { get; init; }

    public required int ContributorSupply { get; init; }

    public required int InvestorSupply { get; init; }

    public required double FundingGoal { get; init; }

    public required string Disclosure { get; init; }

    public double ReservedPercentage { get; init; }

    public string? SuccessCriteria { get; init; }
}

public record FailProjectRequest
{
    public required string Reason { get; init; }
}

/// <summary>What a non-member sees from GetInvestors: proof the project has backers, with no
/// per-pledge amount and no name a stranger could match to a real person.</summary>
public record ProjectInvestorSummaryDto
{
    public required int BackerCount { get; init; }
    public required double TotalAmount { get; init; }
}

/// <summary>One pledge, as a project member reads it from GetInvestors. Carries a display
/// name — never the backer's internal user id — that reads "Anonymous backer" unless the
/// backer's own per-pledge consent (<see cref="ProjectInvestment.showBackerName"/>) opted in.</summary>
public record ProjectInvestorLineDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required double Amount { get; init; }
    public required int UnitsPledged { get; init; }
    public required DateTime InvestedAt { get; init; }
    public required bool ProtectionEligible { get; init; }
    public required bool ProtectionPaidOut { get; init; }
    public double? ProtectionAmount { get; init; }
}

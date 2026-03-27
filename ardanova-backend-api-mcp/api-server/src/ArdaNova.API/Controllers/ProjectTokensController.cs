namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProjectTokensController : ControllerBase
{
    private readonly IProjectTokenService _projectTokenService;
    private readonly IProjectGateService _projectGateService;

    public ProjectTokensController(
        IProjectTokenService projectTokenService,
        IProjectGateService projectGateService)
    {
        _projectTokenService = projectTokenService;
        _projectGateService = projectGateService;
    }

    // === Config CRUD ===

    [HttpPost("config")]
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

    // === Allocations ===

    [HttpPost("{configId}/allocate/task")]
    public async Task<IActionResult> AllocateToTask(string configId, [FromBody] CreateTokenAllocationDto dto, CancellationToken ct)
    {
        var result = await _projectTokenService.AllocateToTaskAsync(configId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/allocate/investor")]
    public async Task<IActionResult> AllocateToInvestor(string configId, [FromBody] CreateInvestorAllocationDto dto, CancellationToken ct)
    {
        var result = await _projectTokenService.AllocateToInvestorAsync(configId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/allocate/founder")]
    public async Task<IActionResult> AllocateToFounder(string configId, [FromBody] CreateFounderAllocationDto dto, CancellationToken ct)
    {
        var result = await _projectTokenService.AllocateToFounderAsync(configId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("allocations/{allocationId}/distribute")]
    public async Task<IActionResult> Distribute(string allocationId, [FromQuery] string recipientUserId, CancellationToken ct)
    {
        var result = await _projectTokenService.DistributeAsync(allocationId, recipientUserId, ct);
        return ToActionResult(result);
    }

    [HttpPost("allocations/{allocationId}/revoke")]
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

    [HttpGet("allocations/by-task/{taskId}")]
    public async Task<IActionResult> GetAllocationsByTask(string taskId, CancellationToken ct)
    {
        var result = await _projectTokenService.GetAllocationsByTaskAsync(taskId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{configId}/investors")]
    public async Task<IActionResult> GetInvestors(string configId, CancellationToken ct)
    {
        var result = await _projectTokenService.GetInvestorsByProjectAsync(configId, ct);
        return ToActionResult(result);
    }

    // === Gate Management ===

    [HttpGet("{configId}/gate")]
    public async Task<IActionResult> GetGateStatus(string configId, CancellationToken ct)
    {
        var result = await _projectGateService.GetGateStatusAsync(configId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/gate/evaluate")]
    public async Task<IActionResult> EvaluateGate1(string configId, CancellationToken ct)
    {
        var result = await _projectGateService.EvaluateGate1Async(configId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/gate/clear")]
    public async Task<IActionResult> ClearGate2(string configId, [FromQuery] string verifiedByUserId, CancellationToken ct)
    {
        var result = await _projectGateService.ClearGate2Async(configId, verifiedByUserId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/gate/fail")]
    public async Task<IActionResult> FailProject(string configId, [FromBody] FailProjectRequest request, CancellationToken ct)
    {
        var result = await _projectGateService.FailProjectAsync(configId, request.Reason, ct);
        return ToActionResult(result);
    }

    // === Failure Handling ===

    [HttpPost("{configId}/burn-founder")]
    public async Task<IActionResult> BurnFounderTokens(string configId, CancellationToken ct)
    {
        var result = await _projectTokenService.BurnFounderTokensAsync(configId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{configId}/trust-protection")]
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
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

public record FailProjectRequest
{
    public required string Reason { get; init; }
}

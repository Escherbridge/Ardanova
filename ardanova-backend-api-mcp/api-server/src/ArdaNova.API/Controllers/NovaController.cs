namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs.Nova;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>Actor-bound Nova AI endpoints; the only path from ArdaNova to the upstream validator (see Infrastructure/Ogai/AGENTS.md).</summary>
[ApiController]
[Route("api/nova")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public sealed class NovaController : ControllerBase
{
    private readonly INovaOrchestrationService _nova;

    public NovaController(INovaOrchestrationService nova)
    {
        _nova = nova;
    }

    /// <summary>One project-refinement turn for the signed actor: the next question, or the feasibility verdict.</summary>
    [HttpPost("project-refinement")]
    public async Task<IActionResult> RefineProject([FromBody] NovaRefineRequestDto request, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _nova.RefineProjectIdeaAsync(request, actorId, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Type switch
            {
                ResultType.ValidationError or ResultType.BadRequest => BadRequest(new { error = result.Error }),
                ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
                ResultType.Conflict => StatusCode(StatusCodes.Status429TooManyRequests, new { error = result.Error }),
                _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error }),
            };
    }
}

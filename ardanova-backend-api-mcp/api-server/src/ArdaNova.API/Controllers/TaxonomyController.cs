namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>Same auth posture as <see cref="EnumsController"/>: API-key authenticated, no role gate.</summary>
[ApiController]
[Route("api/[controller]")]
public class TaxonomyController : ControllerBase
{
    private readonly ITaxonomyService _taxonomyService;

    public TaxonomyController(ITaxonomyService taxonomyService)
    {
        _taxonomyService = taxonomyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? kind, CancellationToken ct)
    {
        var result = await _taxonomyService.GetActiveTermsAsync(kind, ct);
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

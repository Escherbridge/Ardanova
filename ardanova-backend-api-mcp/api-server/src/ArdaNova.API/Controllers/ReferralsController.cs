namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referralService;

    public ReferralsController(IReferralService referralService)
    {
        _referralService = referralService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _referralService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("referrer/{referrerId:guid}")]
    public async Task<IActionResult> GetByReferrerId(Guid referrerId, CancellationToken ct)
    {
        var result = await _referralService.GetByReferrerIdAsync(referrerId, ct);
        return ToActionResult(result);
    }

    [HttpGet("referred/{referredId:guid}")]
    public async Task<IActionResult> GetByReferredId(Guid referredId, CancellationToken ct)
    {
        var result = await _referralService.GetByReferredIdAsync(referredId, ct);
        return ToActionResult(result);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var result = await _referralService.GetByCodeAsync(code, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReferralDto dto, CancellationToken ct)
    {
        var result = await _referralService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _referralService.CompleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> ClaimReward(Guid id, [FromBody] ClaimReferralRewardDto dto, CancellationToken ct)
    {
        var result = await _referralService.ClaimRewardAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/expire")]
    public async Task<IActionResult> Expire(Guid id, CancellationToken ct)
    {
        var result = await _referralService.ExpireAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _referralService.CancelAsync(id, ct);
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

namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class KycController : ControllerBase
{
    private readonly IKycService _kycService;

    public KycController(IKycService kycService)
    {
        _kycService = kycService;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitKycDto dto, CancellationToken ct)
    {
        var result = await _kycService.SubmitAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("status/{userId}")]
    public async Task<IActionResult> GetStatus(string userId, CancellationToken ct)
    {
        var result = await _kycService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _kycService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await _kycService.GetPendingAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, [FromBody] ReviewKycDto dto, CancellationToken ct)
    {
        var result = await _kycService.ApproveAsync(id, dto.ReviewerId, dto.ReviewNotes, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] ReviewKycDto dto, CancellationToken ct)
    {
        var result = await _kycService.RejectAsync(id, dto.ReviewerId, dto.ReviewNotes, dto.RejectionReason, ct);
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
            ResultType.Forbidden => StatusCode(403, new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/features")]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureService _featureService;

    public FeaturesController(IFeatureService featureService)
    {
        _featureService = featureService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _featureService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeatureDto dto, CancellationToken ct)
    {
        var result = await _featureService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateFeatureDto dto, CancellationToken ct)
    {
        var result = await _featureService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _featureService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPut("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignFeatureDto dto, CancellationToken ct)
    {
        var result = await _featureService.AssignAsync(id, dto.UserId, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateFeatureStatusDto dto, CancellationToken ct)
    {
        var result = await _featureService.UpdateStatusAsync(id, dto.Status, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdateFeaturePriorityDto dto, CancellationToken ct)
    {
        var result = await _featureService.UpdatePriorityAsync(id, dto.Priority, ct);
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

// Helper DTOs for endpoints
public record AssignFeatureDto
{
    public string? UserId { get; init; }
}

public record UpdateFeatureStatusDto
{
    public FeatureStatus Status { get; init; }
}

public record UpdateFeaturePriorityDto
{
    public Priority Priority { get; init; }
}

// Nested controller for sprint features
[ApiController]
[Route("api/sprints/{sprintId}/features")]
public class SprintFeaturesController : ControllerBase
{
    private readonly IFeatureService _featureService;

    public SprintFeaturesController(IFeatureService featureService)
    {
        _featureService = featureService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBySprintId(string sprintId, CancellationToken ct)
    {
        var result = await _featureService.GetBySprintIdAsync(sprintId, ct);
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

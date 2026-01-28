namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _opportunityService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _opportunityService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] OpportunityType? type,
        [FromQuery] OpportunityStatus? status,
        [FromQuery] ExperienceLevel? experienceLevel,
        [FromQuery] string? skills,
        [FromQuery] string? sourceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _opportunityService.SearchAsync(searchTerm, type, status, experienceLevel, skills, sourceType, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _opportunityService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _opportunityService.GetBySlugAsync(slug, ct);
        return ToActionResult(result);
    }

    [HttpGet("poster/{posterId}")]
    public async Task<IActionResult> GetByPosterId(string posterId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByPosterIdAsync(posterId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}")]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("shop/{shopId}")]
    public async Task<IActionResult> GetByShopId(string shopId, CancellationToken ct)
    {
        var result = await _opportunityService.GetByShopIdAsync(shopId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto, CancellationToken ct)
    {
        var result = await _opportunityService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOpportunityDto dto, CancellationToken ct)
    {
        var result = await _opportunityService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> Close(string id, CancellationToken ct)
    {
        var result = await _opportunityService.CloseAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _opportunityService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/apply")]
    public async Task<IActionResult> Apply(string id, [FromBody] ApplyToOpportunityDto dto, CancellationToken ct)
    {
        var result = await _opportunityService.ApplyAsync(id, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("{id}/applications")]
    public async Task<IActionResult> GetApplications(string id, CancellationToken ct)
    {
        var result = await _opportunityService.GetApplicationsAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("applications/{applicationId}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(string applicationId, [FromBody] UpdateApplicationStatusDto dto, CancellationToken ct)
    {
        var result = await _opportunityService.UpdateApplicationStatusAsync(applicationId, dto, ct);
        return ToActionResult(result);
    }

    // ===== Updates =====

    [HttpGet("{opportunityId}/updates")]
    public async Task<IActionResult> GetUpdates(string opportunityId, CancellationToken ct)
    {
        var result = await _opportunityService.GetUpdatesAsync(opportunityId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{opportunityId}/updates")]
    public async Task<IActionResult> CreateUpdate(string opportunityId, [FromBody] CreateOpportunityUpdateDto dto, CancellationToken ct)
    {
        var dtoWithId = dto with { OpportunityId = opportunityId };
        var result = await _opportunityService.CreateUpdateAsync(dtoWithId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = opportunityId }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("updates/{updateId}")]
    public async Task<IActionResult> DeleteUpdate(string updateId, CancellationToken ct)
    {
        var result = await _opportunityService.DeleteUpdateAsync(updateId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== Comments =====

    [HttpGet("{opportunityId}/comments")]
    public async Task<IActionResult> GetComments(string opportunityId, CancellationToken ct)
    {
        var result = await _opportunityService.GetCommentsAsync(opportunityId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{opportunityId}/comments")]
    public async Task<IActionResult> AddComment(string opportunityId, [FromBody] CreateOpportunityCommentDto dto, CancellationToken ct)
    {
        var dtoWithId = dto with { OpportunityId = opportunityId };
        var result = await _opportunityService.AddCommentAsync(dtoWithId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = opportunityId }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string commentId, CancellationToken ct)
    {
        var result = await _opportunityService.DeleteCommentAsync(commentId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
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

namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AgenciesController : ControllerBase
{
    private readonly IAgencyService _agencyService;

    public AgenciesController(IAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _agencyService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _agencyService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _agencyService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _agencyService.GetBySlugAsync(slug, ct);
        return ToActionResult(result);
    }

    [HttpGet("owner/{ownerId:guid}")]
    public async Task<IActionResult> GetByOwnerId(Guid ownerId, CancellationToken ct)
    {
        var result = await _agencyService.GetByOwnerIdAsync(ownerId, ct);
        return ToActionResult(result);
    }

    [HttpGet("verified")]
    public async Task<IActionResult> GetVerified(CancellationToken ct)
    {
        var result = await _agencyService.GetVerifiedAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgencyDto dto, CancellationToken ct)
    {
        var result = await _agencyService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgencyDto dto, CancellationToken ct)
    {
        var result = await _agencyService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _agencyService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var result = await _agencyService.VerifyAsync(id, ct);
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

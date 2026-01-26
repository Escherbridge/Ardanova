namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _eventService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _eventService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] EventType? type,
        [FromQuery] EventStatus? status,
        [FromQuery] bool? upcoming,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _eventService.SearchAsync(searchTerm, type, status, upcoming, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _eventService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _eventService.GetBySlugAsync(slug, ct);
        return ToActionResult(result);
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _eventService.GetUpcomingAsync(limit, ct);
        return ToActionResult(result);
    }

    [HttpGet("organizer/{organizerId}")]
    public async Task<IActionResult> GetByOrganizerId(string organizerId, CancellationToken ct)
    {
        var result = await _eventService.GetByOrganizerIdAsync(organizerId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/registered")]
    public async Task<IActionResult> GetRegisteredByUserId(string userId, CancellationToken ct)
    {
        var result = await _eventService.GetRegisteredByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto, CancellationToken ct)
    {
        var result = await _eventService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateEventDto dto, CancellationToken ct)
    {
        var result = await _eventService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _eventService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/register")]
    public async Task<IActionResult> Register(string id, [FromBody] RegisterEventDto dto, CancellationToken ct)
    {
        var result = await _eventService.RegisterAsync(id, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{id}/register")]
    public async Task<IActionResult> Unregister(string id, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _eventService.UnregisterAsync(id, userId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpGet("{id}/attendees")]
    public async Task<IActionResult> GetAttendees(string id, CancellationToken ct)
    {
        var result = await _eventService.GetAttendeesAsync(id, ct);
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

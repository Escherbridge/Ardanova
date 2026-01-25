namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _notificationService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _notificationService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/paged")]
    public async Task<IActionResult> GetByUserIdPaged(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _notificationService.GetByUserIdPagedAsync(userId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/unread")]
    public async Task<IActionResult> GetUnreadByUserId(string userId, CancellationToken ct)
    {
        var result = await _notificationService.GetUnreadByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/summary")]
    public async Task<IActionResult> GetSummary(string userId, CancellationToken ct)
    {
        var result = await _notificationService.GetSummaryAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto, CancellationToken ct)
    {
        var result = await _notificationService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id, CancellationToken ct)
    {
        var result = await _notificationService.MarkAsReadAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("user/{userId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(string userId, CancellationToken ct)
    {
        var result = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _notificationService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> DeleteAllByUserId(string userId, CancellationToken ct)
    {
        var result = await _notificationService.DeleteAllByUserIdAsync(userId, ct);
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

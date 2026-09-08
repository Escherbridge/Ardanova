namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
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
        if (!result.IsSuccess)
            return ToActionResult(result);

        return IsActorNotification(result.Value!)
            ? Ok(result.Value)
            : Forbid();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _notificationService.GetByUserIdAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("me/paged")]
    public async Task<IActionResult> GetMinePaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _notificationService.GetByUserIdPagedAsync(ActorId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("me/unread")]
    public async Task<IActionResult> GetUnreadMine(CancellationToken ct)
    {
        var result = await _notificationService.GetUnreadByUserIdAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpGet("me/summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _notificationService.GetSummaryAsync(ActorId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateNotificationDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Public notification creation is unavailable",
            detail: "Notifications are created by trusted application workflows, not public requests.");
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id, CancellationToken ct)
    {
        var existing = await _notificationService.GetByIdAsync(id, ct);
        if (!existing.IsSuccess)
            return ToActionResult(existing);

        if (!IsActorNotification(existing.Value!))
            return Forbid();

        var result = await _notificationService.MarkAsReadAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("me/read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var result = await _notificationService.MarkAllAsReadAsync(ActorId, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var existing = await _notificationService.GetByIdAsync(id, ct);
        if (!existing.IsSuccess)
            return ToActionResult(existing);

        if (!IsActorNotification(existing.Value!))
            return Forbid();

        var result = await _notificationService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAllMine(CancellationToken ct)
    {
        var result = await _notificationService.DeleteAllByUserIdAsync(ActorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsActorNotification(NotificationDto notification)
        => string.Equals(notification.UserId, ActorId, StringComparison.Ordinal);

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

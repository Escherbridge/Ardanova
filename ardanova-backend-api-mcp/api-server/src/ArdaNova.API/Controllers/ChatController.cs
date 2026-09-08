namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // ========== Conversation Endpoints ==========

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        _ = type;
        var result = await _chatService.GetUserConversationsAsync(ActorId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(
        string conversationId,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetConversationByIdAsync(conversationId, ActorId, ct);
        return ToActionResult(result);
    }

    [HttpPost("conversations/direct")]
    public async Task<IActionResult> GetOrCreateDirect(
        [FromBody] CreateDirectConversationDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetOrCreateDirectConversationAsync(ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("conversations/group")]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupConversationDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.CreateGroupConversationAsync(ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPut("conversations/{conversationId}")]
    public async Task<IActionResult> UpdateGroup(
        string conversationId,
        [FromBody] UpdateGroupConversationDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.UpdateGroupConversationAsync(conversationId, ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("conversations/{conversationId}/members")]
    public async Task<IActionResult> AddMember(
        string conversationId,
        [FromBody] AddConversationMemberDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.AddMemberAsync(conversationId, ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("conversations/{conversationId}/members/{memberUserId}")]
    public async Task<IActionResult> RemoveMember(
        string conversationId,
        string memberUserId,
        CancellationToken ct = default)
    {
        var result = await _chatService.RemoveMemberAsync(conversationId, memberUserId, ActorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("conversations/{conversationId}/leave")]
    public async Task<IActionResult> Leave(
        string conversationId,
        CancellationToken ct = default)
    {
        var result = await _chatService.LeaveConversationAsync(conversationId, ActorId, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    // ========== Message Endpoints ==========

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetMessagesAsync(conversationId, ActorId, limit, cursor, ct);
        return ToActionResult(result);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.SendMessageAsync(ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> UpdateMessage(
        string messageId,
        [FromBody] UpdateMessageDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.UpdateMessageAsync(messageId, ActorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(
        string messageId,
        CancellationToken ct = default)
    {
        var result = await _chatService.DeleteMessageAsync(messageId, ActorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("messages/read")]
    public async Task<IActionResult> MarkAsRead(
        [FromBody] MarkMessagesReadDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.MarkMessagesReadAsync(ActorId, dto, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    [HttpPost("typing")]
    public async Task<IActionResult> SendTypingIndicator(
        [FromBody] TypingIndicatorDto dto,
        CancellationToken ct = default)
    {
        var actorDto = dto with { UserId = ActorId, UserName = null };
        var result = await _chatService.SendTypingIndicatorAsync(ActorId, actorDto, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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

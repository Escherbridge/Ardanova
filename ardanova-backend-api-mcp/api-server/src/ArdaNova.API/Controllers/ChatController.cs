namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
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
        [FromQuery] string userId,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetUserConversationsAsync(userId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(
        string conversationId,
        [FromQuery] string userId,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetConversationByIdAsync(conversationId, userId, ct);
        return ToActionResult(result);
    }

    [HttpPost("conversations/direct")]
    public async Task<IActionResult> GetOrCreateDirect(
        [FromQuery] string userId,
        [FromBody] CreateDirectConversationDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetOrCreateDirectConversationAsync(userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("conversations/group")]
    public async Task<IActionResult> CreateGroup(
        [FromQuery] string userId,
        [FromBody] CreateGroupConversationDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.CreateGroupConversationAsync(userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPut("conversations/{conversationId}")]
    public async Task<IActionResult> UpdateGroup(
        string conversationId,
        [FromQuery] string userId,
        [FromBody] UpdateGroupConversationDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.UpdateGroupConversationAsync(conversationId, userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("conversations/{conversationId}/members")]
    public async Task<IActionResult> AddMember(
        string conversationId,
        [FromQuery] string userId,
        [FromBody] AddConversationMemberDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.AddMemberAsync(conversationId, userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("conversations/{conversationId}/members/{memberUserId}")]
    public async Task<IActionResult> RemoveMember(
        string conversationId,
        string memberUserId,
        [FromQuery] string userId,
        CancellationToken ct = default)
    {
        var result = await _chatService.RemoveMemberAsync(conversationId, memberUserId, userId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("conversations/{conversationId}/leave")]
    public async Task<IActionResult> Leave(
        string conversationId,
        [FromQuery] string userId,
        CancellationToken ct = default)
    {
        var result = await _chatService.LeaveConversationAsync(conversationId, userId, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    // ========== Message Endpoints ==========

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        [FromQuery] string userId,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var result = await _chatService.GetMessagesAsync(conversationId, userId, limit, cursor, ct);
        return ToActionResult(result);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(
        [FromQuery] string userId,
        [FromBody] SendMessageDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.SendMessageAsync(userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> UpdateMessage(
        string messageId,
        [FromQuery] string userId,
        [FromBody] UpdateMessageDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.UpdateMessageAsync(messageId, userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(
        string messageId,
        [FromQuery] string userId,
        CancellationToken ct = default)
    {
        var result = await _chatService.DeleteMessageAsync(messageId, userId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("messages/read")]
    public async Task<IActionResult> MarkAsRead(
        [FromQuery] string userId,
        [FromBody] MarkMessagesReadDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.MarkMessagesReadAsync(userId, dto, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
    }

    [HttpPost("typing")]
    public async Task<IActionResult> SendTypingIndicator(
        [FromQuery] string userId,
        [FromBody] TypingIndicatorDto dto,
        CancellationToken ct = default)
    {
        var result = await _chatService.SendTypingIndicatorAsync(userId, dto, ct);
        return result.IsSuccess ? Ok(new { success = true }) : ToActionResult(result);
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

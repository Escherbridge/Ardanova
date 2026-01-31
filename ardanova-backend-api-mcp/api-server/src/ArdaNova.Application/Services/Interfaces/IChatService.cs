namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IChatService
{
    // ========== Conversation Operations ==========

    Task<Result<ConversationDto>> GetConversationByIdAsync(
        string conversationId,
        string requestingUserId,
        CancellationToken ct = default);

    Task<Result<ConversationListDto>> GetUserConversationsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<Result<ConversationDto>> GetOrCreateDirectConversationAsync(
        string userId,
        CreateDirectConversationDto dto,
        CancellationToken ct = default);

    Task<Result<ConversationDto>> CreateGroupConversationAsync(
        string creatorUserId,
        CreateGroupConversationDto dto,
        CancellationToken ct = default);

    Task<Result<ConversationDto>> UpdateGroupConversationAsync(
        string conversationId,
        string requestingUserId,
        UpdateGroupConversationDto dto,
        CancellationToken ct = default);

    Task<Result<ConversationMemberDto>> AddMemberAsync(
        string conversationId,
        string requestingUserId,
        AddConversationMemberDto dto,
        CancellationToken ct = default);

    Task<Result<bool>> RemoveMemberAsync(
        string conversationId,
        string memberUserId,
        string requestingUserId,
        CancellationToken ct = default);

    Task<Result<bool>> LeaveConversationAsync(
        string conversationId,
        string userId,
        CancellationToken ct = default);

    // ========== Message Operations ==========

    Task<Result<MessageListDto>> GetMessagesAsync(
        string conversationId,
        string requestingUserId,
        int limit = 50,
        string? beforeCursor = null,
        CancellationToken ct = default);

    Task<Result<ChatMessageDto>> SendMessageAsync(
        string userId,
        SendMessageDto dto,
        CancellationToken ct = default);

    Task<Result<ChatMessageDto>> UpdateMessageAsync(
        string messageId,
        string requestingUserId,
        UpdateMessageDto dto,
        CancellationToken ct = default);

    Task<Result<bool>> DeleteMessageAsync(
        string messageId,
        string requestingUserId,
        CancellationToken ct = default);

    Task<Result<bool>> MarkMessagesDeliveredAsync(
        string conversationId,
        string userId,
        CancellationToken ct = default);

    Task<Result<bool>> MarkMessagesReadAsync(
        string userId,
        MarkMessagesReadDto dto,
        CancellationToken ct = default);

    // ========== Presence Operations ==========

    Task<Result<bool>> UpdatePresenceAsync(
        string conversationId,
        string userId,
        CancellationToken ct = default);

    Task<Result<bool>> SendTypingIndicatorAsync(
        string userId,
        TypingIndicatorDto dto,
        CancellationToken ct = default);
}

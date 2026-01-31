namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

// ========== Conversation DTOs ==========

public record ConversationDto
{
    public required string Id { get; init; }
    public required ConversationType Type { get; init; }
    public string? Name { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public IReadOnlyList<ConversationMemberDto> Members { get; init; } = [];
    public ChatMessageDto? LastMessage { get; init; }
    public int UnreadCount { get; init; }
}

public record ConversationMemberDto
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public string? UserImage { get; init; }
    public required ConversationRole Role { get; init; }
    public DateTime? LastReadAt { get; init; }
    public DateTime? LastActiveAt { get; init; }
    public DateTime JoinedAt { get; init; }
    public bool IsOnline { get; init; }
}

public record CreateDirectConversationDto
{
    public required string ParticipantUserId { get; init; }
}

public record CreateGroupConversationDto
{
    public required string Name { get; init; }
    public string? AvatarUrl { get; init; }
    public required IReadOnlyList<string> MemberUserIds { get; init; }
}

public record UpdateGroupConversationDto
{
    public string? Name { get; init; }
    public string? AvatarUrl { get; init; }
}

public record AddConversationMemberDto
{
    public required string UserId { get; init; }
    public ConversationRole Role { get; init; } = ConversationRole.MEMBER;
}

// ========== Message DTOs ==========

public record ChatMessageDto
{
    public required string Id { get; init; }
    public required string ConversationId { get; init; }
    public required string UserFromId { get; init; }
    public required string UserFromName { get; init; }
    public string? UserFromImage { get; init; }
    public string? Message { get; init; }
    public required MessageStatus Status { get; init; }
    public string? ReplyToId { get; init; }
    public ChatMessageDto? ReplyTo { get; init; }
    public DateTime SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? SeenAt { get; init; }
    public DateTime? EditedAt { get; init; }
    public bool IsDeleted { get; init; }
}

public record SendMessageDto
{
    public required string ConversationId { get; init; }
    public required string Message { get; init; }
    public string? ReplyToId { get; init; }
}

public record UpdateMessageDto
{
    public required string Message { get; init; }
}

public record MarkMessagesReadDto
{
    public required string ConversationId { get; init; }
    public DateTime ReadUpTo { get; init; }
}

// ========== Typing Indicator DTO ==========

public record TypingIndicatorDto
{
    public required string ConversationId { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public required bool IsTyping { get; init; }
}

// ========== Paged Results ==========

public record ConversationListDto
{
    public IReadOnlyList<ConversationDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}

public record MessageListDto
{
    public IReadOnlyList<ChatMessageDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
    public string? NextCursor { get; init; }
}

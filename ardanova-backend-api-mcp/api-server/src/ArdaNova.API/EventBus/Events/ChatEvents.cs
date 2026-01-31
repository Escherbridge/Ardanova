using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

/// <summary>
/// Event raised when a chat message is sent.
/// </summary>
public sealed record ChatMessageSentEvent(
    string MessageId,
    string ConversationId,
    string SenderId,
    string SenderName,
    string Content,
    DateTime SentAt,
    string? SenderImage = null,
    string? ReplyToId = null
) : DomainEvent
{
    public override string EventType => "chat.message_sent";
}

/// <summary>
/// Event raised when a chat message is delivered to a user.
/// </summary>
public sealed record ChatMessageDeliveredEvent(
    string MessageId,
    string ConversationId,
    string UserId,
    DateTime DeliveredAt
) : DomainEvent
{
    public override string EventType => "chat.message_delivered";
}

/// <summary>
/// Event raised when a user reads messages in a conversation.
/// </summary>
public sealed record ChatMessageReadEvent(
    string ConversationId,
    string UserId,
    string LastReadMessageId,
    DateTime ReadAt
) : DomainEvent
{
    public override string EventType => "chat.message_read";
}

/// <summary>
/// Event raised when a user starts or stops typing in a conversation.
/// </summary>
public sealed record ChatTypingEvent(
    string ConversationId,
    string UserId,
    string UserName,
    bool IsTyping
) : DomainEvent
{
    public override string EventType => "chat.typing";
}

/// <summary>
/// Event raised when a new chat conversation is created.
/// </summary>
public sealed record ChatConversationCreatedEvent(
    string ConversationId,
    string ConversationType,
    IReadOnlyList<string> MemberUserIds,
    string CreatedById,
    string? Name = null
) : DomainEvent
{
    public override string EventType => "chat.conversation_created";
}

/// <summary>
/// Event raised when a member is added to a conversation.
/// </summary>
public sealed record ChatMemberAddedEvent(
    string ConversationId,
    string UserId,
    string UserName,
    string AddedById
) : DomainEvent
{
    public override string EventType => "chat.member_added";
}

/// <summary>
/// Event raised when a member is removed from a conversation.
/// </summary>
public sealed record ChatMemberRemovedEvent(
    string ConversationId,
    string UserId,
    string RemovedById
) : DomainEvent
{
    public override string EventType => "chat.member_removed";
}

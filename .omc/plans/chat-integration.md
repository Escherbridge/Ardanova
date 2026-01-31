# Chat Integration Implementation Plan

**Created:** 2026-01-28
**Status:** Ready for Implementation

---

## Context

### Original Request
Implement a full-stack chat integration feature for ArdaNova, enabling real-time messaging between users with support for direct messages, group conversations, message threading, typing indicators, and read receipts.

### Technical Specification Summary
- **Backend:** .NET Core with Entity Framework, SignalR for real-time
- **Frontend:** Next.js with tRPC, TypeScript
- **Existing patterns:** Service layer pattern, DTOs, domain events, repository pattern

---

## Work Objectives

### Core Objective
Build a complete chat system with real-time messaging capabilities integrated into the existing ArdaNova architecture.

### Deliverables
1. Domain entities for Conversation and ConversationMember
2. Enhanced ChatMessage entity with threading and delivery tracking
3. Chat service layer with full CRUD operations
4. SignalR hub methods for real-time chat events
5. Frontend tRPC router and hooks
6. Updated chat UI connected to real data

### Definition of Done
- All entities created with proper relationships and configurations
- Service layer handles all chat operations with proper validation
- SignalR broadcasts chat events to relevant users
- Frontend displays real conversations and sends messages in real-time
- Unit tests cover core service functionality (80%+ coverage on ChatService)

---

## Guardrails

### Must Have
- Conversation membership validation (users can only access their conversations)
- Message delivery status tracking (sent, delivered, read)
- Typing indicators with proper timeout handling
- Support for both direct (1:1) and group conversations
- Soft delete for messages (isDeleted flag, not hard delete)

### Must NOT Have
- File attachments in this phase (use existing Attachment entity later)
- Voice/video calling integration
- End-to-end encryption
- Message search functionality (future phase)

---

## Phase A: Backend Data Layer

### Task A1: Create Conversation Entity
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models/Entities/Conversation.cs`

**Create new file with:**
```csharp
namespace ArdaNova.Domain.Models.Entities;

[Table("Conversation")]
public class Conversation
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    // "direct" or "group"
    [Required]
    public string type { get; set; } = "direct";

    // Only for group conversations
    public string? name { get; set; }

    // Group avatar URL (optional)
    public string? avatarUrl { get; set; }

    // Creator of the conversation
    [Required]
    public string createdById { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    // Last message timestamp for sorting
    public DateTime? lastMessageAt { get; set; }

    // Navigation properties
    [ForeignKey("createdById")]
    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
```

**Acceptance Criteria:**
- [ ] Entity follows existing naming conventions (camelCase properties)
- [ ] Includes proper data annotations
- [ ] Has navigation properties for Members and Messages

---

### Task A2: Create ConversationMember Entity
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models/Entities/ConversationMember.cs`

**Create new file with:**
```csharp
namespace ArdaNova.Domain.Models.Entities;

[Table("ConversationMember")]
[Index(nameof(conversationId), nameof(userId), IsUnique = true)]
public class ConversationMember
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string conversationId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    // "admin" | "member" - for group conversations
    [Required]
    public string role { get; set; } = "member";

    // Nickname within this conversation (optional)
    public string? nickname { get; set; }

    // Mute notifications for this conversation
    [Required]
    public bool isMuted { get; set; } = false;

    // Pin this conversation to top
    [Required]
    public bool isPinned { get; set; } = false;

    // Last read message ID for unread count
    public string? lastReadMessageId { get; set; }

    // When user last read this conversation
    public DateTime? lastReadAt { get; set; }

    [Required]
    public DateTime joinedAt { get; set; }

    public DateTime? leftAt { get; set; }

    // Navigation properties
    [ForeignKey("conversationId")]
    public virtual Conversation? Conversation { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("ConversationMemberships")]
    public virtual User? User { get; set; }

    [ForeignKey("lastReadMessageId")]
    public virtual ChatMessage? LastReadMessage { get; set; }
}
```

**Acceptance Criteria:**
- [ ] Unique constraint on conversationId + userId
- [ ] Supports pinning and muting per-user
- [ ] Tracks last read message for unread counts

---

### Task A3: Modify ChatMessage Entity
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models/Entities/ChatMessage.cs`

**Add these properties to existing entity:**
```csharp
// Add after existing properties:

// Conversation this message belongs to
public string? conversationId { get; set; }

// For threaded replies
public string? replyToId { get; set; }

// Delivery tracking
public DateTime? deliveredAt { get; set; }

// Soft delete
[Required]
public bool isDeleted { get; set; } = false;

// Edit tracking
public DateTime? editedAt { get; set; }

// Add navigation properties:
[ForeignKey("conversationId")]
public virtual Conversation? Conversation { get; set; }

[ForeignKey("replyToId")]
public virtual ChatMessage? ReplyTo { get; set; }

public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();
```

**Acceptance Criteria:**
- [ ] Maintains backward compatibility with existing userToId/userFromId
- [ ] conversationId is nullable for migration purposes
- [ ] ReplyTo enables threaded conversations

---

### Task A4: Modify User Entity
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models/Entities/User.cs`

**Add navigation property after existing collections (around line 232):**
```csharp
public virtual ICollection<ConversationMember> ConversationMemberships { get; set; } = new List<ConversationMember>();

public virtual ICollection<Conversation> CreatedConversations { get; set; } = new List<Conversation>();
```

**Acceptance Criteria:**
- [ ] Added after UserFollowsAsFollowing collection
- [ ] Enables navigation from User to their conversations

---

### Task A5: Update GeneratedModelConfigurations
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Data/GeneratedModelConfigurations.cs`

**Add inside ApplyGeneratedConfigurations method:**
```csharp
// Conversation configurations
modelBuilder.Entity<Conversation>()
    .HasOne(c => c.CreatedBy)
    .WithMany(u => u.CreatedConversations)
    .HasForeignKey(c => c.createdById)
    .OnDelete(DeleteBehavior.Restrict);

// ConversationMember configurations
modelBuilder.Entity<ConversationMember>()
    .HasIndex(e => new { e.conversationId, e.userId })
    .IsUnique();

modelBuilder.Entity<ConversationMember>()
    .HasOne(cm => cm.Conversation)
    .WithMany(c => c.Members)
    .HasForeignKey(cm => cm.conversationId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ConversationMember>()
    .HasOne(cm => cm.User)
    .WithMany(u => u.ConversationMemberships)
    .HasForeignKey(cm => cm.userId)
    .OnDelete(DeleteBehavior.Cascade);

// ChatMessage conversation relationship
modelBuilder.Entity<ChatMessage>()
    .HasOne(m => m.Conversation)
    .WithMany(c => c.Messages)
    .HasForeignKey(m => m.conversationId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ChatMessage>()
    .HasOne(m => m.ReplyTo)
    .WithMany(m => m.Replies)
    .HasForeignKey(m => m.replyToId)
    .OnDelete(DeleteBehavior.Restrict);
```

**Acceptance Criteria:**
- [ ] Proper cascade delete behavior
- [ ] Self-referencing relationship for replies configured correctly

---

## Phase B: Backend Service Layer

### Task B1: Create ChatDtos
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/DTOs/ChatDtos.cs`

**Create new file with DTOs:**
```csharp
namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

// === Conversation DTOs ===

public record ConversationDto
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = "direct";
    public string? Name { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public ChatMessageDto? LastMessage { get; init; }
    public int UnreadCount { get; init; }
    public bool IsPinned { get; init; }
    public bool IsMuted { get; init; }
    public IReadOnlyList<ConversationMemberDto> Members { get; init; } = [];
}

public record ConversationMemberDto
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? UserAvatar { get; init; }
    public string Role { get; init; } = "member";
    public string? Nickname { get; init; }
    public bool IsOnline { get; init; }
    public DateTime JoinedAt { get; init; }
}

public record CreateConversationDto
{
    public required string CreatedById { get; init; }
    public required string Type { get; init; } // "direct" or "group"
    public string? Name { get; init; }
    public required IReadOnlyList<string> MemberIds { get; init; }
}

public record UpdateConversationDto
{
    public string? Name { get; init; }
    public string? AvatarUrl { get; init; }
}

public record AddConversationMemberDto
{
    public required string ConversationId { get; init; }
    public required string UserId { get; init; }
    public string Role { get; init; } = "member";
}

public record UpdateMemberSettingsDto
{
    public bool? IsMuted { get; init; }
    public bool? IsPinned { get; init; }
    public string? Nickname { get; init; }
}

// === Message DTOs ===

public record ChatMessageDto
{
    public string Id { get; init; } = string.Empty;
    public string ConversationId { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string? SenderName { get; init; }
    public string? SenderAvatar { get; init; }
    public string? Message { get; init; }
    public MessageStatus Status { get; init; }
    public string? ReplyToId { get; init; }
    public ChatMessageDto? ReplyTo { get; init; }
    public DateTime SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? SeenAt { get; init; }
    public DateTime? EditedAt { get; init; }
    public bool IsDeleted { get; init; }
    public string? AttachmentId { get; init; }
    public AttachmentDto? Attachment { get; init; }
}

public record SendMessageDto
{
    public required string ConversationId { get; init; }
    public required string SenderId { get; init; }
    public required string Message { get; init; }
    public string? ReplyToId { get; init; }
    public string? AttachmentId { get; init; }
}

public record UpdateMessageDto
{
    public required string Message { get; init; }
}

public record MarkMessagesReadDto
{
    public required string ConversationId { get; init; }
    public required string UserId { get; init; }
    public string? LastReadMessageId { get; init; }
}

// === Typing Indicator DTOs ===

public record TypingIndicatorDto
{
    public string ConversationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public bool IsTyping { get; init; }
}
```

**Acceptance Criteria:**
- [ ] All DTOs use init-only properties
- [ ] Follows existing DTO patterns in the codebase
- [ ] Includes nested DTOs for rich responses

---

### Task B2: Create IChatService Interface
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/Services/Interfaces/IChatService.cs`

**Create new file:**
```csharp
namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IChatService
{
    // === Conversation Operations ===
    Task<Result<ConversationDto>> GetConversationByIdAsync(string id, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ConversationDto>>> GetUserConversationsAsync(string userId, CancellationToken ct = default);
    Task<Result<ConversationDto>> GetOrCreateDirectConversationAsync(string userId1, string userId2, CancellationToken ct = default);
    Task<Result<ConversationDto>> CreateGroupConversationAsync(CreateConversationDto dto, CancellationToken ct = default);
    Task<Result<ConversationDto>> UpdateConversationAsync(string id, string userId, UpdateConversationDto dto, CancellationToken ct = default);
    Task<Result<bool>> LeaveConversationAsync(string conversationId, string userId, CancellationToken ct = default);

    // === Member Operations ===
    Task<Result<ConversationMemberDto>> AddMemberAsync(AddConversationMemberDto dto, string requesterId, CancellationToken ct = default);
    Task<Result<bool>> RemoveMemberAsync(string conversationId, string userId, string requesterId, CancellationToken ct = default);
    Task<Result<ConversationMemberDto>> UpdateMemberSettingsAsync(string conversationId, string userId, UpdateMemberSettingsDto dto, CancellationToken ct = default);

    // === Message Operations ===
    Task<Result<ChatMessageDto>> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default);
    Task<Result<ChatMessageDto>> UpdateMessageAsync(string messageId, string userId, UpdateMessageDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteMessageAsync(string messageId, string userId, CancellationToken ct = default);
    Task<Result<PagedResult<ChatMessageDto>>> GetMessagesAsync(string conversationId, string userId, int page, int pageSize, CancellationToken ct = default);

    // === Read Receipts ===
    Task<Result<bool>> MarkMessagesAsReadAsync(MarkMessagesReadDto dto, CancellationToken ct = default);
    Task<Result<bool>> MarkMessageAsDeliveredAsync(string messageId, string userId, CancellationToken ct = default);

    // === Typing Indicators ===
    Task<Result<bool>> SetTypingStatusAsync(string conversationId, string userId, bool isTyping, CancellationToken ct = default);

    // === Unread Counts ===
    Task<Result<int>> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<Result<Dictionary<string, int>>> GetUnreadCountsPerConversationAsync(string userId, CancellationToken ct = default);
}
```

**Acceptance Criteria:**
- [ ] All methods return Result<T> for consistent error handling
- [ ] Includes userId parameter for authorization checks
- [ ] Supports pagination for messages

---

### Task B3: Create ChatService Implementation
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/Services/Implementations/ChatService.cs`

**Create new file with full implementation:**

Key methods to implement:
1. **GetUserConversationsAsync** - Fetch all conversations for a user with unread counts
2. **GetOrCreateDirectConversationAsync** - Find existing or create new direct conversation
3. **CreateGroupConversationAsync** - Create group with multiple members
4. **SendMessageAsync** - Create message, update lastMessageAt, return with sender info
5. **GetMessagesAsync** - Paginated messages with sender info, ordered by sentAt desc
6. **MarkMessagesAsReadAsync** - Update lastReadMessageId and lastReadAt
7. **DeleteMessageAsync** - Soft delete (set isDeleted = true)

**Constructor dependencies:**
```csharp
public ChatService(
    IRepository<Conversation> conversationRepository,
    IRepository<ConversationMember> memberRepository,
    IRepository<ChatMessage> messageRepository,
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
```

**Acceptance Criteria:**
- [ ] Validates user membership before allowing access
- [ ] Updates conversation.lastMessageAt when message sent
- [ ] Calculates unread count based on lastReadMessageId
- [ ] Prevents duplicate direct conversations between same users
- [ ] Only admins can add/remove members from groups

---

### Task B4: Register ChatService in DependencyInjection
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/DependencyInjection.cs`

**Add after line 107 (after AttachmentService):**
```csharp
// Chat services
services.AddScoped<IChatService, ChatService>();
```

**Acceptance Criteria:**
- [ ] Service registered with Scoped lifetime
- [ ] Proper using statement added if needed

---

## Phase C: Backend SignalR/Events

### Task C1: Create ChatEvents
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.API/EventBus/Events/ChatEvents.cs`

**Create new file:**
```csharp
using ArdaNova.API.EventBus.Abstractions;

namespace ArdaNova.API.EventBus.Events;

public sealed record MessageSentEvent(
    string MessageId,
    string ConversationId,
    string SenderId,
    string? SenderName,
    string Message,
    string? ReplyToId,
    DateTime SentAt
) : DomainEvent
{
    public override string EventType => "chat.message_sent";
}

public sealed record MessageDeliveredEvent(
    string MessageId,
    string ConversationId,
    string UserId,
    DateTime DeliveredAt
) : DomainEvent
{
    public override string EventType => "chat.message_delivered";
}

public sealed record MessageReadEvent(
    string ConversationId,
    string UserId,
    string? LastReadMessageId,
    DateTime ReadAt
) : DomainEvent
{
    public override string EventType => "chat.message_read";
}

public sealed record MessageUpdatedEvent(
    string MessageId,
    string ConversationId,
    string NewContent,
    DateTime EditedAt
) : DomainEvent
{
    public override string EventType => "chat.message_updated";
}

public sealed record MessageDeletedEvent(
    string MessageId,
    string ConversationId,
    string DeletedBy
) : DomainEvent
{
    public override string EventType => "chat.message_deleted";
}

public sealed record TypingStartedEvent(
    string ConversationId,
    string UserId,
    string? UserName
) : DomainEvent
{
    public override string EventType => "chat.typing_started";
}

public sealed record TypingStoppedEvent(
    string ConversationId,
    string UserId
) : DomainEvent
{
    public override string EventType => "chat.typing_stopped";
}

public sealed record ConversationCreatedEvent(
    string ConversationId,
    string Type,
    string? Name,
    IReadOnlyList<string> MemberIds
) : DomainEvent
{
    public override string EventType => "chat.conversation_created";
}

public sealed record MemberAddedToConversationEvent(
    string ConversationId,
    string UserId,
    string AddedById
) : DomainEvent
{
    public override string EventType => "chat.member_added";
}

public sealed record MemberRemovedFromConversationEvent(
    string ConversationId,
    string UserId,
    string RemovedById
) : DomainEvent
{
    public override string EventType => "chat.member_removed";
}
```

**Acceptance Criteria:**
- [ ] All events extend DomainEvent base class
- [ ] EventType follows "chat.*" naming convention
- [ ] Events are sealed records (immutable)

---

### Task C2: Update IArdaNovaHubClient
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.API/WebSocket/Clients/IArdaNovaHubClient.cs`

**Add these methods to the interface:**
```csharp
// Chat events
Task MessageReceived(object message);
Task MessageDelivered(object data);
Task MessageRead(object data);
Task MessageUpdated(object message);
Task MessageDeleted(object data);
Task TypingIndicator(object data);
Task ConversationCreated(object conversation);
Task MemberAdded(object data);
Task MemberRemoved(object data);
```

**Acceptance Criteria:**
- [ ] Methods added after existing ActivityLogged method
- [ ] All methods use object parameter for flexibility

---

### Task C3: Update ArdaNovaHub with Chat Methods
**File:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.API/WebSocket/Hubs/ArdaNovaHub.cs`

**Add these methods to the hub class:**
```csharp
/// <summary>
/// Subscribes the client to a specific conversation's events.
/// </summary>
public async Task SubscribeToConversation(string conversationId)
{
    var userId = GetUserId();
    var connectionId = Context.ConnectionId;

    // TODO: Validate user is member of conversation
    // Inject IChatService and verify membership

    var groupName = $"conversation:{conversationId}";
    await Groups.AddToGroupAsync(connectionId, groupName);

    _logger.LogInformation(
        "Client {ConnectionId} (User: {UserId}) subscribed to conversation {ConversationId}",
        connectionId,
        userId ?? "anonymous",
        conversationId);
}

/// <summary>
/// Unsubscribes from a conversation's events.
/// </summary>
public async Task UnsubscribeFromConversation(string conversationId)
{
    var connectionId = Context.ConnectionId;
    var groupName = $"conversation:{conversationId}";

    await Groups.RemoveFromGroupAsync(connectionId, groupName);

    _logger.LogInformation(
        "Client {ConnectionId} unsubscribed from conversation {ConversationId}",
        connectionId,
        conversationId);
}

/// <summary>
/// Notifies other members that user is typing.
/// </summary>
public async Task SendTypingIndicator(string conversationId, bool isTyping)
{
    var userId = GetUserId();
    if (string.IsNullOrEmpty(userId)) return;

    var groupName = $"conversation:{conversationId}";

    // Broadcast to all members except sender
    await Clients.OthersInGroup(groupName).TypingIndicator(new
    {
        conversationId,
        userId,
        isTyping,
        timestamp = DateTime.UtcNow
    });

    _logger.LogDebug(
        "User {UserId} typing indicator ({IsTyping}) in conversation {ConversationId}",
        userId,
        isTyping,
        conversationId);
}

/// <summary>
/// Marks messages as delivered when client receives them.
/// </summary>
public async Task AcknowledgeMessageDelivery(string messageId, string conversationId)
{
    var userId = GetUserId();
    if (string.IsNullOrEmpty(userId)) return;

    // TODO: Call IChatService.MarkMessageAsDeliveredAsync

    var groupName = $"conversation:{conversationId}";
    await Clients.OthersInGroup(groupName).MessageDelivered(new
    {
        messageId,
        conversationId,
        userId,
        deliveredAt = DateTime.UtcNow
    });
}
```

**Acceptance Criteria:**
- [ ] SubscribeToConversation validates membership (add TODO for now)
- [ ] Typing indicators use OthersInGroup to exclude sender
- [ ] Proper logging for all operations

---

## Phase D: Backend Tests

### Task D1: Create ChatServiceTests
**File:** `ardanova-backend-api-mcp/api-server/tests/ArdaNova.Application.Tests/Services/ChatServiceTests.cs`

**Create test file with these test cases:**

```csharp
namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class ChatServiceTests
{
    // Setup mocks similar to OpportunityServiceTests

    // Test categories:

    // === Conversation Tests ===
    // GetUserConversationsAsync_ReturnsUserConversations
    // GetUserConversationsAsync_IncludesUnreadCounts
    // GetOrCreateDirectConversationAsync_ReturnsExistingConversation
    // GetOrCreateDirectConversationAsync_CreatesNewWhenNotExists
    // CreateGroupConversationAsync_WithValidMembers_CreatesGroup
    // CreateGroupConversationAsync_WithLessThanTwoMembers_ReturnsValidationError
    // GetConversationByIdAsync_WhenNotMember_ReturnsUnauthorized

    // === Message Tests ===
    // SendMessageAsync_WithValidData_CreatesMessage
    // SendMessageAsync_WhenNotMember_ReturnsUnauthorized
    // SendMessageAsync_UpdatesConversationLastMessageAt
    // GetMessagesAsync_ReturnsPaginatedMessages
    // GetMessagesAsync_WhenNotMember_ReturnsUnauthorized
    // DeleteMessageAsync_SoftDeletesMessage
    // DeleteMessageAsync_WhenNotSender_ReturnsUnauthorized
    // UpdateMessageAsync_UpdatesContentAndEditedAt

    // === Read Receipt Tests ===
    // MarkMessagesAsReadAsync_UpdatesLastReadMessageId
    // MarkMessagesAsReadAsync_WhenNotMember_ReturnsUnauthorized

    // === Member Tests ===
    // AddMemberAsync_WhenAdmin_AddsMember
    // AddMemberAsync_WhenNotAdmin_ReturnsUnauthorized
    // RemoveMemberAsync_WhenAdmin_RemovesMember
    // LeaveConversationAsync_RemovesMemberFromGroup
    // LeaveConversationAsync_WhenDirectConversation_ReturnsValidationError
}
```

**Minimum required tests (implement these):**
1. `GetUserConversationsAsync_ReturnsUserConversations`
2. `GetOrCreateDirectConversationAsync_ReturnsExistingConversation`
3. `GetOrCreateDirectConversationAsync_CreatesNewWhenNotExists`
4. `SendMessageAsync_WithValidData_CreatesMessage`
5. `SendMessageAsync_WhenNotMember_ReturnsUnauthorized`
6. `GetMessagesAsync_ReturnsPaginatedMessages`
7. `DeleteMessageAsync_SoftDeletesMessage`
8. `MarkMessagesAsReadAsync_UpdatesLastReadMessageId`

**Acceptance Criteria:**
- [ ] Follows existing test patterns (Moq, FluentAssertions)
- [ ] Each test has Arrange/Act/Assert structure
- [ ] Tests cover both success and failure scenarios
- [ ] Minimum 8 tests implemented

---

## Phase E: Frontend Types & Infrastructure

### Task E1: Add Chat Event Types to WebSocket Types
**File:** `ardanova-client/src/lib/websocket/types.ts`

**Add after ActivityLoggedEvent (around line 117):**
```typescript
// Chat Events
export interface MessageSentEvent extends DomainEvent {
  eventType: "chat.message_sent";
  messageId: string;
  conversationId: string;
  senderId: string;
  senderName?: string;
  message: string;
  replyToId?: string;
  sentAt: string;
}

export interface MessageDeliveredEvent extends DomainEvent {
  eventType: "chat.message_delivered";
  messageId: string;
  conversationId: string;
  userId: string;
  deliveredAt: string;
}

export interface MessageReadEvent extends DomainEvent {
  eventType: "chat.message_read";
  conversationId: string;
  userId: string;
  lastReadMessageId?: string;
  readAt: string;
}

export interface MessageUpdatedEvent extends DomainEvent {
  eventType: "chat.message_updated";
  messageId: string;
  conversationId: string;
  newContent: string;
  editedAt: string;
}

export interface MessageDeletedEvent extends DomainEvent {
  eventType: "chat.message_deleted";
  messageId: string;
  conversationId: string;
  deletedBy: string;
}

export interface TypingIndicatorEvent extends DomainEvent {
  eventType: "chat.typing";
  conversationId: string;
  userId: string;
  userName?: string;
  isTyping: boolean;
}

export interface ConversationCreatedEvent extends DomainEvent {
  eventType: "chat.conversation_created";
  conversationId: string;
  type: "direct" | "group";
  name?: string;
  memberIds: string[];
}
```

**Update ArdaNovaEvent union type:**
```typescript
export type ArdaNovaEvent =
  | UserCreatedEvent
  | UserUpdatedEvent
  // ... existing events ...
  | ActivityLoggedEvent
  | MessageSentEvent
  | MessageDeliveredEvent
  | MessageReadEvent
  | MessageUpdatedEvent
  | MessageDeletedEvent
  | TypingIndicatorEvent
  | ConversationCreatedEvent;
```

**Update SubscriptionAction type:**
```typescript
export type SubscriptionAction =
  // ... existing actions ...
  | { action: "subscribeToConversation"; payload: { conversationId: string } }
  | { action: "unsubscribeFromConversation"; payload: { conversationId: string } };
```

**Acceptance Criteria:**
- [ ] All event types extend DomainEvent
- [ ] Union type updated to include new events
- [ ] Subscription actions added for conversations

---

### Task E2: Create Chat TypeScript Types
**File:** `ardanova-client/src/types/chat.ts`

**Create new file:**
```typescript
export type ConversationType = "direct" | "group";
export type MessageStatus = "sending" | "sent" | "delivered" | "read" | "failed";
export type MemberRole = "admin" | "member";

export interface ConversationMember {
  id: string;
  userId: string;
  userName?: string;
  userAvatar?: string;
  role: MemberRole;
  nickname?: string;
  isOnline: boolean;
  joinedAt: string;
}

export interface Conversation {
  id: string;
  type: ConversationType;
  name?: string;
  avatarUrl?: string;
  createdAt: string;
  lastMessageAt?: string;
  lastMessage?: ChatMessage;
  unreadCount: number;
  isPinned: boolean;
  isMuted: boolean;
  members: ConversationMember[];
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  senderId: string;
  senderName?: string;
  senderAvatar?: string;
  message?: string;
  status: MessageStatus;
  replyToId?: string;
  replyTo?: ChatMessage;
  sentAt: string;
  deliveredAt?: string;
  seenAt?: string;
  editedAt?: string;
  isDeleted: boolean;
  attachmentId?: string;
}

export interface TypingUser {
  conversationId: string;
  userId: string;
  userName?: string;
  startedAt: number;
}

// Input types for mutations
export interface CreateConversationInput {
  type: ConversationType;
  name?: string;
  memberIds: string[];
}

export interface SendMessageInput {
  conversationId: string;
  message: string;
  replyToId?: string;
}

export interface UpdateMemberSettingsInput {
  isMuted?: boolean;
  isPinned?: boolean;
  nickname?: string;
}
```

**Acceptance Criteria:**
- [ ] Types match backend DTOs
- [ ] Includes input types for mutations
- [ ] Exported for use across the app

---

### Task E3: Create Chat tRPC Router
**File:** `ardanova-client/src/server/api/routers/chat.ts`

**Create new file with full router:**

```typescript
import { z } from "zod";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const chatRouter = createTRPCRouter({
  // Get all conversations for current user
  getConversations: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.chat.getConversations(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get or create direct conversation
  getOrCreateDirect: protectedProcedure
    .input(z.object({ otherUserId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.getOrCreateDirect(userId, input.otherUserId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to get/create conversation");
      }

      return response.data;
    }),

  // Create group conversation
  createGroup: protectedProcedure
    .input(z.object({
      name: z.string().min(1).max(100),
      memberIds: z.array(z.string()).min(1),
    }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.createGroup({
        createdById: userId,
        type: "group",
        name: input.name,
        memberIds: [userId, ...input.memberIds],
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create group");
      }

      return response.data;
    }),

  // Get conversation by ID
  getConversation: protectedProcedure
    .input(z.object({ conversationId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.getConversation(input.conversationId, userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Conversation not found");
      }

      return response.data;
    }),

  // Get messages for a conversation
  getMessages: protectedProcedure
    .input(z.object({
      conversationId: z.string(),
      page: z.number().min(1).default(1),
      pageSize: z.number().min(1).max(100).default(50),
    }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.getMessages(
        input.conversationId,
        userId,
        input.page,
        input.pageSize
      );

      if (response.error) {
        throw new Error(response.error);
      }

      return {
        messages: response.data?.items ?? [],
        hasMore: response.data?.hasNextPage ?? false,
        totalCount: response.data?.totalCount ?? 0,
      };
    }),

  // Send a message
  sendMessage: protectedProcedure
    .input(z.object({
      conversationId: z.string(),
      message: z.string().min(1).max(5000),
      replyToId: z.string().optional(),
    }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.sendMessage({
        conversationId: input.conversationId,
        senderId: userId,
        message: input.message,
        replyToId: input.replyToId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to send message");
      }

      return response.data;
    }),

  // Edit a message
  editMessage: protectedProcedure
    .input(z.object({
      messageId: z.string(),
      message: z.string().min(1).max(5000),
    }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.updateMessage(input.messageId, userId, {
        message: input.message,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to edit message");
      }

      return response.data;
    }),

  // Delete a message
  deleteMessage: protectedProcedure
    .input(z.object({ messageId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.deleteMessage(input.messageId, userId);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // Mark messages as read
  markAsRead: protectedProcedure
    .input(z.object({
      conversationId: z.string(),
      lastReadMessageId: z.string().optional(),
    }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.markAsRead({
        conversationId: input.conversationId,
        userId,
        lastReadMessageId: input.lastReadMessageId,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // Update conversation settings (pin/mute)
  updateSettings: protectedProcedure
    .input(z.object({
      conversationId: z.string(),
      isPinned: z.boolean().optional(),
      isMuted: z.boolean().optional(),
    }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.updateMemberSettings(
        input.conversationId,
        userId,
        {
          isPinned: input.isPinned,
          isMuted: input.isMuted,
        }
      );

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // Get total unread count
  getUnreadCount: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.chat.getUnreadCount(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? 0;
  }),

  // Add member to group (admin only)
  addMember: protectedProcedure
    .input(z.object({
      conversationId: z.string(),
      userId: z.string(),
    }))
    .mutation(async ({ input, ctx }) => {
      const requesterId = ctx.session.user.id;
      const response = await apiClient.chat.addMember({
        conversationId: input.conversationId,
        userId: input.userId,
      }, requesterId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add member");
      }

      return response.data;
    }),

  // Remove member from group (admin only)
  removeMember: protectedProcedure
    .input(z.object({
      conversationId: z.string(),
      userId: z.string(),
    }))
    .mutation(async ({ input, ctx }) => {
      const requesterId = ctx.session.user.id;
      const response = await apiClient.chat.removeMember(
        input.conversationId,
        input.userId,
        requesterId
      );

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // Leave conversation
  leave: protectedProcedure
    .input(z.object({ conversationId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.chat.leaveConversation(input.conversationId, userId);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),
});
```

**Acceptance Criteria:**
- [ ] All mutations require authentication (protectedProcedure)
- [ ] Input validation with Zod
- [ ] Proper error handling
- [ ] Follows existing router patterns

---

### Task E4: Register Chat Router
**File:** `ardanova-client/src/server/api/root.ts`

**Add import:**
```typescript
import { chatRouter } from "~/server/api/routers/chat";
```

**Add to appRouter:**
```typescript
export const appRouter = createTRPCRouter({
  // ... existing routers ...
  taskBid: taskBidRouter,
  chat: chatRouter,  // Add this line
});
```

**Acceptance Criteria:**
- [ ] Router imported at top
- [ ] Added to appRouter object

---

## Phase F: Frontend Hooks

### Task F1: Create useChat Hook
**File:** `ardanova-client/src/hooks/use-chat.ts`

**Create new file:**
```typescript
"use client";

import { useCallback, useEffect, useState } from "react";
import { api } from "~/trpc/react";
import type { Conversation, TypingUser } from "~/types/chat";

interface UseChatOptions {
  enabled?: boolean;
}

export function useChat(options: UseChatOptions = {}) {
  const { enabled = true } = options;
  const [typingUsers, setTypingUsers] = useState<Map<string, TypingUser[]>>(new Map());

  // Fetch conversations
  const {
    data: conversations,
    isLoading,
    error,
    refetch,
  } = api.chat.getConversations.useQuery(undefined, {
    enabled,
    refetchInterval: 30000, // Refetch every 30s as fallback
  });

  // Get total unread count
  const { data: unreadCount = 0 } = api.chat.getUnreadCount.useQuery(undefined, {
    enabled,
    refetchInterval: 60000,
  });

  // Mutations
  const createDirectMutation = api.chat.getOrCreateDirect.useMutation();
  const createGroupMutation = api.chat.createGroup.useMutation();
  const updateSettingsMutation = api.chat.updateSettings.useMutation();

  // Create direct conversation
  const createDirectConversation = useCallback(
    async (otherUserId: string) => {
      const result = await createDirectMutation.mutateAsync({ otherUserId });
      refetch();
      return result;
    },
    [createDirectMutation, refetch]
  );

  // Create group conversation
  const createGroupConversation = useCallback(
    async (name: string, memberIds: string[]) => {
      const result = await createGroupMutation.mutateAsync({ name, memberIds });
      refetch();
      return result;
    },
    [createGroupMutation, refetch]
  );

  // Toggle pin
  const togglePin = useCallback(
    async (conversationId: string, isPinned: boolean) => {
      await updateSettingsMutation.mutateAsync({ conversationId, isPinned });
      refetch();
    },
    [updateSettingsMutation, refetch]
  );

  // Toggle mute
  const toggleMute = useCallback(
    async (conversationId: string, isMuted: boolean) => {
      await updateSettingsMutation.mutateAsync({ conversationId, isMuted });
      refetch();
    },
    [updateSettingsMutation, refetch]
  );

  // Handle typing indicators (with timeout cleanup)
  const handleTypingIndicator = useCallback(
    (conversationId: string, userId: string, userName: string | undefined, isTyping: boolean) => {
      setTypingUsers((prev) => {
        const newMap = new Map(prev);
        const conversationTyping = newMap.get(conversationId) ?? [];

        if (isTyping) {
          // Add or update typing user
          const existingIndex = conversationTyping.findIndex((t) => t.userId === userId);
          const typingUser: TypingUser = {
            conversationId,
            userId,
            userName,
            startedAt: Date.now(),
          };

          if (existingIndex >= 0) {
            conversationTyping[existingIndex] = typingUser;
          } else {
            conversationTyping.push(typingUser);
          }
        } else {
          // Remove typing user
          const filteredTyping = conversationTyping.filter((t) => t.userId !== userId);
          if (filteredTyping.length === 0) {
            newMap.delete(conversationId);
            return newMap;
          }
          newMap.set(conversationId, filteredTyping);
          return newMap;
        }

        newMap.set(conversationId, conversationTyping);
        return newMap;
      });
    },
    []
  );

  // Clean up stale typing indicators (after 5 seconds)
  useEffect(() => {
    const interval = setInterval(() => {
      const now = Date.now();
      setTypingUsers((prev) => {
        const newMap = new Map(prev);
        let hasChanges = false;

        for (const [conversationId, users] of newMap) {
          const activeUsers = users.filter((u) => now - u.startedAt < 5000);
          if (activeUsers.length !== users.length) {
            hasChanges = true;
            if (activeUsers.length === 0) {
              newMap.delete(conversationId);
            } else {
              newMap.set(conversationId, activeUsers);
            }
          }
        }

        return hasChanges ? newMap : prev;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  // Sort conversations: pinned first, then by lastMessageAt
  const sortedConversations = [...(conversations ?? [])].sort((a, b) => {
    if (a.isPinned && !b.isPinned) return -1;
    if (!a.isPinned && b.isPinned) return 1;
    const aTime = a.lastMessageAt ? new Date(a.lastMessageAt).getTime() : 0;
    const bTime = b.lastMessageAt ? new Date(b.lastMessageAt).getTime() : 0;
    return bTime - aTime;
  });

  return {
    conversations: sortedConversations,
    isLoading,
    error,
    unreadCount,
    typingUsers,
    createDirectConversation,
    createGroupConversation,
    togglePin,
    toggleMute,
    handleTypingIndicator,
    refetch,
  };
}
```

**Acceptance Criteria:**
- [ ] Manages conversation list with sorting
- [ ] Handles typing indicators with timeout cleanup
- [ ] Provides mutations for creating conversations
- [ ] Returns unread count

---

### Task F2: Create useConversation Hook
**File:** `ardanova-client/src/hooks/use-conversation.ts`

**Create new file:**
```typescript
"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "~/trpc/react";
import type { ChatMessage } from "~/types/chat";

interface UseConversationOptions {
  conversationId: string;
  enabled?: boolean;
  pageSize?: number;
}

export function useConversation(options: UseConversationOptions) {
  const { conversationId, enabled = true, pageSize = 50 } = options;
  const [page, setPage] = useState(1);
  const [allMessages, setAllMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Fetch conversation details
  const {
    data: conversation,
    isLoading: isLoadingConversation,
    error: conversationError,
  } = api.chat.getConversation.useQuery(
    { conversationId },
    { enabled: enabled && !!conversationId }
  );

  // Fetch messages
  const {
    data: messagesData,
    isLoading: isLoadingMessages,
    error: messagesError,
    refetch: refetchMessages,
  } = api.chat.getMessages.useQuery(
    { conversationId, page, pageSize },
    { enabled: enabled && !!conversationId }
  );

  // Update allMessages when new data arrives
  useEffect(() => {
    if (messagesData?.messages) {
      if (page === 1) {
        setAllMessages(messagesData.messages);
      } else {
        // Prepend older messages (pagination loads older messages)
        setAllMessages((prev) => {
          const existingIds = new Set(prev.map((m) => m.id));
          const newMessages = messagesData.messages.filter((m) => !existingIds.has(m.id));
          return [...newMessages, ...prev];
        });
      }
    }
  }, [messagesData, page]);

  // Mutations
  const sendMessageMutation = api.chat.sendMessage.useMutation();
  const editMessageMutation = api.chat.editMessage.useMutation();
  const deleteMessageMutation = api.chat.deleteMessage.useMutation();
  const markAsReadMutation = api.chat.markAsRead.useMutation();

  // Send message
  const sendMessage = useCallback(
    async (message: string, replyToId?: string) => {
      const result = await sendMessageMutation.mutateAsync({
        conversationId,
        message,
        replyToId,
      });

      // Optimistically add message
      setAllMessages((prev) => [...prev, result as ChatMessage]);

      return result;
    },
    [conversationId, sendMessageMutation]
  );

  // Edit message
  const editMessage = useCallback(
    async (messageId: string, message: string) => {
      const result = await editMessageMutation.mutateAsync({ messageId, message });

      // Update message in list
      setAllMessages((prev) =>
        prev.map((m) => (m.id === messageId ? { ...m, message, editedAt: new Date().toISOString() } : m))
      );

      return result;
    },
    [editMessageMutation]
  );

  // Delete message
  const deleteMessage = useCallback(
    async (messageId: string) => {
      await deleteMessageMutation.mutateAsync({ messageId });

      // Mark as deleted in list
      setAllMessages((prev) =>
        prev.map((m) => (m.id === messageId ? { ...m, isDeleted: true } : m))
      );
    },
    [deleteMessageMutation]
  );

  // Mark as read
  const markAsRead = useCallback(async () => {
    if (allMessages.length === 0) return;

    const lastMessage = allMessages[allMessages.length - 1];
    await markAsReadMutation.mutateAsync({
      conversationId,
      lastReadMessageId: lastMessage?.id,
    });
  }, [conversationId, markAsReadMutation, allMessages]);

  // Load more (older) messages
  const loadMore = useCallback(() => {
    if (messagesData?.hasMore) {
      setPage((p) => p + 1);
    }
  }, [messagesData?.hasMore]);

  // Handle typing with debounce
  const handleTyping = useCallback(() => {
    if (!isTyping) {
      setIsTyping(true);
      // TODO: Send typing indicator via WebSocket
    }

    // Reset timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      // TODO: Send typing stopped via WebSocket
    }, 2000);
  }, [isTyping]);

  // Add new message from WebSocket
  const addMessage = useCallback((message: ChatMessage) => {
    setAllMessages((prev) => {
      // Check if message already exists
      if (prev.some((m) => m.id === message.id)) {
        return prev;
      }
      return [...prev, message];
    });
  }, []);

  // Update message from WebSocket (edit/delete/delivered/read)
  const updateMessage = useCallback((messageId: string, updates: Partial<ChatMessage>) => {
    setAllMessages((prev) =>
      prev.map((m) => (m.id === messageId ? { ...m, ...updates } : m))
    );
  }, []);

  // Cleanup typing timeout
  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, []);

  return {
    conversation,
    messages: allMessages,
    isLoading: isLoadingConversation || (isLoadingMessages && page === 1),
    isLoadingMore: isLoadingMessages && page > 1,
    error: conversationError || messagesError,
    hasMore: messagesData?.hasMore ?? false,
    sendMessage,
    editMessage,
    deleteMessage,
    markAsRead,
    loadMore,
    handleTyping,
    addMessage,
    updateMessage,
    refetchMessages,
    isSending: sendMessageMutation.isPending,
  };
}
```

**Acceptance Criteria:**
- [ ] Manages messages with pagination (loads older on scroll up)
- [ ] Provides optimistic updates for send/edit/delete
- [ ] Handles typing indicator with debounce
- [ ] Exposes methods for WebSocket message updates

---

## Phase G: Frontend UI

### Task G1: Update Chats Page
**File:** `ardanova-client/src/app/chats/page.tsx`

**Key changes:**

1. **Replace sample data with hooks:**
```typescript
import { useChat } from "~/hooks/use-chat";
import { useConversation } from "~/hooks/use-conversation";
import { useSession } from "next-auth/react";

// Inside component:
const { data: session } = useSession();
const currentUserId = session?.user?.id ?? "";

const {
  conversations,
  isLoading: isLoadingConversations,
  unreadCount,
  typingUsers,
  createDirectConversation,
  togglePin,
  toggleMute,
} = useChat();

const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);

const {
  conversation: selectedConversation,
  messages,
  isLoading: isLoadingMessages,
  hasMore,
  sendMessage,
  editMessage,
  deleteMessage,
  markAsRead,
  loadMore,
  handleTyping,
  isSending,
} = useConversation({
  conversationId: selectedConversationId ?? "",
  enabled: !!selectedConversationId,
});
```

2. **Update conversation list rendering:**
- Map over `conversations` instead of `sampleConversations`
- Use real `lastMessage` data
- Display real `unreadCount`
- Handle loading state

3. **Update messages rendering:**
- Map over `messages` instead of `sampleMessages`
- Use `currentUserId` for "isOwn" check
- Handle deleted messages (show "This message was deleted")
- Show edit indicator for edited messages

4. **Connect message input:**
```typescript
const handleSend = async () => {
  if (!messageInput.trim() || isSending) return;

  try {
    await sendMessage(messageInput.trim(), replyingToId);
    setMessageInput("");
    setReplyingToId(null);
  } catch (error) {
    console.error("Failed to send message:", error);
  }
};
```

5. **Mark as read on conversation open:**
```typescript
useEffect(() => {
  if (selectedConversationId && messages.length > 0) {
    markAsRead();
  }
}, [selectedConversationId, messages.length, markAsRead]);
```

6. **Add infinite scroll for loading more messages:**
```typescript
const messagesContainerRef = useRef<HTMLDivElement>(null);

const handleScroll = () => {
  const container = messagesContainerRef.current;
  if (!container) return;

  // Load more when scrolled near top
  if (container.scrollTop < 100 && hasMore && !isLoadingMore) {
    loadMore();
  }
};
```

**Acceptance Criteria:**
- [ ] Uses real data from hooks instead of sample data
- [ ] Displays loading states appropriately
- [ ] Sends messages via tRPC mutation
- [ ] Marks messages as read when conversation opened
- [ ] Supports infinite scroll for message history
- [ ] Handles empty states gracefully

---

## Commit Strategy

### Commit 1: Phase A - Data Layer
```
feat(chat): add Conversation and ConversationMember entities

- Create Conversation entity with type, name, members
- Create ConversationMember entity with roles and settings
- Modify ChatMessage with conversationId, replyToId, delivery tracking
- Update User with ConversationMemberships navigation
- Add EF configurations for relationships
```

### Commit 2: Phase B - Service Layer
```
feat(chat): implement ChatService with CRUD operations

- Create ChatDtos for conversations and messages
- Create IChatService interface
- Implement ChatService with full CRUD
- Register in DependencyInjection
```

### Commit 3: Phase C - SignalR/Events
```
feat(chat): add real-time chat events and hub methods

- Create ChatEvents for messaging, typing, membership
- Update IArdaNovaHubClient with chat methods
- Add SubscribeToConversation and typing methods to hub
```

### Commit 4: Phase D - Backend Tests
```
test(chat): add ChatService unit tests

- Test conversation CRUD operations
- Test message operations
- Test read receipts
- Test authorization checks
```

### Commit 5: Phase E - Frontend Infrastructure
```
feat(chat): add frontend types and tRPC router

- Add chat event types to websocket types
- Create chat.ts TypeScript types
- Create chat tRPC router with all operations
- Register in root router
```

### Commit 6: Phase F - Frontend Hooks
```
feat(chat): add useChat and useConversation hooks

- Create useChat for conversation list management
- Create useConversation for single conversation
- Handle typing indicators with debounce
- Support optimistic updates
```

### Commit 7: Phase G - UI Integration
```
feat(chat): connect chat UI to real data

- Replace sample data with hook data
- Implement send message functionality
- Add mark as read on open
- Add infinite scroll for message history
```

---

## Success Criteria

1. **Functional Requirements:**
   - [ ] Users can view their conversation list sorted by recent activity
   - [ ] Users can start direct conversations with other users
   - [ ] Users can create group conversations with multiple members
   - [ ] Messages are sent and received in real-time
   - [ ] Unread counts are accurate and update in real-time
   - [ ] Users can pin/mute conversations
   - [ ] Messages can be edited and soft-deleted
   - [ ] Typing indicators show when others are typing

2. **Technical Requirements:**
   - [ ] All backend entities follow existing conventions
   - [ ] Service layer has proper validation and authorization
   - [ ] SignalR events broadcast to correct conversation members
   - [ ] Unit tests achieve 80%+ coverage on ChatService
   - [ ] Frontend uses optimistic updates for responsiveness
   - [ ] Hooks handle loading and error states

3. **Performance Requirements:**
   - [ ] Message pagination works correctly (50 messages per page)
   - [ ] Typing indicator debounce prevents excessive updates
   - [ ] Stale typing indicators cleaned up after 5 seconds

---

## Notes for Implementation

1. **Migration:** The ChatMessage.conversationId is nullable to support migration from the existing userToId/userFromId model. A data migration may be needed later to create conversations for existing messages.

2. **API Client:** The frontend tRPC router assumes an `apiClient.chat.*` exists. This needs to be added to the API client to match the backend endpoints.

3. **WebSocket Integration:** The hooks have TODO comments for WebSocket integration. The actual WebSocket connection and event handling should be integrated based on the existing WebSocket infrastructure.

4. **Online Status:** The ConversationMemberDto.isOnline field requires implementing presence tracking, which may be deferred to a future phase.

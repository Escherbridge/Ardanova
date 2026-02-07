# Social & Real-time Specification

## Overview
This track powers the social graph, real-time communication (Chat), and system-wide notifications. It binds the community together.

## Data Models

### Social Graph
- **UserFollow**: User -> User.
- **ProjectFollow**: User -> Project.
- **GuildFollow**: User -> Guild.
- **Activity**: Feed/Audit log.
    - Fields: `Type` (Action enum), `EntityType`, `EntityId`, `Data` (JSON metadata).

### Chat System
- **Conversation**:
    - `Type`: Enum (DIRECT, GROUP).
    - `LastMessageAt`: DateTime (Sorting).
    - `UnreadCount`: Integer (Per user projection).
- **ConversationMember**:
    - `Role`: Enum (OWNER, ADMIN, MEMBER).
    - `LastReadAt`: DateTime.
    - `IsOnline`: Boolean (SignalR state).
- **ChatMessage**:
    - `Status`: Enum (SENT, DELIVERED, READ, FAILED).
    - `ReplyToId`: UUID? (Thread support).
    - `IsDeleted`: Boolean (Soft delete).

### Notification
- `Type`: Enum (Detailed list: TASK_*, PROJECT_*, GUILD_*, SOCIAL_*).
- `Data`: JSON (Context for navigation).
- `ActionUrl`: String.
- `IsRead`: Boolean.

## API Endpoints (`ChatController` / `NotificationsController`)

| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/chat/conversations` | List chats | `ConversationListDto` |
| `POST` | `/api/chat/conversations` | Start chat | `CreateDirectConversationDto` |
| `GET` | `/api/chat/conversations/{id}/messages` | Get history | `MessageListDto` |
| `POST` | `/api/chat/conversations/{id}/messages` | Send message | `SendMessageDto` |
| `POST` | `/api/chat/conversations/{id}/read` | Mark read | `MarkMessagesReadDto` |
| `GET` | `/api/notifications` | Get alerts | `NotificationDto[]` |
| `POST` | `/api/notifications/mark-read` | Clear alerts | - |

## Business Logic & Validation

### 1. Real-time (SignalR)
- **Hubs**: `ChatHub`, `NotificationHub`.
- **Events**:
    - `ReceiveMessage`: New message payload.
    - `MessageStatus`: IDs marked as READ.
    - `TypingIndicator`: User X is typing...
    - `UserOnlineStatus`: Presence updates.

### 2. Activity Feed
- **Aggregation**: Activities are aggregated by `ProjectId` or `GuildId` for context streams.
- **Fan-out**: When a project updates, `Activity` is created. Followers query `Activity` table filtered by their follows.

### 3. Privacy
- **Direct Messages**: Strict RLS (Row Level Security) logic—only members can fetch messages.
- **Group Chats**: Ad-hoc groups or Project/Guild channels.

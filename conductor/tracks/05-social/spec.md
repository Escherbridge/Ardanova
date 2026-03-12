# Social & Real-time — Retroactive Specification

> This document retroactively captures the social and real-time communication systems implemented across early development.

## Status: PARTIALLY COMPLETE

Chat system is fully functional. Post/feed system has domain entities but no controller/service (see Track 12). Notification backend is complete but has no frontend UI (see Track 14).

## Chat System (COMPLETE)

### Backend

**ChatController** (`ArdaNova.API/Controllers/ChatController.cs`) — 14 endpoints:

| Endpoint | Purpose |
|----------|---------|
| `GET conversations` | List user conversations (paged) |
| `GET conversations/{id}` | Get single conversation |
| `POST conversations/direct` | Create/get direct message conversation |
| `POST conversations/group` | Create group conversation |
| `PUT conversations/{id}` | Update conversation settings |
| `POST conversations/{id}/members` | Add member to group |
| `DELETE conversations/{id}/members/{userId}` | Remove member |
| `POST conversations/{id}/leave` | Leave conversation |
| `GET conversations/{id}/messages` | Get messages (paged) |
| `POST messages` | Send message |
| `PUT messages/{id}` | Edit message |
| `DELETE messages/{id}` | Delete message |
| `POST messages/read` | Mark messages as read |
| `POST typing` | Send typing indicator |

**Service:** `IChatService` / `ChatService`

### Real-time (SignalR)

**ArdaNovaHub** — Central SignalR hub with subscription-based groups:
- User subscriptions (personal notifications)
- Project subscriptions (project activity)
- Guild subscriptions (guild activity)
- Conversation subscriptions (chat messages)

**Event handlers:**
- `ChatEventHandler` — Real-time message delivery, typing indicators
- `NotificationHubHandler` — Push notifications to connected users
- `ActivityHubHandler` — Activity feed updates

**EventBus** — In-memory event bus for decoupled handler invocation.

### Frontend

**Chat page** — Full real-time chat interface with:
- Conversation list with unread counts
- Message thread with real-time updates
- Typing indicators
- Direct and group conversations

**tRPC Router:** `chat.ts` — Thin proxy
**API Client:** `endpoints/chat.ts` — HTTP wrapper

## Post/Feed System (SCAFFOLDED — See Track 12)

**Domain entities exist:**
- `Post`, `PostComment`, `PostLike`, `PostBookmark`, `PostShare`, `PostMedia`
- Enums: `PostType`, `PostVisibility`

**Missing:**
- No `PostService` or `PostsController`
- `post.ts` tRPC router is T3 scaffold stub (only `hello` + `getSecretMessage`)
- Dashboard feed uses hardcoded sample data

**Resolution:** Track 12 (Dashboard & Feed Integration) addresses all post/feed gaps.

## Notification Backend (COMPLETE — Frontend in Track 14)

**NotificationsController** (`ArdaNova.API/Controllers/NotificationsController.cs`) — 10 endpoints:

| Endpoint | Purpose |
|----------|---------|
| `GET {id}` | Get notification by ID |
| `GET user/{userId}` | Get user notifications |
| `GET user/{userId}/paged` | Paged notifications |
| `GET user/{userId}/unread` | Unread notifications |
| `GET user/{userId}/summary` | Notification summary (counts) |
| `POST` | Create notification |
| `POST {id}/read` | Mark as read |
| `POST user/{userId}/read-all` | Mark all as read |
| `DELETE {id}` | Delete notification |
| `DELETE user/{userId}` | Delete all user notifications |

**Service:** `INotificationService` / `NotificationService`
**SignalR:** `NotificationHubHandler` — Real-time push to connected users

**Frontend:** No notification UI exists. See Track 14 (Notification System UI).

## User Following (COMPLETE)

**Backend:** `UserFollowService` — follow/unfollow/check/counts on `UsersController`
**Frontend:** Follow buttons exist on People page but are decorative (not wired to API). Track 12 addresses wiring social actions.

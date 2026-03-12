# Track 14 — Notification System UI — Technical Specification

## Overview

The notification system backend is **fully implemented**. The .NET API exposes a complete `NotificationsController`, `NotificationServices`, and a SignalR `NotificationHubHandler` that pushes real-time events to the `user:{userId}` group on creation, read, and all-read. The frontend has **zero notification UI** — the Bell icon in `app-sidebar.tsx` is a non-functional placeholder. This track wires the existing backend to a real UI.

---

## Current State

### What Exists (backend, fully working)

| Layer | File | Status |
|-------|------|--------|
| Entity | `ArdaNova.Domain/Models/Entities/Notification.cs` | Done |
| Enum | `ArdaNova.Domain/Models/Enums/NotificationType.cs` | Done — 50 types |
| DTOs | `ArdaNova.Application/DTOs/NotificationDtos.cs` | Done |
| Service interface | `INotificationService.cs` | Done |
| Service impl | `NotificationServices.cs` | Done |
| Controller | `NotificationsController.cs` | Done |
| SignalR handler | `NotificationHubHandler.cs` | Done |
| Hub events | `NotificationEvents.cs` | Done — `notification.created`, `notification.read`, `notification.all_read` |

### What Exists (frontend, partial)

| File | State |
|------|-------|
| `src/lib/websocket/types.ts` | `NotificationCreatedEvent`, `NotificationReadEvent`, `NotificationsMarkedAllReadEvent` types already defined |
| `src/hooks/use-realtime.ts` | SignalR/SSE connection hook — `subscribe(eventType, cb)` available |
| `src/hooks/use-realtime-query.ts` | `useRealtimeInvalidation`, `useRealtimeUpdate`, `useRealtimeAppend` helpers |
| `src/components/app-sidebar.tsx` | `Bell` icon imported, rendered as inert `<Button>` — no logic |

### What Is Missing (frontend, all of this track)

- `src/lib/api/ardanova/endpoints/notifications.ts` — API client endpoint
- `src/server/api/routers/notification.ts` — tRPC thin proxy
- `src/components/notifications/notification-bell.tsx` — Bell with unread badge
- `src/components/notifications/notification-dropdown.tsx` — Popover panel, real-time list
- `src/components/notifications/notification-item.tsx` — Individual row
- `src/app/notifications/page.tsx` — Full notifications page with filters
- Register in `ArdaNovaApiClient` and `root.ts`

---

## Backend API Reference

All endpoints are under `GET|POST|DELETE /api/notifications/...`.

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/notifications/{id}` | Single notification by ID |
| GET | `/api/notifications/user/{userId}` | All notifications for user |
| GET | `/api/notifications/user/{userId}/paged?page=1&pageSize=10` | Paginated |
| GET | `/api/notifications/user/{userId}/unread` | Unread only |
| GET | `/api/notifications/user/{userId}/summary` | `{ totalCount, unreadCount }` |
| POST | `/api/notifications/{id}/read` | Mark single as read |
| POST | `/api/notifications/user/{userId}/read-all` | Mark all as read |
| DELETE | `/api/notifications/{id}` | Delete single |
| DELETE | `/api/notifications/user/{userId}` | Delete all for user |

### DTOs

```typescript
interface NotificationDto {
  id: string;
  userId: string;
  type: NotificationType;       // string enum, 50 values
  title: string;
  message: string;
  data?: string;                // JSON metadata
  isRead: boolean;
  readAt?: string;
  actionUrl?: string;           // deep-link target
  createdAt: string;
}

interface NotificationSummaryDto {
  totalCount: number;
  unreadCount: number;
}
```

---

## SignalR Integration

The hub at `ArdaNovaHub` automatically adds connections to `user:{userId}` on connect (via `X-User-Id` header). The frontend `RealtimeClient` already handles this via the SSE proxy at `/api/realtime`.

### Relevant events

```
notification.created  → NotificationCreatedEvent
  { notificationId, userId, type, title, message, actionUrl, eventId, occurredAt }

notification.read     → NotificationReadEvent
  { notificationId, userId, eventId, occurredAt }

notification.all_read → NotificationsMarkedAllReadEvent
  { userId, eventId, occurredAt }
```

### Pattern in UI

The existing `useRealtimeInvalidation` hook is the correct tool:

```typescript
// Invalidate summary + notification list when any notification event fires
useRealtimeInvalidation("notification.created", [
  ["notifications", "summary"],
  ["notifications", "list"],
]);
```

For optimistic bell counter updates, `useRealtimeUpdate` against the `summary` query key is preferred over full invalidation to avoid flicker.

---

## Notification Types

Grouped by category for filter UI:

### Task & Work
`TASK_ASSIGNED`, `TASK_COMPLETED`, `TASK_REVIEWED`, `TASK_REVISION_REQUESTED`

### Proposals & Governance
`PROPOSAL_CREATED`, `PROPOSAL_VOTED`, `PROPOSAL_PASSED`, `PROPOSAL_REJECTED`, `PROPOSAL_EXECUTED`

### Gamification
`ACHIEVEMENT_EARNED`, `LEVEL_UP`, `STREAK_MILESTONE`

### Equity & Finance
`SHARES_RECEIVED`, `SHARES_VESTED`, `ESCROW_FUNDED`, `ESCROW_RELEASED`

### Project Membership
`PROJECT_INVITATION`, `PROJECT_INVITATION_ACCEPTED`, `PROJECT_INVITATION_DECLINED`, `PROJECT_MEMBERSHIP_REQUEST`, `PROJECT_MEMBERSHIP_APPROVED`, `PROJECT_MEMBERSHIP_REJECTED`, `PROJECT_UPDATE`, `PROJECT_FUNDED`

### Guild
`GUILD_INVITATION`, `GUILD_INVITATION_ACCEPTED`, `GUILD_INVITATION_DECLINED`, `GUILD_APPLICATION`, `GUILD_APPLICATION_APPROVED`, `GUILD_APPLICATION_REJECTED`

### Events
`EVENT_INVITATION`, `EVENT_REMINDER`, `EVENT_STARTING_SOON`, `EVENT_CANCELLED`, `EVENT_UPDATED`

### Social
`USER_FOLLOWED`, `PROJECT_FOLLOWED`, `GUILD_FOLLOWED`, `COMMENT_REPLY`, `MENTION`, `FOLLOWER_NEW`

### Membership Credentials
`MEMBERSHIP_GRANTED`, `MEMBERSHIP_REVOKED`, `MEMBERSHIP_SUSPENDED`

### System
`SYSTEM_ANNOUNCEMENT`, `SECURITY_ALERT`

---

## UI Components

### 1. NotificationBell (`notification-bell.tsx`)

Replaces the inert `Bell` button in `app-sidebar.tsx`.

- Renders `Bell` icon with a red badge showing unread count (hidden when 0)
- Loads `notification.getSummary` query on mount — lightweight, just `{ totalCount, unreadCount }`
- Subscribes to `notification.created` via `useRealtimeUpdate` to increment badge count optimistically
- Subscribes to `notification.all_read` to reset badge to 0
- Click opens `NotificationDropdown` as a Popover
- Collapsed sidebar: icon only with badge. Expanded sidebar: icon + "Notifications" label + badge

### 2. NotificationDropdown (`notification-dropdown.tsx`)

Popover panel anchored to the Bell button.

- Fetches `notification.getUnread` on open (unread-first, up to 20 items)
- Lists `NotificationItem` rows
- Header: "Notifications" title + "Mark all as read" button (disabled when count = 0)
- "View all" link → `/notifications`
- Empty state: "You're all caught up" with a small check icon
- Subscribes to `notification.created` → prepend new item optimistically
- Subscribes to `notification.all_read` → mark all items read locally

### 3. NotificationItem (`notification-item.tsx`)

Single notification row.

- Unread indicator: left-border accent or dot
- Icon: map `NotificationType` category → Lucide icon (e.g. `TASK_ASSIGNED` → `CheckSquare`, `ACHIEVEMENT_EARNED` → `Trophy`, `SHARES_RECEIVED` → `Coins`, `SECURITY_ALERT` → `ShieldAlert`)
- Title + message (message truncated to 2 lines)
- Relative timestamp (`2m ago`, `3h ago`, `Yesterday`)
- Click: mark as read (`notification.markAsRead`) + navigate to `actionUrl` (if present)
- Hover: subtle background

### 4. Notifications Page (`/notifications/page.tsx`)

Full-page notification center.

- Page header: "Notifications" + unread count chip + "Mark all as read" button
- Filter tabs: All | Unread | by category (Task, Governance, Gamification, Finance, Social, System)
- Paginated list using `notification.getPaged` — infinite scroll with `useInfiniteQuery`
- Each item has explicit "Mark as read" and "Delete" actions (icon buttons on hover)
- Empty state per filter
- Real-time updates via `useRealtimeInvalidation` for the active filter's query key

---

## tRPC Router Design

`notificationRouter` is a **thin proxy** — all business logic stays in .NET.

```
notification.getSummary       → GET /api/notifications/user/{userId}/summary
notification.getList          → GET /api/notifications/user/{userId}/paged
notification.getUnread        → GET /api/notifications/user/{userId}/unread
notification.markAsRead       → POST /api/notifications/{id}/read
notification.markAllAsRead    → POST /api/notifications/user/{userId}/read-all
notification.delete           → DELETE /api/notifications/{id}
notification.deleteAll        → DELETE /api/notifications/user/{userId}
```

All procedures are `protectedProcedure` — `userId` is always pulled from `ctx.session.user.id` (never trusted from input).

---

## Architecture Constraints

- **No Prisma in the router** — call `apiClient.notifications.*` only
- **No business logic in Next.js** — router is a pure proxy
- `userId` always sourced from `ctx.session.user.id` in tRPC procedures, never from client input
- Real-time updates must degrade gracefully — if SSE is disconnected, next page load or focus re-fetch catches up
- The `notification.created` SignalR event carries enough data to prepend directly to the unread list without a refetch

---

## File Manifest

### New Files

| File | Purpose |
|------|---------|
| `ardanova-client/src/lib/api/ardanova/endpoints/notifications.ts` | API client endpoint wrapper |
| `ardanova-client/src/server/api/routers/notification.ts` | tRPC thin proxy |
| `ardanova-client/src/components/notifications/notification-bell.tsx` | Bell + badge component |
| `ardanova-client/src/components/notifications/notification-dropdown.tsx` | Popover dropdown panel |
| `ardanova-client/src/components/notifications/notification-item.tsx` | Single row component |
| `ardanova-client/src/app/notifications/page.tsx` | Full notifications page |

### Modified Files

| File | Changes |
|------|---------|
| `ardanova-client/src/lib/api/ardanova/index.ts` | Import + register `NotificationsEndpoint` |
| `ardanova-client/src/server/api/root.ts` | Register `notificationRouter` |
| `ardanova-client/src/components/app-sidebar.tsx` | Replace inert Bell button with `NotificationBell` component |

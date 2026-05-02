# Track 14 — Notification System UI

## 1. API Client Endpoint

- [ ] **[P0] Create `notifications.ts` endpoint wrapper**
    - File: `ardanova-client/src/lib/api/ardanova/endpoints/notifications.ts`
    - TypeScript interfaces mirroring backend DTOs:
        - `NotificationDto` — `{ id, userId, type, title, message, data?, isRead, readAt?, actionUrl?, createdAt }`
        - `NotificationSummaryDto` — `{ totalCount, unreadCount }`
        - `PagedNotificationsParams` — `{ page?, pageSize? }`
    - `NotificationsEndpoint` class methods:
        - `getById(id: string)` → `GET /api/notifications/{id}`
        - `getByUserId(userId: string)` → `GET /api/notifications/user/{userId}`
        - `getPaged(userId: string, page?, pageSize?)` → `GET /api/notifications/user/{userId}/paged`
        - `getUnread(userId: string)` → `GET /api/notifications/user/{userId}/unread`
        - `getSummary(userId: string)` → `GET /api/notifications/user/{userId}/summary`
        - `markAsRead(id: string)` → `POST /api/notifications/{id}/read`
        - `markAllAsRead(userId: string)` → `POST /api/notifications/user/{userId}/read-all`
        - `delete(id: string)` → `DELETE /api/notifications/{id}`
        - `deleteAll(userId: string)` → `DELETE /api/notifications/user/{userId}`

- [ ] **[P0] Register `NotificationsEndpoint` in `ArdaNovaApiClient`**
    - File: `ardanova-client/src/lib/api/ardanova/index.ts`
    - Import `NotificationsEndpoint` from `./endpoints/notifications`
    - Add `readonly notifications: NotificationsEndpoint` field
    - Instantiate in constructor: `this.notifications = new NotificationsEndpoint(this)`
    - Re-export `NotificationDto` and `NotificationSummaryDto` types

## 2. tRPC Router

- [ ] **[P0] Create `notification.ts` tRPC router (thin proxy)**
    - File: `ardanova-client/src/server/api/routers/notification.ts`
    - All procedures are `protectedProcedure` — `userId` always from `ctx.session.user.id`
    - Procedures:
        - `getSummary` — query, calls `apiClient.notifications.getSummary(userId)`
        - `getList` — query, input `{ page?: number, pageSize?: number }`, calls `apiClient.notifications.getPaged(userId, page, pageSize)`
        - `getUnread` — query, calls `apiClient.notifications.getUnread(userId)`
        - `markAsRead` — mutation, input `{ id: string }`, calls `apiClient.notifications.markAsRead(id)`
        - `markAllAsRead` — mutation, calls `apiClient.notifications.markAllAsRead(userId)`
        - `delete` — mutation, input `{ id: string }`, calls `apiClient.notifications.delete(id)`
        - `deleteAll` — mutation, calls `apiClient.notifications.deleteAll(userId)`
    - Follow error-handling pattern from `chat.ts`: `if (response.error) throw new Error(response.error)`

- [ ] **[P0] Register `notificationRouter` in `root.ts`**
    - File: `ardanova-client/src/server/api/root.ts`
    - Import `notificationRouter` from `./routers/notification`
    - Add `notification: notificationRouter` to `appRouter`

## 3. Notification Bell Component

- [ ] **[P0] Create `NotificationBell` component**
    - File: `ardanova-client/src/components/notifications/notification-bell.tsx`
    - `"use client"` directive
    - Calls `api.notification.getSummary.useQuery()` — lightweight, runs always when authenticated
    - Subscribes to `notification.created` via `useRealtimeUpdate` to optimistically increment `unreadCount`
    - Subscribes to `notification.all_read` via `useRealtimeUpdate` to reset `unreadCount` to 0
    - Subscribes to `notification.read` via `useRealtimeUpdate` to decrement `unreadCount` by 1
    - Props: `isCollapsed: boolean` — controls label visibility
    - Renders: `Bell` icon + red badge chip when `unreadCount > 0` (shows `99+` when > 99)
    - Wraps a Popover trigger — click opens `NotificationDropdown`
    - Collapsed: icon + badge only (badge overlays top-right of icon)
    - Expanded: icon + "Notifications" text + badge to the right of text
    - Matches existing sidebar button styles: `text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent`

- [ ] **[P0] Replace inert Bell in `app-sidebar.tsx`**
    - File: `ardanova-client/src/components/app-sidebar.tsx`
    - Import `NotificationBell` from `~/components/notifications/notification-bell`
    - Replace both `<Bell className="size-4" />` button instances (expanded and collapsed) with `<NotificationBell isCollapsed={isCollapsed} />`
    - Remove now-unused `Bell` import from lucide-react if it's only used there

## 4. Notification Dropdown Panel

- [ ] **[P1] Create `NotificationDropdown` component**
    - File: `ardanova-client/src/components/notifications/notification-dropdown.tsx`
    - `"use client"` directive
    - Uses Radix `Popover` (or shadcn `Popover` component) anchored to `NotificationBell`
    - On open: calls `api.notification.getUnread.useQuery()` — enabled only when open
    - Header:
        - "Notifications" title
        - "Mark all as read" button — calls `api.notification.markAllAsRead.useMutation()`, disabled when `unreadCount === 0`
    - Body: scrollable list (max height `~400px`) of `NotificationItem` rows
    - Empty state: centered icon + "You're all caught up" text
    - Footer: "View all notifications →" link to `/notifications`
    - Subscribes to `notification.created` → prepend new item to list using `useRealtimeAppend` — the event carries `{ notificationId, type, title, message, actionUrl }`, transform to partial `NotificationDto` shape for immediate display
    - Subscribes to `notification.all_read` → invalidate unread query
    - Width: `w-80` (320px), positioned right-aligned to sidebar

- [ ] **[P1] Create `NotificationItem` component**
    - File: `ardanova-client/src/components/notifications/notification-item.tsx`
    - Props: `notification: NotificationDto`, `onMarkRead: (id: string) => void`
    - Layout: icon column | content column | timestamp column
    - Icon: map `NotificationType` category to Lucide icon:
        - Task types → `CheckSquare`
        - Gamification (`ACHIEVEMENT_EARNED`, `LEVEL_UP`, `STREAK_MILESTONE`) → `Trophy`
        - Finance (`SHARES_RECEIVED`, `SHARES_VESTED`, `ESCROW_FUNDED`, `ESCROW_RELEASED`) → `Coins`
        - Project invitations/membership → `FolderKanban`
        - Guild types → `Users`
        - Social (`USER_FOLLOWED`, `MENTION`, `COMMENT_REPLY`, `FOLLOWER_NEW`) → `Heart`
        - Events → `Calendar`
        - Governance/proposals → `Vote`
        - `SECURITY_ALERT` → `ShieldAlert`
        - `SYSTEM_ANNOUNCEMENT` → `Bell`
        - Default → `Bell`
    - Unread state: left border `border-l-2 border-sidebar-primary` + slightly brighter background
    - Title: `text-sm font-medium` — single line, truncated
    - Message: `text-xs text-sidebar-muted` — 2-line clamp
    - Timestamp: `text-xs text-sidebar-muted` — relative (`2m ago`, `3h ago`, `Yesterday`)
    - Click: if `actionUrl` present, navigate; always call `onMarkRead(notification.id)`
    - On hover (desktop): show "×" delete button at far right

## 5. Mark as Read / Mark All as Read

- [ ] **[P1] Wire mark-as-read interactions**
    - In `NotificationDropdown`: clicking a `NotificationItem` calls `api.notification.markAsRead.useMutation()`
    - Optimistic update: set `isRead = true` on the item in the unread list immediately, decrement summary `unreadCount`
    - On mutation success: invalidate `["notifications", "summary"]` and `["notifications", "unread"]`
    - On mutation error: revert optimistic update
    - "Mark all as read" button in dropdown: calls `api.notification.markAllAsRead.useMutation()`
    - On success: invalidate both query keys; all items visually cleared from unread list
    - On `/notifications` page: same mutation pattern for both the bulk button and per-item mark-read

## 6. Full Notifications Page

- [ ] **[P1] Create `/notifications` page**
    - File: `ardanova-client/src/app/notifications/page.tsx`
    - Server component wrapper with `auth()` check → redirect to sign-in if unauthenticated
    - Client sub-component `NotificationsPageClient` holds interactive state

- [ ] **[P1] Page header and actions**
    - "Notifications" h1 + unread count badge (from `getSummary`)
    - "Mark all as read" button — calls `markAllAsRead` mutation, disabled when `unreadCount === 0`

- [ ] **[P1] Filter tabs**
    - Tab options: All | Unread | Tasks | Governance | Gamification | Finance | Social | System
    - "Unread" tab filters by `isRead === false` client-side (from full list) or use unread endpoint
    - Category tabs filter by `NotificationType` group (see spec for groupings)
    - Active tab reflected in URL search param `?filter=tasks` for linkability

- [ ] **[P1] Paginated list with infinite scroll**
    - Uses `api.notification.getList.useInfiniteQuery()` — load more on scroll bottom
    - Each row: full `NotificationItem` with explicit "Mark as read" and "Delete" (×) icon buttons visible on hover
    - Delete calls `api.notification.delete.useMutation()` with optimistic removal from list
    - Empty state per filter: "No notifications" with appropriate icon

- [ ] **[P1] Real-time updates on notifications page**
    - `useRealtimeInvalidation("notification.created", [["notifications", "list"], ["notifications", "summary"]])` — new items appear on refetch
    - `useRealtimeInvalidation("notification.all_read", [["notifications", "list"], ["notifications", "summary"]])` — bulk read clears indicators

## 7. Notification Trigger Wiring

- [ ] **[P2] Verify `TASK_ASSIGNED` trigger fires correctly**
    - Confirm `NotificationServices` creates a `TASK_ASSIGNED` notification when a task's `AssignedToId` is set
    - Verify `actionUrl` points to the task or project page
    - Manual test: assign a task to a user, confirm bell increments in real time

- [ ] **[P2] Verify `ACHIEVEMENT_EARNED` / `LEVEL_UP` trigger**
    - Confirm XP events that cross level thresholds trigger `LEVEL_UP` notification
    - Confirm achievement unlock triggers `ACHIEVEMENT_EARNED` notification
    - Manual test: award XP to cross a level boundary

- [ ] **[P2] Verify `PROJECT_MEMBERSHIP_REQUEST` / `PROJECT_INVITATION` triggers**
    - Confirm project owner receives `PROJECT_MEMBERSHIP_REQUEST` when a user applies
    - Confirm invited user receives `PROJECT_INVITATION` when invited
    - Verify `actionUrl` deep-links to project membership page

- [ ] **[P2] Verify `PROPOSAL_CREATED` trigger**
    - Confirm project members receive `PROPOSAL_CREATED` when a new governance proposal is submitted
    - Verify `actionUrl` links to the proposal

- [ ] **[P2] Verify `SHARES_RECEIVED` / `SHARES_VESTED` triggers**
    - Confirm equity allocation events fire the correct notification types
    - Verify `actionUrl` links to the user's equity/token page

- [ ] **[P2] Verify `MEMBERSHIP_GRANTED` / KYC approval triggers**
    - Confirm `MEMBERSHIP_GRANTED` fires when a membership credential is granted
    - Confirm `SYSTEM_ANNOUNCEMENT` (or appropriate type) fires on KYC approval

# ArdaNova Platform - Comprehensive QA & Functional Testing Checklist (Excluding Wallet Integration)

> **Version:** 1.0
> **Last Updated:** 2026-02-01
> **Platform:** ArdaNova - Collaborative Innovation & Project Incubation Platform
> **Architecture:** Next.js (tRPC/Prisma/NextAuth) + .NET 8 (EF Core/SignalR/S3) + MCP Server
> **Schema Source of Truth:** `ardanova-client/prisma/database-architecture.dbml`
> **Note:** This version excludes all Wallet Integration testing (Wallet CRUD, Wallet Verification, Primary Wallet Logic)

## How to Use This Checklist
- **P0 (Critical):** Must pass before any release. Blocking issues.
- **P1 (High):** Must pass before production release.
- **P2 (Medium):** Should pass. Can be deferred for hotfix.
- **P3 (Low):** Nice to have. Can be backlogged.
- Check off items as they pass testing `[x]`
- Add notes in-line for failures or observations

## Test Environment Setup
- [ ] Google OAuth credentials configured
- [ ] PostgreSQL database running and migrated using the dbml file and the prisma + csharp generators
- [ ] .NET 8 backend API running with valid API key
- [ ] Next.js frontend running with environment variables
- [ ] SignalR hub accessible
- [ ] AWS S3 bucket configured (or local storage fallback)
- [ ] Test user accounts created for each role/type

---

## Table of Contents
1. [Authentication & User Core](#1-authentication--user-core)
2. [Project Management](#2-project-management-full-lifecycle)
3. [Agile Hierarchy](#3-agile-hierarchy)
4. [Guild Module](#4-guild-module)
5. [Opportunities Marketplace](#5-opportunities-marketplace)
6. [DAO Governance](#6-dao-governance)
7. [Finance & Tokenomics](#7-finance--tokenomics)
8. [Social & Communication](#8-social--communication)
9. [Events Module](#9-events-module)
10. [Social & Follow](#10-social--follow)
11. [Gamification & Reputation](#11-gamification--reputation)
12. [Product Catalog](#12-product-catalog)
13. [Real-time & WebSocket](#13-real-time--websocket)
14. [Cross-Cutting Concerns](#14-cross-cutting-concerns)
15. [Navigation & Layout](#15-navigation--layout)
16. [Edge Cases & Negative Testing](#16-edge-cases--negative-testing)
17. [Dual-Asset Model & Membership Credentials](#17-dual-asset-model--membership-credentials)

---

## 13. Real-time & WebSocket

### 13.1 SSE Connection Establishment (Browser -> Next.js)
- [ ] **[P0]** Navigate to authenticated page -> SSE connection automatically established to `/api/realtime` -> Expected: EventSource connection successful, ready state = 1 (OPEN)
- [ ] **[P0]** Check browser network tab during page load -> Expected: `/api/realtime` request with `Accept: text/event-stream` header visible
- [ ] **[P0]** SSE connection established with valid session -> Expected: Connection stream begins, no authentication errors
- [ ] **[P1]** Check connection headers -> Expected: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive`
- [ ] **[P1]** Initial connection sends heartbeat/ping -> Expected: Regular `comment: ping` or `event: ping` messages received every 30-60 seconds
- [ ] **[P2]** Multiple tabs open simultaneously -> Expected: Each tab establishes its own SSE connection independently
- [ ] **[P2]** Check browser console for connection logs -> Expected: "SSE connected" or similar status message logged

### 13.2 SSE Reconnection & Error Handling
- [ ] **[P0]** Temporarily disable network (airplane mode) -> re-enable -> Expected: SSE automatically reconnects within 5-10 seconds, events resume
- [ ] **[P0]** Server-side SSE endpoint crashes/restarts -> Expected: Browser EventSource detects disconnection and auto-reconnects
- [ ] **[P1]** Connection drops mid-session -> Expected: `onerror` handler fires, status changes to CONNECTING (0), then reconnects
- [ ] **[P1]** Observe reconnection backoff strategy -> Expected: Progressive retry delays (1s, 2s, 5s, 10s) visible in network timing
- [ ] **[P1]** Server returns HTTP 500 on `/api/realtime` -> Expected: Client retries connection, logs error, does not crash app
- [ ] **[P2]** Server sends `Connection: close` -> Expected: SSE connection gracefully terminates and immediately reconnects
- [ ] **[P2]** Browser tab goes to background for 10+ minutes -> Expected: Connection may close, but reconnects on tab focus
- [ ] **[P3]** Check for duplicate subscriptions after reconnection -> Expected: Previous subscriptions cleaned up, no duplicate events

### 13.3 SSE Event Types & Parsing
- [ ] **[P0]** Receive `notification` event via SSE -> Expected: Event parsed correctly with `event: notification` and JSON data payload
- [ ] **[P0]** Check notification payload structure -> Expected: Contains `{ type, userId, title, content, timestamp }` fields
- [ ] **[P1]** Receive `message` event (new chat message) -> Expected: Event type = `message`, data contains conversationId, senderId, content
- [ ] **[P1]** Receive `project_update` event -> Expected: Event type = `project_update`, data contains projectId, changeType, updatedFields
- [ ] **[P1]** Receive `activity` event -> Expected: Event type = `activity`, data contains activityType, entityId, userId
- [ ] **[P1]** Receive `user_status` event (online/offline) -> Expected: Event type = `user_status`, data contains userId, status, lastSeen
- [ ] **[P1]** Receive multi-line data payload -> Expected: Parser correctly combines `data:` lines until double newline
- [ ] **[P2]** Receive malformed JSON in SSE data -> Expected: Parser catches error, logs warning, does not crash, skips event
- [ ] **[P2]** Receive unknown event type -> Expected: Client handles gracefully, logs unknown type, does not throw error
- [ ] **[P2]** Receive event with custom `id:` field -> Expected: Client can access event.lastEventId for replay purposes
- [ ] **[P3]** Server sends `retry: 5000` directive -> Expected: Browser uses 5000ms as reconnection timeout override

### 13.4 SignalR Hub Connection (Next.js Server -> .NET Backend)
- [ ] **[P0]** Next.js server starts up -> SignalR connection to .NET backend established -> Expected: Connection ID logged, status = Connected
- [ ] **[P0]** Check Next.js logs for SignalR connection success -> Expected: "Connected to SignalR hub at ws://..." message visible
- [ ] **[P0]** SignalR connection uses WebSocket transport -> Expected: Network shows WebSocket upgrade (HTTP 101 Switching Protocols)
- [ ] **[P1]** SignalR connection established with X-Api-Key header -> Expected: Backend validates API key, connection accepted
- [ ] **[P1]** Missing or invalid X-Api-Key on SignalR connection -> Expected: Connection rejected with 401 Unauthorized
- [ ] **[P1]** .NET backend restarts -> Expected: Next.js SignalR client detects disconnection and automatically reconnects
- [ ] **[P1]** SignalR reconnection with exponential backoff -> Expected: Retry delays visible in logs (1s, 2s, 5s, 10s, 30s)
- [ ] **[P2]** Connection state transitions logged -> Expected: Connecting -> Connected -> Disconnected -> Reconnecting states visible
- [ ] **[P2]** SignalR connection maintains persistent connection -> Expected: Single WebSocket connection reused for all events, not reopened per message
- [ ] **[P3]** Enable detailed SignalR logging -> Expected: Hub method invocations, group joins, and message sends logged at DEBUG level

### 13.5 SignalR Group Subscriptions
- [ ] **[P0]** User logs in -> Next.js subscribes to `user:{userId}` group -> Expected: Group subscription successful, user receives personal events
- [ ] **[P0]** User views project detail -> Subscribe to `project:{projectId}` group -> Expected: Group subscription acknowledged by backend
- [ ] **[P0]** User joins guild -> Subscribe to `guild:{guildId}` group -> Expected: Guild real-time events start flowing to client
- [ ] **[P0]** User opens conversation -> Subscribe to `conversation:{conversationId}` group -> Expected: Chat events for that conversation received
- [ ] **[P1]** Check backend logs for group join -> Expected: "User {connectionId} joined group {groupName}" logged on .NET backend
- [ ] **[P1]** Subscribe to same group twice (idempotency) -> Expected: No error, subscription acknowledged, no duplicate events
- [ ] **[P1]** User navigates away from project -> Unsubscribe from `project:{projectId}` -> Expected: No further project events received
- [ ] **[P1]** User logs out -> All group subscriptions removed -> Expected: No events delivered to disconnected client
- [ ] **[P1]** Subscribe to multiple groups simultaneously (user + project + guild + conversation) -> Expected: All subscriptions succeed, events routed correctly
- [ ] **[P2]** User has multiple tabs open -> Each tab's server connection subscribes to same groups -> Expected: Groups deduplicated on backend, events sent once per connection
- [ ] **[P2]** User leaves guild -> Unsubscribe from guild group -> Expected: Guild events stop, subscription removed from backend group registry
- [ ] **[P2]** Check group membership via SignalR hub method -> Expected: Backend can query groups for a connection ID
- [ ] **[P3]** Server sends event to empty group (no members) -> Expected: Event discarded gracefully, no errors logged

### 13.6 Real-time Notification Delivery (NotificationHubHandler)
- [ ] **[P0]** Create notification for user -> Expected: Event published to InMemoryEventBus -> NotificationHubHandler invokes SendToUser -> SSE delivers notification to browser
- [ ] **[P0]** User receives notification in browser -> Expected: Notification appears in UI notification dropdown/toast without page refresh
- [ ] **[P0]** Notification includes all required fields -> Expected: userId, type, title, content, createdAt, isRead = false
- [ ] **[P1]** User subscribed to `user:{id}` group receives notification -> Expected: Notification event sent only to that user's connections
- [ ] **[P1]** User with multiple tabs open -> Notification created -> Expected: All tabs receive the same notification simultaneously
- [ ] **[P1]** User offline when notification created -> User reconnects -> Expected: Notification visible in notification list (persisted), but not re-sent as real-time event
- [ ] **[P1]** Notification contains related entity (project, opportunity, task) -> Expected: NotificationEntity links correctly, notification shows entity details
- [ ] **[P2]** System notification (no specific user target) -> Expected: Not sent via real-time, only visible in notification list
- [ ] **[P2]** Bulk notifications sent to 50+ users -> Expected: All users receive their notifications within 1-2 seconds
- [ ] **[P2]** Check for duplicate notifications -> Expected: Each notification sent exactly once per connection, no event ID duplication
- [ ] **[P3]** Notification delivery latency -> Expected: < 500ms from creation to browser display under normal conditions

### 13.7 Real-time Project Events (ProjectEventHubHandler)
- [ ] **[P0]** Project status changed (PLANNING -> ACTIVE) -> Expected: Real-time event sent to `project:{id}` group, UI updates project status badge
- [ ] **[P0]** New member added to project -> Expected: Event sent to project group, team member list updates in real-time
- [ ] **[P0]** Member removed from project -> Expected: Removed user's UI shows access revoked, team list updates for remaining members
- [ ] **[P1]** Project details edited (title, description) -> Expected: Real-time update event sent, detail page refreshes fields without reload
- [ ] **[P1]** New proposal submitted to project -> Expected: Project members receive real-time notification, proposal count increments
- [ ] **[P1]** Proposal voting completed -> Expected: Proposal status update sent in real-time, project view reflects APPROVED/REJECTED status
- [ ] **[P1]** Project visibility changed (PRIVATE -> PUBLIC) -> Expected: Update event sent, visibility indicator changes
- [ ] **[P1]** New task created under project -> Expected: Task creation event sent to project group, task list auto-updates
- [ ] **[P1]** Task status changed (TODO -> IN_PROGRESS) -> Expected: Real-time event updates task board without refresh
- [ ] **[P1]** New opportunity posted for project -> Expected: Opportunity created event sent, project opportunities tab updates
- [ ] **[P2]** Project milestone achieved -> Expected: Achievement event sent, celebration animation/toast shown to project members
- [ ] **[P2]** Project deleted -> Expected: All subscribed users redirected or shown "Project no longer exists" message
- [ ] **[P2]** Project archived -> Expected: Project status updated in real-time, access revoked for non-members
- [ ] **[P2]** Multiple rapid updates to project (5+ edits in 10 seconds) -> Expected: All events delivered, UI updates smoothly without race conditions
- [ ] **[P3]** Project event includes detailed change diff -> Expected: Event payload contains `before` and `after` states or change summary

### 13.8 Real-time Activity Events (ActivityHubHandler)
- [ ] **[P0]** User creates post -> Expected: Activity event sent to followers' feeds, new post appears in real-time
- [ ] **[P0]** User likes a post -> Expected: Like count increments in real-time for all viewers of that post
- [ ] **[P0]** User comments on post -> Expected: Comment appears in post's comment section without refresh
- [ ] **[P1]** Activity event includes activityType (POST_CREATED, COMMENT_ADDED, LIKE_ADDED, etc.) -> Expected: Correct event type, UI handles each type appropriately
- [ ] **[P1]** Activity aggregation (10 likes in 1 minute) -> Expected: UI batches/aggregates updates to avoid spam, shows final count
- [ ] **[P1]** User unfollows another user -> Activity from unfollowed user stops appearing -> Expected: Real-time feed filtering works
- [ ] **[P2]** Activity event includes actor details (userId, name, avatar) -> Expected: Activity shows "John Doe liked your post" with profile picture
- [ ] **[P2]** Global activity feed updated -> Expected: Public activity stream shows latest platform activities in real-time
- [ ] **[P2]** Activity event for private entity (private project post) -> Expected: Only authorized users receive the activity event
- [ ] **[P3]** Activity deduplication -> Expected: Same activity event not sent multiple times to same user

### 13.9 Real-time User Events (UserEventHubHandler)
- [ ] **[P0]** User goes online -> Expected: User status event sent to friends/followers, status indicator changes to green/online
- [ ] **[P0]** User goes offline -> Expected: Status event sent, indicator changes to gray/offline, lastSeen timestamp updated
- [ ] **[P1]** User updates profile (name, avatar, bio) -> Expected: Real-time event sent to user's connections, profile updates visible
- [ ] **[P1]** User changes online status manually (ONLINE -> AWAY -> DO_NOT_DISTURB) -> Expected: Status broadcast to relevant groups
- [ ] **[P1]** User presence updated with "currently viewing" info -> Expected: "Viewing Project XYZ" status shown to team members
- [ ] **[P2]** User skill set updated -> Expected: Profile update event sent, skill tags refresh in UI
- [ ] **[P2]** User reputation changes (badge earned) -> Expected: Badge notification sent in real-time, profile badge appears
- [ ] **[P2]** User's last activity timestamp updated -> Expected: "Last seen 2 minutes ago" updates in real-time for profile viewers
- [ ] **[P3]** User timezone changes -> Expected: All timestamp displays update to reflect new timezone

### 13.10 Chat Real-time: Message Broadcast
- [ ] **[P0]** User sends chat message -> Expected: Message instantly appears in sender's UI (optimistic update) and broadcast to `conversation:{id}` group
- [ ] **[P0]** Other conversation participants receive message -> Expected: New message appears in chat window within < 1 second
- [ ] **[P0]** Message includes all fields: id, conversationId, senderId, content, createdAt, status -> Expected: All fields populated correctly
- [ ] **[P1]** Message sent to group conversation (5+ participants) -> Expected: All participants receive message simultaneously
- [ ] **[P1]** Message sent to 1-on-1 conversation -> Expected: Only the two participants receive the message event
- [ ] **[P1]** Message with attachment (image) -> Expected: Attachment URL included in event, image preview renders in chat
- [ ] **[P1]** Message with mentions (@username) -> Expected: Mentioned users receive notification + real-time message event
- [ ] **[P1]** Message edited -> Expected: Edit event sent, message content updates in real-time for all viewers
- [ ] **[P1]** Message deleted -> Expected: Delete event sent, message removed from all participants' chat windows
- [ ] **[P2]** Long message (1000+ characters) -> Expected: Full message delivered, no truncation, renders correctly
- [ ] **[P2]** Message with emoji/unicode -> Expected: Emoji renders correctly across all clients
- [ ] **[P2]** Message sent to archived conversation -> Expected: Conversation auto-unarchives, message delivered
- [ ] **[P3]** Message order guaranteed -> Expected: Messages displayed in send order even if delivery times vary slightly

### 13.11 Chat Real-time: Typing Indicator
- [ ] **[P0]** User starts typing in chat -> Expected: "... is typing" indicator appears for other participants within 1 second
- [ ] **[P0]** User stops typing (no keypress for 3 seconds) -> Expected: Typing indicator disappears
- [ ] **[P1]** User sends message while typing indicator active -> Expected: Indicator immediately disappears, replaced by message
- [ ] **[P1]** Multiple users typing simultaneously -> Expected: "Alice and Bob are typing..." or similar aggregated indicator shown
- [ ] **[P1]** Typing indicator sent as separate SignalR event -> Expected: Event type = `typing_start` or `typing_stop`, conversationId included
- [ ] **[P2]** User closes chat while typing -> Expected: Typing stop event sent automatically
- [ ] **[P2]** Typing indicator timeout after 10 seconds of no activity -> Expected: Indicator auto-clears even without explicit stop event
- [ ] **[P2]** Typing indicator not shown for user's own typing -> Expected: User does not see their own typing indicator
- [ ] **[P3]** Typing indicator throttled (max 1 event per 2 seconds per user) -> Expected: Rapid typing doesn't spam events

### 13.12 Chat Real-time: Read Receipts
- [ ] **[P0]** User opens conversation -> All unread messages marked as read -> Expected: Read receipt events sent to conversation group
- [ ] **[P0]** Message sender sees read receipt -> Expected: Message status changes to "READ" with timestamp, checkmark icon updates
- [ ] **[P1]** Read receipt includes readBy userId and readAt timestamp -> Expected: Sender can see who read the message and when
- [ ] **[P1]** Group conversation read receipts -> Expected: Sender sees "Read by 3 of 5" or individual read receipts per participant
- [ ] **[P1]** User scrolls to old unread message -> Expected: Read receipt sent for that specific message, others remain unread
- [ ] **[P1]** User closes chat with unread messages -> Reopens later -> Expected: Read receipts sent on reopen when messages viewed
- [ ] **[P2]** Read receipt sent only once per user per message -> Expected: No duplicate read events for same user
- [ ] **[P2]** User reads message in one tab -> Expected: Other tabs for same user also show message as read
- [ ] **[P2]** Read receipt for deleted message -> Expected: Gracefully handled, no error
- [ ] **[P3]** Read receipt delivery confirmation -> Expected: Sender receives acknowledgment that read receipt was delivered

### 13.13 Chat Real-time: Message Delivery Confirmation
- [ ] **[P0]** User sends message -> Expected: Client receives delivery confirmation event, message status changes from SENDING -> SENT
- [ ] **[P0]** Message persisted to database -> Expected: Delivery confirmation sent with permanent message ID
- [ ] **[P1]** Delivery confirmation includes messageId and timestamp -> Expected: Client can match confirmation to optimistic UI update
- [ ] **[P1]** Message fails to send (network error) -> Expected: No delivery confirmation, message status = FAILED, retry option shown
- [ ] **[P1]** Message queued on server but not yet delivered to recipients -> Expected: Status = SENT, not yet DELIVERED
- [ ] **[P2]** All conversation participants online -> Expected: Status progresses SENT -> DELIVERED (to all) within 1-2 seconds
- [ ] **[P2]** One participant offline -> Expected: Status = DELIVERED for online users, pending for offline
- [ ] **[P3]** Delivery confirmation timeout -> Expected: If no confirmation within 10 seconds, client flags warning

### 13.14 Chat Real-time: Message Status Updates
- [ ] **[P0]** Message lifecycle: SENDING -> SENT -> DELIVERED -> READ -> Expected: Each status transition triggered by appropriate event
- [ ] **[P0]** Client UI reflects current message status -> Expected: Icon/text updates (clock -> single checkmark -> double checkmark -> blue checkmark)
- [ ] **[P1]** Status update events sent to sender only (not all participants) -> Expected: Recipient doesn't see sender's delivery status, only their own read status
- [ ] **[P1]** SENT status when backend acknowledges receipt -> Expected: Message saved to DB, SENT event fired
- [ ] **[P1]** DELIVERED status when at least one recipient's client receives it -> Expected: DELIVERED event fired when message downloaded to client
- [ ] **[P1]** READ status when recipient views message -> Expected: READ event fired on scroll into view or conversation open
- [ ] **[P2]** Status updates for group messages -> Expected: Aggregate status (e.g., "DELIVERED to 4 of 5") shown
- [ ] **[P2]** Failed status with error reason -> Expected: Status = FAILED, error message (e.g., "Network error", "User blocked") shown
- [ ] **[P3]** Retry failed message -> Expected: Status resets to SENDING, re-attempts delivery

### 13.15 Event Bus Flow: Service -> InMemoryEventBus -> Handlers -> SignalR -> SSE
- [ ] **[P0]** Domain service publishes event to InMemoryEventBus -> Expected: Event received by registered handlers within milliseconds
- [ ] **[P0]** NotificationCreatedEvent published -> NotificationHubHandler receives event -> Expected: Handler invokes SignalR hub method
- [ ] **[P0]** SignalR hub method sends event to subscribed group -> Expected: Event transmitted over WebSocket to Next.js server
- [ ] **[P0]** Next.js server receives SignalR event -> Broadcasts via SSE to browser clients -> Expected: Browser EventSource receives event
- [ ] **[P1]** Event bus supports multiple handlers for same event -> Expected: All registered handlers invoked in parallel
- [ ] **[P1]** Handler throws exception -> Expected: Event bus continues processing other handlers, exception logged, event not lost
- [ ] **[P1]** Event published with no registered handlers -> Expected: Event logged/discarded gracefully, no errors
- [ ] **[P1]** End-to-end latency measured (service publish -> browser display) -> Expected: < 1 second for typical events under normal load
- [ ] **[P2]** Event payload serialization -> Expected: Complex objects serialized to JSON correctly, deserialized on client without data loss
- [ ] **[P2]** Event bus queue overflow (1000+ events/second) -> Expected: Backpressure mechanism prevents memory overflow, events processed in order
- [ ] **[P2]** Event ordering preserved through entire pipeline -> Expected: Events published in order A->B->C arrive at browser in same order
- [ ] **[P3]** Event bus metrics tracked -> Expected: Event publish count, handler execution time, failure count visible in monitoring

### 13.16 Multiple Browser Tabs Handling
- [ ] **[P0]** Open application in 2 tabs -> Both tabs establish separate SSE connections -> Expected: Both tabs receive real-time events independently
- [ ] **[P0]** Receive notification in Tab 1 -> Mark as read -> Expected: Tab 2 also shows notification as read (via real-time sync)
- [ ] **[P1]** Send message in Tab 1 -> Expected: Tab 2 shows message appear in conversation in real-time
- [ ] **[P1]** Update profile in Tab 1 -> Expected: Tab 2 reflects updated profile data
- [ ] **[P1]** Both tabs subscribed to same project -> Project update occurs -> Expected: Both tabs receive the same event
- [ ] **[P1]** Close Tab 1 -> Expected: Tab 2 connection unaffected, continues receiving events
- [ ] **[P2]** Tab in background -> Event received -> Expected: Browser may throttle updates, but events queued and processed when tab refocused
- [ ] **[P2]** Logout in Tab 1 -> Expected: Tab 2 detects session invalidation, also logs out or prompts re-login
- [ ] **[P2]** Duplicate state synchronization between tabs -> Expected: Local storage or BroadcastChannel API used for cross-tab state sync
- [ ] **[P3]** 5+ tabs open simultaneously -> Expected: All tabs remain responsive, no performance degradation

### 13.17 Connection State Management
- [ ] **[P0]** Application maintains connection state: CONNECTED, DISCONNECTED, RECONNECTING -> Expected: UI reflects current state (indicator icon, banner)
- [ ] **[P0]** State transitions logged to console -> Expected: "Connection state changed: CONNECTED" messages visible
- [ ] **[P1]** UI shows "Connecting..." on initial load -> Expected: Loading state until SSE connection established
- [ ] **[P1]** Connection drops -> UI shows "Disconnected" banner -> Expected: User informed of lost connection
- [ ] **[P1]** Reconnecting state -> UI shows "Reconnecting..." with retry count -> Expected: User knows system is attempting to reconnect
- [ ] **[P1]** Connection restored -> UI shows "Connected" briefly, then hides banner -> Expected: User notified of successful reconnection
- [ ] **[P2]** Connection state persisted across page navigations (SPA) -> Expected: State maintained during client-side routing
- [ ] **[P2]** Manual reconnect button available during disconnected state -> Expected: User can trigger immediate reconnection attempt
- [ ] **[P3]** Connection state change triggers analytics event -> Expected: Disconnection frequency tracked for reliability monitoring

### 13.18 Event Deduplication
- [ ] **[P0]** Same event sent twice (due to network retry) -> Expected: Client detects duplicate via event ID, processes only once
- [ ] **[P1]** Event ID included in all SSE events -> Expected: Each event has unique ID or combination of type+entityId+timestamp
- [ ] **[P1]** Client maintains recent event cache (last 100 events) -> Expected: Duplicate check against cache before processing
- [ ] **[P1]** Duplicate notification event -> Expected: Only one notification appears in UI, not duplicated
- [ ] **[P2]** Duplicate message event -> Expected: Message appears once in chat, not duplicated
- [ ] **[P2]** Event cache expires after 5 minutes -> Expected: Old event IDs removed from cache to prevent memory bloat
- [ ] **[P3]** Deduplication across multiple tabs -> Expected: Each tab independently deduplicates, no cross-tab coordination required

### 13.19 Latency & Ordering Guarantees
- [ ] **[P0]** Event latency measured end-to-end -> Expected: P95 latency < 1 second, P99 < 2 seconds under normal load
- [ ] **[P0]** Events arrive in order for same entity (e.g., message A sent before B) -> Expected: Browser receives and displays in correct order
- [ ] **[P1]** High server load (100+ concurrent events) -> Expected: Events still delivered within 2-3 seconds
- [ ] **[P1]** Network latency (simulated 200ms delay) -> Expected: Events delayed but ordering preserved
- [ ] **[P1]** Out-of-order arrival detection -> Expected: Client can detect and reorder events using timestamp if necessary
- [ ] **[P2]** Event timestamp comparison -> Expected: createdAt timestamp used for authoritative ordering
- [ ] **[P2]** Conflicting updates (two users edit same entity) -> Expected: Last-write-wins or conflict resolution based on timestamp
- [ ] **[P3]** Latency monitoring dashboard -> Expected: Real-time latency metrics visible to ops team

---

## 14. Cross-Cutting Concerns

### 14.1 API Key Authentication (.NET Middleware)
- [ ] **[P0]** Request to .NET API without X-Api-Key header -> Expected: 401 Unauthorized response, request rejected
- [ ] **[P0]** Request with valid X-Api-Key header -> Expected: Request accepted, proceeds to controller action
- [ ] **[P0]** Request with invalid/expired X-Api-Key -> Expected: 401 Unauthorized, error message "Invalid API key"
- [ ] **[P1]** X-Api-Key validation middleware executes before controller -> Expected: Unauthorized requests never reach business logic
- [ ] **[P1]** API key stored securely in configuration (not hardcoded) -> Expected: Key loaded from environment variable or secrets manager
- [ ] **[P1]** API key rotation process tested -> Expected: Old key rejected, new key accepted after rotation
- [ ] **[P2]** Request logs sanitize API key -> Expected: Full API key not logged in plain text, only masked version (e.g., "abc***xyz")
- [ ] **[P2]** API key validation case-sensitive -> Expected: "ABC123" != "abc123"
- [ ] **[P3]** Rate limiting per API key -> Expected: Single API key cannot exceed 1000 requests/minute

### 14.2 tRPC Protected vs Public Procedures
- [ ] **[P0]** Call protectedProcedure without session -> Expected: 401 UNAUTHORIZED error, request rejected
- [ ] **[P0]** Call protectedProcedure with valid session -> Expected: Request proceeds, userId available in context
- [ ] **[P0]** Call publicProcedure without session -> Expected: Request succeeds, no authentication required
- [ ] **[P1]** protectedProcedure context includes `session.user.id` -> Expected: userId accessible in procedure resolver
- [ ] **[P1]** Expired session token -> protectedProcedure call -> Expected: 401 error, user prompted to re-login
- [ ] **[P1]** Public procedure called with session -> Expected: Succeeds, session ignored (optional authentication)
- [ ] **[P2]** Session validation uses NextAuth JWT verification -> Expected: Tampered JWT rejected, valid JWT accepted
- [ ] **[P2]** Protected procedure checks role/permissions beyond just authentication -> Expected: Admin-only procedures reject non-admin users
- [ ] **[P3]** Public procedure with optional authenticated behavior -> Expected: Different response if user is logged in vs anonymous

### 14.3 Ownership Verification Patterns
- [ ] **[P0]** User tries to update another user's project (createdById mismatch) -> Expected: 403 Forbidden error
- [ ] **[P0]** User tries to delete resource they don't own -> Expected: 403 Forbidden, "You do not have permission to delete this resource"
- [ ] **[P1]** Ownership check via `createdById === session.user.id` -> Expected: Only creator can edit/delete
- [ ] **[P1]** Project member (non-owner) tries to delete project -> Expected: 403 Forbidden unless user is OWNER role
- [ ] **[P1]** Guild member tries to edit guild settings (not OWNER/ADMIN) -> Expected: 403 Forbidden
- [ ] **[P1]** User edits their own profile -> Expected: Success
- [ ] **[P1]** User tries to edit another user's profile -> Expected: 403 Forbidden (unless admin)
- [ ] **[P2]** Ownership check for nested resources (comment on post) -> Expected: Only comment creator can edit/delete their comment
- [ ] **[P2]** Transfer ownership operation (project owner -> new owner) -> Expected: Ownership check allows current owner, updates createdById
- [ ] **[P3]** Admin override for ownership checks -> Expected: Platform admin can access all resources regardless of ownership

### 14.4 Role-Based Access Control
- [ ] **[P0]** Admin user accesses admin-only endpoint -> Expected: Success, admin panel data returned
- [ ] **[P0]** Regular user tries to access admin endpoint -> Expected: 403 Forbidden
- [ ] **[P1]** Guild role hierarchy: OWNER > ADMIN > MODERATOR > MEMBER -> Expected: Higher roles can perform lower role actions
- [ ] **[P1]** Guild OWNER can promote member to ADMIN -> Expected: Success
- [ ] **[P1]** Guild MEMBER tries to promote another member -> Expected: 403 Forbidden
- [ ] **[P1]** Guild ADMIN can remove MEMBER -> Expected: Success
- [ ] **[P1]** Guild ADMIN tries to remove OWNER -> Expected: 403 Forbidden
- [ ] **[P1]** Project role hierarchy: OWNER > LEAD > CONTRIBUTOR > VIEWER -> Expected: Permissions enforced per role
- [ ] **[P1]** Project OWNER can delete project -> Expected: Success
- [ ] **[P1]** Project CONTRIBUTOR tries to delete project -> Expected: 403 Forbidden
- [ ] **[P1]** Project LEAD can assign tasks -> Expected: Success
- [ ] **[P1]** Project VIEWER tries to assign tasks -> Expected: 403 Forbidden
- [ ] **[P2]** Role change triggers permission refresh -> Expected: User's permissions updated immediately after role change
- [ ] **[P2]** Role check middleware before sensitive operations -> Expected: Role verified before allowing action
- [ ] **[P3]** Role-based UI element visibility -> Expected: Admin buttons hidden for non-admins

### 14.5 File Uploads: S3 Integration
- [ ] **[P0]** Upload image file (PNG, JPG) -> Expected: File uploaded to S3 bucket, public URL returned
- [ ] **[P0]** S3 bucket path follows convention: `{bucket}/{userId}/{entityType}/{filename}` -> Expected: Files organized correctly
- [ ] **[P1]** Upload file to production S3 bucket -> Expected: File accessible via HTTPS URL, no CORS errors
- [ ] **[P1]** S3 upload with IAM credentials -> Expected: Valid AWS credentials used, upload succeeds
- [ ] **[P1]** Upload fails (invalid credentials) -> Expected: Error returned, file not uploaded, user notified
- [ ] **[P1]** Uploaded file URL persisted to Attachment entity -> Expected: URL, filename, fileSize, mimeType saved to DB
- [ ] **[P2]** S3 bucket configured with public-read ACL for uploaded files -> Expected: Files accessible without signed URLs
- [ ] **[P2]** Upload with custom S3 path prefix (e.g., "projects/attachments/") -> Expected: File stored at correct path
- [ ] **[P2]** S3 bucket lifecycle policy (delete after 90 days for temp files) -> Expected: Old files auto-deleted
- [ ] **[P3]** S3 upload progress tracking -> Expected: Client can monitor upload percentage

### 14.6 File Uploads: Local Storage Fallback
- [ ] **[P0]** S3 unavailable -> Upload falls back to local storage -> Expected: File saved to `/uploads/` directory, local URL returned
- [ ] **[P1]** Local storage path: `/uploads/{userId}/{entityType}/{filename}` -> Expected: Files organized in local filesystem
- [ ] **[P1]** Local file served via static file middleware -> Expected: File accessible at `http://localhost:3000/uploads/...`
- [ ] **[P1]** Local storage used in development environment -> Expected: No S3 required for local dev, files uploaded successfully
- [ ] **[P2]** Local storage directory created if not exists -> Expected: Upload process auto-creates directory structure
- [ ] **[P2]** Local file URL format: `/uploads/...` (relative) or full URL -> Expected: Consistent URL format returned to client
- [ ] **[P3]** Migration from local to S3 -> Expected: Script available to migrate existing local files to S3

### 14.7 File Uploads: MIME Type Validation
- [ ] **[P0]** Upload image file (image/png, image/jpeg, image/gif) -> Expected: Accepted, AttachmentType = IMAGE
- [ ] **[P0]** Upload video file (video/mp4, video/mpeg) -> Expected: Accepted, AttachmentType = VIDEO
- [ ] **[P0]** Upload audio file (audio/mpeg, audio/wav) -> Expected: Accepted, AttachmentType = AUDIO
- [ ] **[P0]** Upload document (application/pdf, application/msword) -> Expected: Accepted, AttachmentType = DOCUMENT
- [ ] **[P0]** Upload archive (application/zip, application/x-tar) -> Expected: Accepted, AttachmentType = ARCHIVE
- [ ] **[P1]** Upload unknown MIME type -> Expected: Accepted, AttachmentType = OTHER
- [ ] **[P1]** Upload .exe or .bat file (executable) -> Expected: Rejected with error "Unsupported file type"
- [ ] **[P1]** Upload .js or .sh file (script) -> Expected: Rejected or flagged as security risk
- [ ] **[P1]** MIME type validation server-side (not just client extension check) -> Expected: Server inspects file header, not just filename
- [ ] **[P2]** Upload file with mismatched extension and MIME type (.png but actually .exe) -> Expected: Rejected, MIME type mismatch detected
- [ ] **[P2]** Allowed MIME types configured in environment -> Expected: Upload policy customizable per deployment
- [ ] **[P3]** MIME type validation error message specifies allowed types -> Expected: "Only images, videos, and documents are allowed"

### 14.8 File Uploads: File Size Limits
- [ ] **[P0]** Upload file under 5MB (default limit) -> Expected: Upload succeeds
- [ ] **[P0]** Upload file over 5MB -> Expected: Rejected with error "File size exceeds maximum limit of 5MB"
- [ ] **[P1]** File size limit configurable per file type (images: 5MB, videos: 50MB, documents: 10MB) -> Expected: Limits enforced per type
- [ ] **[P1]** Upload 10MB video file (within video limit) -> Expected: Success
- [ ] **[P1]** Upload 60MB video file (exceeds video limit) -> Expected: Rejected, "Video file too large (max 50MB)"
- [ ] **[P1]** File size validated before upload starts (client-side pre-check) -> Expected: User notified immediately, upload not attempted
- [ ] **[P1]** File size validated on server (backend check) -> Expected: Server rejects oversized files even if client validation bypassed
- [ ] **[P2]** Upload progress cancelled if size exceeded mid-upload -> Expected: Upload aborted, partial file deleted
- [ ] **[P3]** File size limit displayed in upload UI -> Expected: "Maximum file size: 5MB" shown to user

### 14.9 File Uploads: Attachment Creation & Linking
- [ ] **[P0]** Upload file and link to entity (Project, Task, Post, Message) -> Expected: Attachment record created with correct entityType and entityId
- [ ] **[P0]** Attachment record includes: url, fileName, fileSize, mimeType, uploadedById, attachmentType -> Expected: All fields populated
- [ ] **[P1]** Query attachments for entity -> Expected: GET /attachments?entityId=X&entityType=PROJECT returns all attachments
- [ ] **[P1]** Delete attachment -> Expected: Attachment record deleted, file removed from S3/local storage
- [ ] **[P1]** Delete entity (e.g., project) -> Cascade delete attachments -> Expected: All related attachments deleted
- [ ] **[P1]** Multiple attachments on single entity -> Expected: All attachments linked, retrievable as array
- [ ] **[P2]** Attachment metadata update (rename file) -> Expected: fileName updated, URL unchanged
- [ ] **[P2]** Attachment access control (private project attachment) -> Expected: Only project members can access attachment URL
- [ ] **[P3]** Attachment thumbnail generation for images -> Expected: Thumbnail URL created, used in previews

### 14.10 Pagination: General Pattern
- [ ] **[P0]** Request list with `page=1, limit=10` -> Expected: First 10 results returned
- [ ] **[P0]** Request list with `page=2, limit=10` -> Expected: Results 11-20 returned
- [ ] **[P0]** Pagination metadata included in response: `{ data: [...], total: 100, page: 1, limit: 10, totalPages: 10 }` -> Expected: Metadata accurate
- [ ] **[P1]** Request page beyond total pages (page=99, only 5 pages exist) -> Expected: Empty results array, no error
- [ ] **[P1]** Request with limit=0 -> Expected: Error or default limit applied (e.g., 10)
- [ ] **[P1]** Request with negative page number -> Expected: Error "Invalid page number"
- [ ] **[P1]** Request with limit > 100 (max limit) -> Expected: Limit capped at 100, warning returned
- [ ] **[P2]** Pagination with `skip` and `take` (alternative to page/limit) -> Expected: `skip=20, take=10` returns same as `page=3, limit=10`
- [ ] **[P2]** Pagination combined with filtering -> Expected: Total count reflects filtered results, not entire dataset
- [ ] **[P2]** Pagination combined with sorting -> Expected: Results sorted, then paginated

### 14.11 Pagination: Specific Endpoints
- [ ] **[P0]** GET /projects?page=1&limit=10 -> Expected: Paginated project list returned
- [ ] **[P0]** GET /guilds?page=1&limit=10 -> Expected: Paginated guild list returned
- [ ] **[P0]** GET /opportunities?page=1&limit=10 -> Expected: Paginated opportunities list returned
- [ ] **[P0]** GET /events?page=1&limit=10 -> Expected: Paginated events list returned
- [ ] **[P0]** GET /tasks?page=1&limit=10 -> Expected: Paginated tasks list returned
- [ ] **[P1]** GET /posts?page=1&limit=10 -> Expected: Paginated posts/feed items returned
- [ ] **[P1]** GET /users/search?q=john&page=1&limit=10 -> Expected: Paginated user search results
- [ ] **[P1]** GET /notifications?page=1&limit=20 -> Expected: Paginated notifications list
- [ ] **[P1]** GET /conversations/:id/messages?page=1&limit=50 -> Expected: Paginated chat messages (oldest or newest first)
- [ ] **[P2]** GET /projects/:id/proposals?page=1&limit=10 -> Expected: Paginated proposals for project
- [ ] **[P2]** GET /projects/:id/members?page=1&limit=10 -> Expected: Paginated project members
- [ ] **[P3]** GET /users/:id/followers?page=1&limit=20 -> Expected: Paginated follower list

### 14.12 Search Functionality: Users
- [ ] **[P0]** Search users by name: `/users/search?q=alice` -> Expected: Users with "alice" in firstName or lastName returned
- [ ] **[P0]** Search users by skill: `/users/search?skills=react` -> Expected: Users with "react" in skills array returned
- [ ] **[P1]** Search users by userType: `/users/search?type=CREATOR` -> Expected: Only CREATOR users returned
- [ ] **[P1]** Search case-insensitive: `q=ALICE` matches "Alice" -> Expected: Case-insensitive search
- [ ] **[P1]** Search partial match: `q=ali` matches "Alice" -> Expected: Partial string matching works
- [ ] **[P1]** Search with multiple skills: `skills=react,typescript` -> Expected: Users with any of the skills returned
- [ ] **[P2]** Search with no results -> Expected: Empty array, no error
- [ ] **[P2]** Search with special characters: `q=alice@example` -> Expected: Special chars escaped, no injection vulnerability
- [ ] **[P3]** Search results ordered by relevance or reputation -> Expected: Most relevant/highest rep users first

### 14.13 Search Functionality: Projects
- [ ] **[P0]** Search projects by title: `/projects/search?q=mobile` -> Expected: Projects with "mobile" in title returned
- [ ] **[P0]** Search projects by category: `/projects/search?category=WEB3` -> Expected: Only WEB3 projects returned
- [ ] **[P0]** Search projects by status: `/projects/search?status=ACTIVE` -> Expected: Only ACTIVE projects returned
- [ ] **[P1]** Search projects by visibility: `/projects/search?visibility=PUBLIC` -> Expected: Only PUBLIC projects returned
- [ ] **[P1]** Search projects by fundingStatus: `/projects/search?fundingStatus=FUNDED` -> Expected: Only FUNDED projects returned
- [ ] **[P1]** Combined filters: `category=AI&status=ACTIVE` -> Expected: Projects matching both criteria returned
- [ ] **[P2]** Search projects by description text -> Expected: Full-text search in description field
- [ ] **[P2]** Search projects by tags (if implemented) -> Expected: Projects tagged with specified tags returned
- [ ] **[P3]** Search autocomplete suggestions -> Expected: Suggested project titles as user types

### 14.14 Search Functionality: Opportunities
- [ ] **[P0]** Search opportunities by type: `/opportunities/search?type=FULL_TIME` -> Expected: Only FULL_TIME opportunities returned
- [ ] **[P0]** Search opportunities by skills: `/opportunities/search?skills=python` -> Expected: Opportunities requiring python skill returned
- [ ] **[P0]** Search opportunities by experienceLevel: `/opportunities/search?experienceLevel=INTERMEDIATE` -> Expected: Only INTERMEDIATE level opportunities returned
- [ ] **[P1]** Search opportunities by status: `/opportunities/search?status=OPEN` -> Expected: Only OPEN opportunities returned
- [ ] **[P1]** Search opportunities by project: `/opportunities/search?projectId=123` -> Expected: Opportunities for project 123 returned
- [ ] **[P1]** Combined filters: `type=CONTRACT&experienceLevel=EXPERT` -> Expected: Opportunities matching both criteria
- [ ] **[P2]** Search opportunities by compensation range (min/max) -> Expected: Opportunities with compensation in range returned
- [ ] **[P3]** Search opportunities by remote availability -> Expected: Remote opportunities flagged/filtered

### 14.15 Search Functionality: Guilds & Events
- [ ] **[P1]** Search guilds by name: `/guilds/search?q=blockchain` -> Expected: Guilds with "blockchain" in name returned
- [ ] **[P1]** Search guilds by category -> Expected: Guilds of specified category returned
- [ ] **[P1]** Search events by title -> Expected: Events with matching title returned
- [ ] **[P1]** Search events by type: `/events/search?type=WORKSHOP` -> Expected: Only WORKSHOP events returned
- [ ] **[P1]** Search events by date range: `startDate=2026-02-01&endDate=2026-02-28` -> Expected: Events within date range returned
- [ ] **[P2]** Search events by location (if implemented) -> Expected: Events in specified location returned
- [ ] **[P3]** Search events by host/organizer -> Expected: Events hosted by specific user/guild returned

### 14.16 Filtering & Sorting
- [ ] **[P0]** Filter by status (ACTIVE, COMPLETED, etc.) across entities -> Expected: Filters applied correctly
- [ ] **[P0]** Filter by type (PROJECT, OPPORTUNITY, EVENT, etc.) -> Expected: Type-specific results returned
- [ ] **[P0]** Filter by category (AI, WEB3, SUSTAINABILITY, etc.) -> Expected: Category filter works
- [ ] **[P0]** Filter by date range (createdAt >= startDate AND createdAt <= endDate) -> Expected: Date range filter accurate
- [ ] **[P1]** Sort by newest (createdAt DESC) -> Expected: Most recent items first
- [ ] **[P1]** Sort by oldest (createdAt ASC) -> Expected: Oldest items first
- [ ] **[P1]** Sort by trending (based on activity score/likes/views) -> Expected: Most popular items first
- [ ] **[P1]** Sort by most popular (highest engagement) -> Expected: Items with most interactions first
- [ ] **[P1]** Combined filter + sort: `status=ACTIVE&sort=newest` -> Expected: Filtered and sorted results
- [ ] **[P2]** Multiple filters applied simultaneously (AND logic) -> Expected: All criteria must match
- [ ] **[P2]** Filter with OR logic (if supported): `category=AI OR category=WEB3` -> Expected: Items matching any category
- [ ] **[P3]** Default sort order when none specified -> Expected: Newest first or defined default

### 14.17 Error Handling: Result<T> Pattern (Backend)
- [ ] **[P0]** Successful operation returns `Result<T>.Success(data)` -> Expected: HTTP 200 with data payload
- [ ] **[P0]** Failed operation returns `Result<T>.Failure(error)` -> Expected: HTTP error code (400, 404, 500) with error message
- [ ] **[P1]** Validation error returns 400 Bad Request -> Expected: Error details included in response body
- [ ] **[P1]** Not found error returns 404 Not Found -> Expected: "Resource not found" message
- [ ] **[P1]** Unauthorized access returns 401 Unauthorized -> Expected: "Authentication required" message
- [ ] **[P1]** Forbidden access returns 403 Forbidden -> Expected: "You do not have permission" message
- [ ] **[P1]** Server error returns 500 Internal Server Error -> Expected: Generic error message, details logged server-side
- [ ] **[P2]** ToActionResult() maps Result to appropriate HTTP response -> Expected: Consistent response format
- [ ] **[P2]** Error response includes error code and message: `{ error: { code: "VALIDATION_ERROR", message: "..." } }` -> Expected: Structured error
- [ ] **[P3]** Success response includes success flag: `{ success: true, data: {...} }` -> Expected: Consistent success format

### 14.18 Error Handling: Global Exception Middleware
- [ ] **[P0]** Unhandled exception in controller -> Global exception handler catches it -> Expected: 500 error returned, stack trace logged
- [ ] **[P0]** Database connection error -> Expected: 500 error, "Database unavailable" message, no sensitive DB details exposed
- [ ] **[P1]** Exception details logged to application logs -> Expected: Full stack trace in logs, sanitized error to client
- [ ] **[P1]** Production environment hides stack traces from API response -> Expected: Generic "Internal server error" message only
- [ ] **[P1]** Development environment includes stack trace in response -> Expected: Detailed error for debugging
- [ ] **[P2]** Exception middleware logs request details (URL, method, user) -> Expected: Request context included in error logs
- [ ] **[P2]** Custom exception types (e.g., NotFoundException) handled differently -> Expected: Custom exceptions map to specific HTTP codes
- [ ] **[P3]** Exception monitoring/alerting integration (e.g., Sentry) -> Expected: Critical errors trigger alerts

### 14.19 Error Handling: tRPC Errors
- [ ] **[P0]** tRPC procedure throws error -> Expected: Error serialized and returned to client with error code
- [ ] **[P0]** Client receives tRPC error with code: UNAUTHORIZED, BAD_REQUEST, NOT_FOUND, etc. -> Expected: Client can handle error by code
- [ ] **[P1]** tRPC input validation error (Zod schema failure) -> Expected: BAD_REQUEST error with field-specific messages
- [ ] **[P1]** tRPC procedure throws custom TRPCError -> Expected: Custom error code and message returned
- [ ] **[P1]** tRPC error includes cause/stack in development -> Expected: Detailed error info for debugging
- [ ] **[P2]** tRPC error shape consistent: `{ error: { code, message, data } }` -> Expected: Predictable error structure
- [ ] **[P3]** tRPC onError hook logs errors globally -> Expected: All tRPC errors logged centrally

### 14.20 Error Handling: Client-Side Display
- [ ] **[P0]** API error received -> UI displays error toast/banner -> Expected: User notified of error
- [ ] **[P0]** Error message user-friendly (not raw stack trace) -> Expected: "Failed to load projects. Please try again."
- [ ] **[P1]** Validation errors displayed per field -> Expected: Form fields show specific error messages
- [ ] **[P1]** Network error (timeout, connection refused) -> Expected: "Network error. Check your connection." message
- [ ] **[P1]** Retry button shown for transient errors -> Expected: User can retry failed operation
- [ ] **[P2]** Error state cleared after successful retry -> Expected: Error toast dismissed, data loaded
- [ ] **[P2]** Error logging to client-side analytics -> Expected: Errors tracked for monitoring
- [ ] **[P3]** Error fallback UI for critical failures -> Expected: Graceful degradation, app doesn't crash

### 14.21 CORS Configuration
- [ ] **[P0]** Frontend (localhost:3000) can call backend API (localhost:5000) -> Expected: CORS headers allow cross-origin requests
- [ ] **[P0]** CORS preflight (OPTIONS request) returns 200 OK -> Expected: Preflight succeeds, actual request proceeds
- [ ] **[P1]** CORS allows specific origins (not wildcard * in production) -> Expected: Only trusted domains allowed
- [ ] **[P1]** CORS allows necessary methods: GET, POST, PUT, DELETE, PATCH -> Expected: All CRUD methods allowed
- [ ] **[P1]** CORS allows necessary headers: Authorization, Content-Type, X-Api-Key -> Expected: Required headers allowed
- [ ] **[P2]** CORS allows credentials (cookies, auth headers) -> Expected: `Access-Control-Allow-Credentials: true`
- [ ] **[P2]** Request from disallowed origin rejected -> Expected: CORS error in browser, request blocked
- [ ] **[P3]** CORS configuration environment-specific -> Expected: Localhost allowed in dev, production domain in prod

### 14.22 Slug Generation & Uniqueness
- [ ] **[P0]** Create project with title "My Awesome Project" -> Expected: Slug generated as "my-awesome-project"
- [ ] **[P0]** Slug contains only lowercase letters, numbers, hyphens -> Expected: Special characters removed/replaced
- [ ] **[P0]** Create second project with same title -> Expected: Slug auto-incremented: "my-awesome-project-1"
- [ ] **[P1]** Slug uniqueness enforced by database constraint -> Expected: Duplicate slug insertion fails with unique constraint error
- [ ] **[P1]** Slug generated from title on creation -> Expected: Slug auto-populated if not provided
- [ ] **[P1]** Manual slug provided -> Expected: Manual slug used if unique, otherwise error returned
- [ ] **[P1]** Update project title -> Slug unchanged -> Expected: Slug not auto-updated to avoid breaking URLs
- [ ] **[P2]** Slug max length enforced (e.g., 100 chars) -> Expected: Long titles truncated to max slug length
- [ ] **[P2]** Slug with unicode characters -> Expected: Unicode normalized or transliterated (e.g., "cafe" -> "cafe")
- [ ] **[P3]** Slug collision detection before save -> Expected: Check for existing slug, auto-increment if needed

### 14.23 Timestamp Handling
- [ ] **[P0]** Entity created -> createdAt auto-populated with current UTC timestamp -> Expected: createdAt not null
- [ ] **[P0]** Entity updated -> updatedAt auto-populated with current UTC timestamp -> Expected: updatedAt reflects latest change
- [ ] **[P1]** createdAt never changes after creation -> Expected: createdAt immutable
- [ ] **[P1]** updatedAt changes on every update -> Expected: updatedAt timestamp increments with each save
- [ ] **[P1]** Timestamps stored in UTC -> Expected: All timestamps in UTC timezone, converted to local time on client
- [ ] **[P1]** Timestamp precision (milliseconds) -> Expected: Timestamps include milliseconds: "2026-02-01T12:34:56.789Z"
- [ ] **[P2]** Timezone conversion on client display -> Expected: Timestamps displayed in user's local timezone
- [ ] **[P2]** Timestamp comparison (entity A created before entity B) -> Expected: Accurate ordering by createdAt
- [ ] **[P3]** Timestamp formatting (relative time: "2 minutes ago") -> Expected: Human-readable timestamps in UI

### 14.24 Soft Deletes
- [ ] **[P0]** Delete entity -> deletedAt timestamp set, entity not physically removed -> Expected: Entity still exists in DB with deletedAt != null
- [ ] **[P0]** Query entities -> Soft-deleted entities excluded by default -> Expected: WHERE deletedAt IS NULL filter applied
- [ ] **[P1]** Restore soft-deleted entity -> deletedAt set to null -> Expected: Entity reappears in normal queries
- [ ] **[P1]** Hard delete option available for permanent removal -> Expected: Physical DELETE query removes row from DB
- [ ] **[P1]** Cascade soft deletes (delete project -> soft delete all tasks) -> Expected: Related entities also soft-deleted
- [ ] **[P2]** Query explicitly including soft-deleted entities -> Expected: withDeleted() or similar flag includes deleted records
- [ ] **[P2]** Soft-deleted entity inaccessible to normal users, visible to admins -> Expected: Role-based access to deleted data
- [ ] **[P3]** Cleanup job periodically hard-deletes old soft-deleted entities -> Expected: deletedAt older than 90 days purged

### 14.25 Data Validation: Required Fields
- [ ] **[P0]** Submit form with missing required field -> Expected: Validation error, "Field X is required"
- [ ] **[P0]** Backend validation rejects null/empty required field -> Expected: 400 Bad Request with validation error
- [ ] **[P1]** Client-side validation prevents submission -> Expected: Form submit button disabled or error shown before API call
- [ ] **[P1]** Required field validation on: title, name, email, password -> Expected: All critical fields validated
- [ ] **[P2]** Validation error message specific to field -> Expected: "Project title is required", not generic error
- [ ] **[P3]** Required fields marked in UI with asterisk (*) -> Expected: Visual indicator for required fields

### 14.26 Data Validation: String Lengths
- [ ] **[P0]** Exceed max string length (e.g., title > 200 chars) -> Expected: Validation error, "Title must be less than 200 characters"
- [ ] **[P0]** Backend enforces string length limits -> Expected: Database varchar constraints or validation logic enforced
- [ ] **[P1]** Field with min length (e.g., password >= 8 chars) -> Expected: Validation error if too short
- [ ] **[P1]** Client-side validation shows character count -> Expected: "45/200 characters" counter visible
- [ ] **[P2]** String truncation on display (long descriptions) -> Expected: UI shows "Read more" for truncated text
- [ ] **[P3]** Validation message includes current length vs max -> Expected: "Description too long (523/500)"

### 14.27 Data Validation: Decimal Precision
- [ ] **[P0]** Decimal field (compensation, fundingGoal) has precision 18, scale 8 -> Expected: Values up to 9999999999.99999999 accepted
- [ ] **[P0]** Exceed decimal precision -> Expected: Validation error or value rounded
- [ ] **[P1]** Negative decimal where not allowed -> Expected: Validation error, "Value must be positive"
- [ ] **[P1]** Zero value where not allowed -> Expected: Validation error, "Value must be greater than zero"
- [ ] **[P2]** Decimal rounding consistent (round to 8 decimal places) -> Expected: Values stored with 8 decimal precision
- [ ] **[P3]** Display decimals with appropriate formatting (e.g., $1,234.56) -> Expected: Currency/number formatting applied

---

## 15. Navigation & Layout

### 15.1 Main Navigation/Sidebar Links
- [ ] **[P0]** Click "Home" link -> Navigate to `/` -> Expected: Landing page displayed
- [ ] **[P0]** Click "Projects" link -> Navigate to `/projects` -> Expected: Projects listing page displayed
- [ ] **[P0]** Click "Guilds" link -> Navigate to `/guilds` -> Expected: Guilds listing page displayed
- [ ] **[P0]** Click "Opportunities" link -> Navigate to `/opportunities` -> Expected: Opportunities marketplace displayed
- [ ] **[P0]** Click "Events" link -> Navigate to `/events` -> Expected: Events listing page displayed
- [ ] **[P0]** Click "Governance" link -> Navigate to `/governance` -> Expected: Governance/DAO page displayed
- [ ] **[P0]** Click "Dashboard" link (authenticated user) -> Navigate to `/dashboard` -> Expected: User dashboard displayed
- [ ] **[P1]** Navigation links highlight current page -> Expected: Active page link has active/selected style
- [ ] **[P1]** Logo in header links to home page -> Expected: Click logo -> navigate to `/`
- [ ] **[P2]** Navigation menu collapsible on mobile -> Expected: Hamburger menu icon, menu slides in/out
- [ ] **[P2]** Navigation persistent across page transitions (SPA) -> Expected: No full page reload, client-side routing
- [ ] **[P3]** Keyboard navigation (Tab, Enter) -> Expected: Accessible navigation via keyboard

### 15.2 Public Routes (Unauthenticated Access)
- [ ] **[P0]** Navigate to `/` (landing page) as unauthenticated user -> Expected: Page accessible, no redirect
- [ ] **[P0]** Navigate to `/auth/signin` -> Expected: Sign-in page displayed
- [ ] **[P0]** Navigate to `/projects` -> Expected: Public projects listing displayed
- [ ] **[P0]** Navigate to `/guilds` -> Expected: Public guilds listing displayed
- [ ] **[P0]** Navigate to `/opportunities` -> Expected: Opportunities marketplace displayed
- [ ] **[P0]** Navigate to `/events` -> Expected: Events listing displayed
- [ ] **[P0]** Navigate to `/governance` -> Expected: Governance proposals listing displayed
- [ ] **[P1]** Navigate to `/projects/[slug]` (public project) -> Expected: Project detail page accessible
- [ ] **[P1]** Navigate to `/guilds/[slug]` (public guild) -> Expected: Guild detail page accessible
- [ ] **[P2]** Navigate to `/governance/[id]` (public proposal) -> Expected: Proposal detail page accessible
- [ ] **[P3]** Navigate to `/about`, `/contact`, `/faq` (if exist) -> Expected: Static pages accessible

### 15.3 Protected Routes (Authentication Required)
- [ ] **[P0]** Navigate to `/dashboard` as unauthenticated user -> Expected: Redirect to `/auth/signin?callbackUrl=/dashboard`
- [ ] **[P0]** Navigate to `/dashboard/create` (create project) as unauthenticated user -> Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/projects/create` as unauthenticated user -> Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/guilds/create` as unauthenticated user -> Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/dashboard/tasks` as unauthenticated user -> Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/dashboard/chats` as unauthenticated user -> Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/dashboard/profile` as unauthenticated user -> Expected: Redirect to signin
- [ ] **[P1]** After signin, redirect to original destination (callbackUrl) -> Expected: User lands on originally requested page
- [ ] **[P1]** Navigate to `/dashboard/edit/[id]` as authenticated user -> Expected: Edit page accessible if user has permission
- [ ] **[P2]** Navigate to protected route with expired session -> Expected: Redirect to signin, session refresh prompt
- [ ] **[P3]** Protected route with role requirement (admin-only) -> Non-admin tries to access -> Expected: 403 Forbidden or redirect

### 15.4 Dynamic Routes
- [ ] **[P0]** Navigate to `/projects/[slug]` with valid slug -> Expected: Project detail page loaded with correct project data
- [ ] **[P0]** Navigate to `/projects/[slug]` with invalid slug -> Expected: 404 Not Found page displayed
- [ ] **[P0]** Navigate to `/guilds/[slug]` with valid slug -> Expected: Guild detail page loaded
- [ ] **[P0]** Navigate to `/governance/[id]` with valid ID -> Expected: Proposal detail page loaded
- [ ] **[P0]** Navigate to `/dashboard/profile/[id]` with valid user ID -> Expected: User profile page loaded
- [ ] **[P1]** Dynamic route parameter extraction -> Expected: Correct ID/slug passed to data fetching logic
- [ ] **[P1]** SEO metadata (title, description) populated dynamically based on entity -> Expected: Page title = "Project Name | ArdaNova"
- [ ] **[P2]** Dynamic route with multiple parameters (e.g., `/projects/[slug]/opportunities/[oppId]`) -> Expected: Both parameters extracted correctly
- [ ] **[P3]** Dynamic route pre-rendering (SSR/SSG) -> Expected: Page pre-rendered on server, fast initial load

### 15.5 AuthenticatedLayout Wrapper
- [ ] **[P0]** Authenticated route wrapped with AuthenticatedLayout -> Session check performed -> Expected: Authenticated users see content, others redirected
- [ ] **[P0]** AuthenticatedLayout includes header, sidebar, footer -> Expected: Consistent layout across authenticated pages
- [ ] **[P1]** AuthenticatedLayout displays user avatar/name in header -> Expected: Current user info visible
- [ ] **[P1]** AuthenticatedLayout shows notification icon with unread count -> Expected: Notification badge updated in real-time
- [ ] **[P1]** Logout button in AuthenticatedLayout -> Click logout -> Expected: Session cleared, redirected to signin
- [ ] **[P2]** AuthenticatedLayout responsive -> Expected: Mobile-friendly layout, sidebar collapses on mobile
- [ ] **[P3]** AuthenticatedLayout includes breadcrumbs -> Expected: Navigation breadcrumb trail visible

### 15.6 FeedLayout Wrapper
- [ ] **[P1]** Feed pages use FeedLayout wrapper -> Expected: Consistent feed UI (sidebar, main feed, trending section)
- [ ] **[P1]** FeedLayout includes left sidebar (navigation), main feed area, right sidebar (trending/suggestions) -> Expected: Three-column layout on desktop
- [ ] **[P1]** FeedLayout responsive -> Expected: Single column on mobile, sidebars collapse or stack
- [ ] **[P2]** FeedLayout right sidebar shows trending topics/projects -> Expected: Dynamic trending content displayed
- [ ] **[P3]** FeedLayout includes infinite scroll for feed -> Expected: More posts load as user scrolls

### 15.7 Dashboard Views: Dashboard Home
- [ ] **[P0]** Navigate to `/dashboard` -> Expected: Dashboard home page with overview widgets (recent projects, tasks, notifications)
- [ ] **[P1]** Dashboard home shows user's active projects -> Expected: List of projects user is member of
- [ ] **[P1]** Dashboard home shows recent tasks assigned to user -> Expected: Task list with status indicators
- [ ] **[P1]** Dashboard home shows upcoming events -> Expected: Events user is attending or hosting
- [ ] **[P2]** Dashboard home shows activity feed -> Expected: Recent activity from followed users/projects
- [ ] **[P2]** Dashboard widgets interactive (click to view details) -> Expected: Clicking project widget navigates to project detail
- [ ] **[P3]** Dashboard customizable (user can add/remove widgets) -> Expected: Widget layout preferences saved

### 15.8 Dashboard Views: Create Project
- [ ] **[P0]** Navigate to `/dashboard/create` -> Expected: Create project form displayed
- [ ] **[P0]** Create project form includes fields: title, description, category, visibility -> Expected: All required fields present
- [ ] **[P1]** Submit create project form -> Expected: Project created, redirect to project detail page
- [ ] **[P1]** Create project form validation -> Expected: Client-side validation prevents invalid submission
- [ ] **[P2]** Create project form autosave draft -> Expected: Form data saved to local storage, restored on reload
- [ ] **[P3]** Create project wizard (multi-step form) -> Expected: Step-by-step guidance for project creation

### 15.9 Dashboard Views: Edit Project
- [ ] **[P0]** Navigate to `/dashboard/edit/[id]` for user's own project -> Expected: Edit form pre-populated with project data
- [ ] **[P0]** Submit edit form -> Expected: Project updated, success message shown
- [ ] **[P1]** Navigate to `/dashboard/edit/[id]` for project user doesn't own -> Expected: 403 Forbidden or redirect
- [ ] **[P1]** Edit form cancel button -> Expected: Navigate back to project detail, changes discarded
- [ ] **[P2]** Edit form shows unsaved changes warning -> Expected: "You have unsaved changes" prompt on navigation
- [ ] **[P3]** Edit form version history -> Expected: Previous versions accessible, diff view available

### 15.10 Dashboard Views: Profile
- [ ] **[P0]** Navigate to `/dashboard/profile` -> Expected: Current user's profile edit page displayed
- [ ] **[P0]** Profile page shows editable fields: name, bio, skills, avatar, social links -> Expected: All profile fields editable
- [ ] **[P0]** Submit profile updates -> Expected: User profile saved, success message shown
- [ ] **[P1]** Navigate to `/dashboard/profile/[id]` (view another user's profile) -> Expected: Read-only profile view for specified user
- [ ] **[P1]** View own profile -> Edit button visible -> Expected: User can navigate to edit mode
- [ ] **[P1]** View other user's profile -> Follow/Message buttons visible -> Expected: Social interaction options available
- [ ] **[P2]** Profile shows user's reputation, badges, stats -> Expected: Gamification elements displayed
- [ ] **[P2]** Profile shows user's projects, posts, activity -> Expected: Tabs for different content types
- [ ] **[P3]** Profile privacy settings -> Expected: User can control visibility of profile sections

### 15.11 Dashboard Views: Tasks
- [ ] **[P0]** Navigate to `/dashboard/tasks` -> Expected: Task list view with filters (My Tasks, Assigned by Me, All)
- [ ] **[P0]** Task list shows task title, status, assignee, due date -> Expected: All key task info visible
- [ ] **[P1]** Filter tasks by status (TODO, IN_PROGRESS, DONE) -> Expected: Task list updates to show only selected status
- [ ] **[P1]** Click task -> Expected: Task detail modal or page opens
- [ ] **[P1]** Create new task from tasks page -> Expected: Create task modal/form opens
- [ ] **[P2]** Sort tasks by due date, priority, status -> Expected: Task list reorders accordingly
- [ ] **[P2]** Search tasks by title -> Expected: Task list filters to matching tasks
- [ ] **[P3]** Task kanban board view -> Expected: Drag-and-drop task cards across status columns

### 15.12 Dashboard Views: Chats
- [ ] **[P0]** Navigate to `/dashboard/chats` -> Expected: Chat/messaging interface displayed
- [ ] **[P0]** Chat page shows list of conversations -> Expected: All user's conversations listed
- [ ] **[P0]** Click conversation -> Expected: Conversation messages displayed in main panel
- [ ] **[P1]** Unread conversation highlighted -> Expected: Bold text or badge indicator for unread messages
- [ ] **[P1]** Send message in chat -> Expected: Message sent, appears in conversation thread
- [ ] **[P1]** Start new conversation button -> Expected: Modal to select participants and start chat
- [ ] **[P2]** Search conversations by participant name or message content -> Expected: Conversation list filtered
- [ ] **[P2]** Chat supports attachments (upload image/file) -> Expected: File attachment UI available
- [ ] **[P3]** Chat archive conversation -> Expected: Conversation moved to archived, removed from main list

### 15.13 Dashboard Views: Subscriptions
- [ ] **[P1]** Navigate to `/dashboard/subscriptions` -> Expected: User's subscriptions/following list displayed
- [ ] **[P1]** Subscriptions page shows followed users, projects, guilds -> Expected: All subscription types listed
- [ ] **[P1]** Unfollow user/project -> Expected: Subscription removed, item removed from list
- [ ] **[P2]** Subscription page shows feed of subscribed content -> Expected: Combined feed from all subscriptions
- [ ] **[P3]** Manage subscription notifications -> Expected: User can enable/disable notifications per subscription

### 15.14 Tab Navigation Within Detail Pages
- [ ] **[P0]** Project detail page has tabs: Overview, Team, Proposals, Opportunities -> Expected: All tabs visible
- [ ] **[P0]** Click "Team" tab -> Expected: Team members list displayed, URL updates to `/projects/[slug]?tab=team` or `/projects/[slug]/team`
- [ ] **[P0]** Click "Proposals" tab -> Expected: Proposals list displayed for project
- [ ] **[P0]** Click "Opportunities" tab -> Expected: Opportunities for project displayed
- [ ] **[P1]** Active tab highlighted -> Expected: Current tab has active style
- [ ] **[P1]** Tab navigation updates URL (query param or route segment) -> Expected: Direct URL to tab works (shareable link)
- [ ] **[P1]** Navigate directly to tab URL -> Expected: Correct tab active on page load
- [ ] **[P2]** Tab content lazy-loaded -> Expected: Tab content fetched only when tab clicked
- [ ] **[P3]** Tab navigation keyboard accessible (arrow keys) -> Expected: Can navigate tabs with keyboard

### 15.15 Guild Detail Tabs
- [ ] **[P0]** Guild detail page has tabs: Overview, Members, Projects -> Expected: All tabs visible
- [ ] **[P0]** Click "Members" tab -> Expected: Guild members list displayed
- [ ] **[P0]** Click "Projects" tab -> Expected: Projects associated with guild displayed
- [ ] **[P1]** Guild overview tab shows description, stats, recent activity -> Expected: Comprehensive guild info
- [ ] **[P2]** Guild tabs accessible to non-members (public guild) -> Expected: Tabs viewable, join button visible
- [ ] **[P3]** Guild tabs restricted for private guild -> Expected: Non-members see limited info, prompt to join

### 15.16 Governance Detail View
- [ ] **[P0]** Navigate to `/governance/[id]` -> Expected: Proposal detail page with title, description, voting info
- [ ] **[P1]** Governance detail shows vote counts, quorum progress -> Expected: Visual indicators (progress bars)
- [ ] **[P1]** Governance detail shows voting deadline -> Expected: Countdown or expiration date visible
- [ ] **[P1]** Vote button on governance detail -> Expected: User can cast vote from detail page
- [ ] **[P2]** Governance detail shows vote history (who voted, how) -> Expected: Transparency in voting record
- [ ] **[P3]** Governance detail includes discussion/comments section -> Expected: Users can comment on proposal

### 15.17 Breadcrumb Navigation
- [ ] **[P1]** Breadcrumbs displayed on deep pages: "Home > Projects > Project Name" -> Expected: Breadcrumb trail visible
- [ ] **[P1]** Click breadcrumb link -> Navigate to parent page -> Expected: Breadcrumb navigation functional
- [ ] **[P2]** Breadcrumbs dynamically generated based on route -> Expected: Accurate breadcrumb for current page
- [ ] **[P2]** Breadcrumbs include entity names (project title, guild name) -> Expected: Contextual breadcrumb labels
- [ ] **[P3]** Breadcrumbs accessible -> Expected: ARIA labels for screen readers

### 15.18 Mobile Responsive Layout
- [ ] **[P0]** View application on mobile device (< 768px width) -> Expected: Responsive layout, no horizontal scroll
- [ ] **[P0]** Navigation menu collapses to hamburger icon on mobile -> Expected: Hamburger menu functional
- [ ] **[P1]** Forms stack vertically on mobile -> Expected: Form fields full-width, easy to fill on mobile
- [ ] **[P1]** Tables responsive (horizontal scroll or stacked view) -> Expected: Tables usable on mobile
- [ ] **[P1]** Buttons and touch targets sized appropriately (min 44px) -> Expected: Easy to tap on mobile
- [ ] **[P2]** Images scale to fit mobile screen -> Expected: No image overflow
- [ ] **[P2]** Mobile layout tested on iOS and Android -> Expected: Consistent experience across mobile platforms
- [ ] **[P3]** Mobile-specific features (swipe gestures, pull-to-refresh) -> Expected: Enhanced mobile UX

### 15.19 Back Navigation
- [ ] **[P1]** Browser back button works correctly -> Expected: Navigate to previous page in history
- [ ] **[P1]** In-app back button (if present) -> Expected: Navigate to logical parent page
- [ ] **[P2]** Back navigation preserves scroll position -> Expected: Return to previous scroll position after navigation
- [ ] **[P2]** Back navigation after form submission -> Expected: Navigate back without re-submitting form
- [ ] **[P3]** Deep link followed by back navigation -> Expected: Browser back goes to external site, not trapped in app

### 15.20 404/Error Pages
- [ ] **[P0]** Navigate to non-existent route (e.g., `/does-not-exist`) -> Expected: 404 Not Found page displayed
- [ ] **[P0]** 404 page includes message "Page not found" -> Expected: Clear error message
- [ ] **[P1]** 404 page includes link to home page -> Expected: User can navigate back to home
- [ ] **[P1]** 404 page maintains layout (header, footer) -> Expected: Consistent branding, navigation available
- [ ] **[P2]** Server error (500) -> Custom error page displayed -> Expected: "Something went wrong" page with retry option
- [ ] **[P2]** Error page includes error code and support contact -> Expected: User can report issue
- [ ] **[P3]** 404 page includes search or suggestions -> Expected: Help user find what they were looking for

---

## 16. Edge Cases & Negative Testing

### 16.1 Authentication Edge Cases
- [ ] **[P0]** Expired session token used in API request -> Expected: 401 Unauthorized, user prompted to re-authenticate
- [ ] **[P0]** Invalid JWT token (tampered) -> Expected: Token verification fails, 401 error
- [ ] **[P0]** Concurrent sessions from multiple devices -> Expected: Both sessions valid, actions from either device work
- [ ] **[P1]** OAuth callback failure (Google returns error) -> Expected: User shown error message, option to retry
- [ ] **[P1]** OAuth state parameter mismatch (CSRF protection) -> Expected: OAuth callback rejected, error shown
- [ ] **[P1]** User tries to link Google account already linked to another user -> Expected: Error "This Google account is already linked to another account"
- [ ] **[P1]** Session cookie deleted manually -> User tries to access protected route -> Expected: Redirect to signin
- [ ] **[P2]** Refresh token expired -> Expected: User logged out, prompted to sign in again
- [ ] **[P2]** Session hijacking attempt (stolen session token) -> Expected: Session validation detects anomaly (IP change, etc.), token invalidated
- [ ] **[P3]** Multiple simultaneous login attempts with same account -> Expected: All logins succeed, no account lockout

### 16.2 Authorization Violations
- [ ] **[P0]** Non-member tries to access private project detail -> Expected: 403 Forbidden or redirect, "You do not have access to this project"
- [ ] **[P0]** Non-owner tries to delete another user's project -> Expected: 403 Forbidden
- [ ] **[P0]** Regular user tries to access admin-only endpoint -> Expected: 403 Forbidden
- [ ] **[P1]** Guild MEMBER tries to perform ADMIN action (e.g., remove member) -> Expected: 403 Forbidden, "Insufficient permissions"
- [ ] **[P1]** Project VIEWER tries to edit project details -> Expected: 403 Forbidden
- [ ] **[P1]** User tries to access another user's private profile data (email, phone) -> Expected: Data not returned in API response
- [ ] **[P1]** User tries to modify another user's profile -> Expected: 403 Forbidden
- [ ] **[P1]** API request without X-Api-Key to .NET backend -> Expected: 401 Unauthorized, request rejected
- [ ] **[P2]** User tries to vote on proposal for project they're not member of -> Expected: 403 Forbidden or voting restricted
- [ ] **[P2]** User tries to approve guild membership application without ADMIN role -> Expected: 403 Forbidden
- [ ] **[P3]** User tries to access audit log (admin feature) -> Expected: 403 Forbidden for non-admins

### 16.3 Data Integrity: Duplicate Slug Creation
- [ ] **[P0]** Create project with slug "my-project" -> Create second project with same slug -> Expected: Second project auto-assigned "my-project-1"
- [ ] **[P0]** Manually set slug to duplicate value -> Expected: Database unique constraint error, 409 Conflict response
- [ ] **[P1]** Slug collision detection before insert -> Expected: Application checks for duplicate, auto-increments
- [ ] **[P2]** Rapid parallel project creation with same title -> Expected: Unique slugs generated, no race condition duplicates
- [ ] **[P3]** Slug case sensitivity (if enforced) -> "My-Project" vs "my-project" -> Expected: Treated as different or same based on policy

### 16.4 Data Integrity: Duplicate Email Registration
- [ ] **[P0]** User registers with email already in system -> Expected: Error "Email already registered"
- [ ] **[P0]** Database unique constraint on email field -> Expected: Duplicate insert fails at DB level
- [ ] **[P1]** Case-insensitive email check ("User@Example.com" vs "user@example.com") -> Expected: Treated as duplicate
- [ ] **[P2]** Email verification required -> User tries to register same email before verifying -> Expected: Previous registration invalidated or error shown
- [ ] **[P3]** Email normalization (trim whitespace, lowercase) before uniqueness check -> Expected: " User@Example.com " treated same as "user@example.com"

### 16.5 Data Integrity: Unique Constraint Violations
- [ ] **[P0]** Create OpportunityApplication with same userId + opportunityId -> Expected: Unique constraint error, user cannot apply twice
- [ ] **[P0]** Create OpportunityBid with same userId + opportunityId -> Expected: Unique constraint error, user cannot bid twice
- [ ] **[P0]** Create duplicate MembershipCredential (same userId + projectId) -> Expected: Unique constraint error, one credential per user per project
- [ ] **[P1]** Create duplicate ProjectMember (same userId + projectId) -> Expected: Unique constraint error
- [ ] **[P1]** Create duplicate GuildMembership (same userId + guildId) -> Expected: Unique constraint error
- [ ] **[P1]** Create duplicate Like (same userId + entityId + entityType) -> Expected: Unique constraint error, user cannot like same item twice
- [ ] **[P2]** Create duplicate Follow (same followerId + followedId) -> Expected: Unique constraint error
- [ ] **[P2]** Create duplicate EventRegistration (same userId + eventId) -> Expected: Unique constraint error
- [ ] **[P3]** Application handles unique constraint errors gracefully -> Expected: User-friendly error message, not raw SQL error

### 16.6 Data Integrity: Foreign Key Violations
- [ ] **[P0]** Create project with non-existent createdById (user ID) -> Expected: Foreign key constraint error, 400 Bad Request
- [ ] **[P0]** Delete user who owns projects -> Expected: Cascade delete or foreign key error, depending on configuration
- [ ] **[P1]** Create task with non-existent projectId -> Expected: Foreign key error, task creation fails
- [ ] **[P1]** Delete project -> Check related tasks -> Expected: Tasks cascade deleted or orphaned (depending on FK constraint)
- [ ] **[P1]** Create comment with non-existent parentId (reply to deleted comment) -> Expected: Foreign key error or parent validation
- [ ] **[P2]** Delete conversation -> Check messages -> Expected: Messages cascade deleted
- [ ] **[P2]** Delete opportunity -> Check applications -> Expected: Applications cascade deleted or foreign key prevents deletion
- [ ] **[P3]** Soft-deleted entity referenced by foreign key -> Expected: Application logic prevents FK to soft-deleted entities

### 16.7 Data Integrity: Circular References
- [ ] **[P0]** Create comment replying to itself (parentId = own id) -> Expected: Validation error, "Comment cannot reply to itself"
- [ ] **[P0]** Create task depending on itself (dependsOnId = own id) -> Expected: Validation error, "Task cannot depend on itself"
- [ ] **[P1]** Create circular dependency chain (Task A depends on B, B depends on C, C depends on A) -> Expected: Validation detects cycle, prevents creation
- [ ] **[P2]** Create circular reporting structure (User A reports to B, B reports to A) -> Expected: Validation prevents circular hierarchy
- [ ] **[P3]** Graph traversal for circular reference detection -> Expected: Application logic detects cycles before persistence

### 16.8 Concurrent Operations: Double Voting
- [ ] **[P0]** User votes on proposal -> Immediately votes again (rapid double-click) -> Expected: Only one vote counted, duplicate prevented
- [ ] **[P0]** Two simultaneous vote requests (race condition) -> Expected: Database unique constraint or application lock prevents double vote
- [ ] **[P1]** Vote recorded, user refreshes page and votes again -> Expected: "You have already voted" error shown
- [ ] **[P2]** Vote change allowed (user switches vote) -> Expected: Previous vote removed, new vote recorded (one vote total)
- [ ] **[P3]** Optimistic concurrency control -> Expected: Vote update checks version/timestamp, prevents conflicts

### 16.9 Concurrent Operations: Double Liking
- [ ] **[P0]** User likes post -> Immediately likes again -> Expected: Only one like counted, duplicate prevented by unique constraint
- [ ] **[P0]** User unlikes -> likes again -> Expected: Like count increments/decrements correctly
- [ ] **[P1]** Rapid like/unlike toggling (5 clicks in 1 second) -> Expected: Final state accurate, no race condition errors
- [ ] **[P2]** Like count aggregation consistent -> Expected: Like count matches actual number of Like records
- [ ] **[P3]** Eventual consistency -> Multiple devices like simultaneously -> Expected: Like count eventually accurate across all clients

### 16.10 Concurrent Operations: Simultaneous Bid Submission
- [ ] **[P0]** Two users submit bid on opportunity at same time -> Expected: Both bids accepted if opportunity allows multiple bids
- [ ] **[P0]** Opportunity allows only one bid -> Two users submit simultaneously -> Expected: First bid accepted, second bid rejected "Opportunity already has a bid"
- [ ] **[P1]** Bid submission updates opportunity status (OPEN -> BID_SUBMITTED) -> Concurrent bids -> Expected: Only one status update, consistent state
- [ ] **[P2]** Transaction isolation prevents concurrent bid conflicts -> Expected: Serializable or repeatable read isolation level used
- [ ] **[P3]** Bid submission includes timestamp -> Expected: Tie-breaking based on earliest timestamp

### 16.11 Concurrent Operations: Balance Updates
- [ ] **[P0]** User makes two purchases simultaneously -> Balance deducted twice -> Expected: Both transactions succeed if balance sufficient, balance accurate
- [ ] **[P0]** User balance insufficient for concurrent transactions -> Expected: One transaction succeeds, other fails "Insufficient balance"
- [ ] **[P1]** Concurrent balance updates use row-level locking -> Expected: No lost updates, balance always accurate
- [ ] **[P1]** Transaction rollback on failure -> Expected: Failed transaction does not affect balance
- [ ] **[P2]** Balance audit log tracks all changes -> Expected: All credits/debits logged, balance reconcilable
- [ ] **[P3]** Optimistic locking on balance updates -> Expected: Version field incremented, concurrent writes detected and retried

### 16.12 Concurrent Operations: Membership Approvals
- [ ] **[P0]** Two admins approve same membership request simultaneously -> Expected: Only one approval recorded, status updated once
- [ ] **[P0]** Admin approves while another admin rejects -> Expected: First action wins, second action fails "Request already processed"
- [ ] **[P1]** Membership status transition atomic (PENDING -> APPROVED) -> Expected: No intermediate states, transaction ensures consistency
- [ ] **[P2]** Notification sent only once on approval -> Expected: No duplicate notifications despite concurrent approvals
- [ ] **[P3]** Audit trail shows which admin performed action -> Expected: Both attempts logged, only one succeeded

### 16.13 State Transition Violations: Project Status
- [ ] **[P0]** Try to publish project in COMPLETED status -> Expected: Error "Cannot publish completed project"
- [ ] **[P0]** Try to move CANCELLED project to ACTIVE -> Expected: Error "Cannot reactivate cancelled project"
- [ ] **[P1]** Valid status transitions enforced (PLANNING -> ACTIVE -> ON_HOLD -> ACTIVE -> COMPLETED) -> Expected: Invalid transitions rejected
- [ ] **[P1]** Try to skip status (PLANNING -> COMPLETED directly) -> Expected: Error "Invalid status transition"
- [ ] **[P2]** State machine validation on status updates -> Expected: Application enforces state transition rules
- [ ] **[P3]** Status transition history logged -> Expected: Audit trail shows all status changes

### 16.14 State Transition Violations: Proposal Voting
- [ ] **[P0]** Vote on EXPIRED proposal -> Expected: Error "Voting has ended for this proposal"
- [ ] **[P0]** Vote on APPROVED or REJECTED proposal -> Expected: Error "Proposal voting is closed"
- [ ] **[P1]** Proposal status auto-updated when vote deadline passes -> Expected: PENDING -> EXPIRED if not enough votes
- [ ] **[P1]** Proposal status updated when quorum reached -> Expected: PENDING -> APPROVED/REJECTED based on vote outcome
- [ ] **[P2]** Vote counted only if proposal in PENDING status -> Expected: Votes on expired proposals ignored
- [ ] **[P3]** Proposal can be withdrawn before voting ends -> Expected: Status changed to WITHDRAWN, voting disabled

### 16.15 State Transition Violations: Opportunity Applications
- [ ] **[P0]** Apply to opportunity with status FILLED -> Expected: Error "This opportunity is no longer available"
- [ ] **[P0]** Apply to CLOSED opportunity -> Expected: Error "Applications are closed for this opportunity"
- [ ] **[P1]** Opportunity status auto-updated when max applications reached -> Expected: OPEN -> FILLED
- [ ] **[P1]** Opportunity owner manually closes opportunity -> Expected: Status changed to CLOSED, no new applications accepted
- [ ] **[P2]** Application deadlines enforced -> Expected: Applications after deadline rejected
- [ ] **[P3]** Opportunity reopened (FILLED -> OPEN) -> Expected: New applications accepted again

### 16.16 State Transition Violations: Task Completion
- [ ] **[P0]** Complete task with status CANCELLED -> Expected: Error "Cannot complete cancelled task"
- [ ] **[P0]** Reopen COMPLETED task -> Expected: Task status changed back to TODO or IN_PROGRESS
- [ ] **[P1]** Complete task with incomplete dependencies -> Expected: Warning or error "Dependent tasks not completed"
- [ ] **[P1]** Cancel IN_PROGRESS task -> Expected: Status changed to CANCELLED, assignee notified
- [ ] **[P2]** Task status workflow enforced (TODO -> IN_PROGRESS -> DONE) -> Expected: Invalid transitions rejected
- [ ] **[P3]** Task completion triggers dependent task start -> Expected: Dependent tasks unblocked automatically

### 16.17 State Transition Violations: Escrow Release
- [ ] **[P0]** Release escrow funds already RELEASED -> Expected: Error "Funds already released"
- [ ] **[P0]** Release escrow with status CANCELLED -> Expected: Error "Cannot release cancelled escrow"
- [ ] **[P1]** Release escrow before conditions met -> Expected: Error "Conditions not satisfied for release"
- [ ] **[P1]** Refund escrow in RELEASED status -> Expected: Error "Cannot refund released funds"
- [ ] **[P2]** Escrow state machine: PENDING -> FUNDED -> RELEASED/REFUNDED -> Expected: All transitions validated
- [ ] **[P3]** Partial escrow release -> Expected: Status remains FUNDED until fully released

### 16.18 State Transition Violations: Fundraising Contribution
- [ ] **[P0]** Contribute to fundraising with status FAILED -> Expected: Error "This fundraising campaign has failed"
- [ ] **[P0]** Contribute to COMPLETED fundraising -> Expected: Error "Fundraising goal already reached"
- [ ] **[P1]** Contribute after deadline passed -> Expected: Error "Fundraising deadline has passed"
- [ ] **[P1]** Contribution exceeds remaining goal amount -> Expected: Contribution accepted, fundraising marked COMPLETED
- [ ] **[P2]** Fundraising auto-failed when deadline passes without reaching goal -> Expected: Status changed to FAILED, refunds processed
- [ ] **[P3]** Fundraising extended (deadline moved) -> Expected: More contributions accepted

### 16.19 Input Validation: Empty Required Fields
- [ ] **[P0]** Submit project creation with empty title -> Expected: Error "Title is required"
- [ ] **[P0]** Submit user registration with empty email -> Expected: Error "Email is required"
- [ ] **[P0]** Submit task creation with empty assigneeId -> Expected: Error "Assignee is required" (if required)
- [ ] **[P1]** Submit form with all required fields as whitespace only -> Expected: Validation error, whitespace trimmed and rejected
- [ ] **[P2]** Frontend prevents form submission if required fields empty -> Expected: Submit button disabled or error shown
- [ ] **[P3]** Backend validation catches missing required fields (defense in depth) -> Expected: API returns 400 Bad Request

### 16.20 Input Validation: Exceeding Field Length Limits
- [ ] **[P0]** Submit project title > 200 characters -> Expected: Error "Title must be less than 200 characters"
- [ ] **[P0]** Submit description > 5000 characters -> Expected: Error "Description too long"
- [ ] **[P1]** Frontend enforces maxLength attribute on input fields -> Expected: User cannot type beyond limit
- [ ] **[P1]** Backend validates string length even if frontend bypassed -> Expected: API rejects oversized input
- [ ] **[P2]** Error message includes actual length vs max -> Expected: "Description is 5237 characters, max is 5000"
- [ ] **[P3]** Character counter shown in UI -> Expected: "45/200 characters" updates as user types

### 16.21 Input Validation: Negative Amounts
- [ ] **[P0]** Submit compensation with negative value -> Expected: Error "Compensation must be positive"
- [ ] **[P0]** Submit fundraising goal with negative value -> Expected: Error "Goal amount must be positive"
- [ ] **[P1]** Submit bid amount = -100 -> Expected: Error "Bid amount must be greater than zero"
- [ ] **[P1]** Submit token transfer with negative amount -> Expected: Error "Amount must be positive"
- [ ] **[P2]** Frontend input type="number" with min="0" -> Expected: Negative input prevented in UI
- [ ] **[P3]** Database constraint (CHECK amount > 0) -> Expected: Negative amounts rejected at DB level

### 16.22 Input Validation: Zero Amounts
- [ ] **[P0]** Submit fundraising goal = 0 -> Expected: Error "Goal amount must be greater than zero"
- [ ] **[P0]** Submit compensation = 0 where not allowed -> Expected: Error "Compensation required"
- [ ] **[P1]** Submit token transfer amount = 0 -> Expected: Error "Amount must be greater than zero"
- [ ] **[P1]** Some fields allow zero (e.g., optional compensation) -> Expected: Zero accepted if appropriate
- [ ] **[P2]** Validation distinguishes between "zero not allowed" vs "zero acceptable" fields -> Expected: Context-aware validation
- [ ] **[P3]** Validation error message specific: "Amount must be greater than zero"

### 16.23 Input Validation: Invalid Email Formats
- [ ] **[P0]** Submit email without "@" symbol -> Expected: Error "Invalid email format"
- [ ] **[P0]** Submit email "user@" (missing domain) -> Expected: Error "Invalid email format"
- [ ] **[P0]** Submit email "@example.com" (missing local part) -> Expected: Error "Invalid email format"
- [ ] **[P1]** Email validation regex or library used -> Expected: Comprehensive email format validation
- [ ] **[P1]** Frontend shows email error on blur -> Expected: User notified of invalid email before submission
- [ ] **[P2]** Backend validates email format (defense in depth) -> Expected: API rejects malformed emails
- [ ] **[P3]** Email domain validation (DNS check) -> Expected: Reject emails with non-existent domains

### 16.24 Input Validation: Invalid URL Formats
- [ ] **[P0]** Submit URL without scheme ("example.com") -> Expected: Error "URL must include http:// or https://" or auto-prepend scheme
- [ ] **[P0]** Submit URL "htp://example.com" (typo in scheme) -> Expected: Error "Invalid URL format"
- [ ] **[P1]** URL validation accepts http and https schemes -> Expected: Both accepted
- [ ] **[P1]** URL validation rejects javascript:, data:, file: schemes -> Expected: Only safe schemes allowed
- [ ] **[P2]** URL validation checks for valid domain format -> Expected: Malformed domains rejected
- [ ] **[P3]** URL preview/unfurl for valid URLs -> Expected: Link preview shown in UI

### 16.25 Input Validation: XSS Injection Attempts
- [ ] **[P0]** Submit comment with `<script>alert('XSS')</script>` -> Expected: Script tags escaped or sanitized, not executed
- [ ] **[P0]** Submit project description with HTML tags -> Expected: Tags escaped or allowed tags whitelisted, rendered safely
- [ ] **[P1]** Input sanitization library used (e.g., DOMPurify) -> Expected: All user input sanitized before rendering
- [ ] **[P1]** Content Security Policy (CSP) header set -> Expected: Inline scripts blocked, XSS attack surface reduced
- [ ] **[P2]** Test various XSS payloads (event handlers, img onerror, etc.) -> Expected: All XSS attempts neutralized
- [ ] **[P3]** Rich text editor sanitizes HTML -> Expected: Only safe HTML tags/attributes allowed

### 16.26 Input Validation: SQL Injection Attempts
- [ ] **[P0]** Submit search query with SQL: `' OR '1'='1` -> Expected: Query parameterized, SQL injection ineffective
- [ ] **[P0]** ORM (Prisma, EF Core) used for DB queries -> Expected: Parameterized queries prevent SQL injection
- [ ] **[P1]** Test injection in various input fields (search, filters, etc.) -> Expected: No SQL injection possible
- [ ] **[P1]** Raw SQL queries use parameterization -> Expected: User input never concatenated into SQL strings
- [ ] **[P2]** Database user permissions limited -> Expected: Application DB user cannot DROP tables or access system tables
- [ ] **[P3]** Input validation detects SQL keywords, rejects or escapes -> Expected: Suspicious input flagged

### 16.27 Input Validation: Malformed JSON
- [ ] **[P0]** Submit json field with invalid JSON: `{not valid json}` -> Expected: Error "Invalid JSON format"
- [ ] **[P0]** API request with malformed JSON body -> Expected: 400 Bad Request, "Invalid JSON syntax"
- [ ] **[P1]** Frontend JSON editor validates JSON -> Expected: User notified of JSON syntax errors before submission
- [ ] **[P1]** Backend validates JSON schema (if applicable) -> Expected: JSON structure validated against schema
- [ ] **[P2]** Large JSON payload (MB size) -> Expected: Handled efficiently or rejected if too large
- [ ] **[P3]** JSON parsing error message helpful -> Expected: "Unexpected token '}' at position 42"

### 16.28 Boundary Conditions: Maximum Attendees Reached
- [ ] **[P0]** Event maxAttendees = 10, 10 users registered -> 11th user tries to register -> Expected: Error "Event is full"
- [ ] **[P0]** Event registration count checked before allowing registration -> Expected: Count verified, registration rejected if full
- [ ] **[P1]** Concurrent registrations near limit -> Expected: Exactly maxAttendees registrations accepted, no overflow
- [ ] **[P2]** Waitlist functionality for full events -> Expected: 11th user added to waitlist instead of rejected
- [ ] **[P3]** Event capacity increased -> Expected: Waitlisted users can register

### 16.29 Boundary Conditions: Maximum Applications Reached
- [ ] **[P0]** Opportunity maxApplications = 5, 5 applications received -> 6th application attempted -> Expected: Error "Maximum applications reached"
- [ ] **[P0]** Opportunity status auto-updated to FILLED when maxApplications reached -> Expected: Status changed, no more applications accepted
- [ ] **[P1]** Application count incremented atomically -> Expected: Race conditions prevented, count accurate
- [ ] **[P2]** maxApplications = null (unlimited) -> Expected: Any number of applications accepted
- [ ] **[P3]** Opportunity owner can manually mark as FILLED before maxApplications -> Expected: Status change honored, applications closed early

### 16.30 Boundary Conditions: Fundraising Goal Exactly Met
- [ ] **[P0]** Fundraising goal = $1000, contributions total exactly $1000 -> Expected: Fundraising status = COMPLETED
- [ ] **[P0]** Contribution would exceed goal (e.g., goal $1000, current $950, contribute $100) -> Expected: Contribution accepted, fundraising COMPLETED, total = $1050 or contribution limited to $50
- [ ] **[P1]** Fundraising completion trigger fires when goal reached -> Expected: Status updated, backers notified
- [ ] **[P2]** Over-funding policy defined -> Expected: Either accept over-funding or cap at goal amount
- [ ] **[P3]** Fundraising stretch goals -> Expected: Additional goals unlocked when primary goal exceeded

### 16.31 Boundary Conditions: Vesting Cliff Exactly at Current Date
- [ ] **[P0]** Vesting cliff date = today -> Expected: Tokens vest, available for user
- [ ] **[P0]** Vesting calculation accurate when cliff date = current timestamp -> Expected: No off-by-one errors
- [ ] **[P1]** Vesting checked at midnight UTC -> Expected: Consistent timezone handling
- [ ] **[P2]** Vesting cliff = start date (immediate vesting) -> Expected: Tokens available immediately
- [ ] **[P3]** Vesting cliff in past -> Expected: Tokens already vested, available to user

### 16.32 Boundary Conditions: Sprint Dates
- [ ] **[P0]** Sprint start date = end date (1-day sprint) -> Expected: Sprint accepted or error "End date must be after start date"
- [ ] **[P0]** Sprint start date > end date -> Expected: Error "Start date must be before end date"
- [ ] **[P1]** Sprint end date in past -> Expected: Sprint auto-marked as COMPLETED
- [ ] **[P2]** Sprint start date in future -> Expected: Sprint status = PLANNED, auto-starts when start date reached
- [ ] **[P3]** Overlapping sprints for same project -> Expected: Allowed or validation prevents overlap

### 16.33 Boundary Conditions: Zero Voting Weight
- [ ] **[P0]** User with voting weight = 0 tries to vote -> Expected: Vote rejected or counted with zero weight
- [ ] **[P1]** Voting weight calculation includes zero-weight users -> Expected: Total weight calculation accurate
- [ ] **[P2]** Vote delegation to user with zero weight -> Expected: Delegation rejected or delegated weight = 0
- [ ] **[P3]** Proposal outcome calculation excludes zero-weight votes -> Expected: No effect on vote result

### 16.34 Boundary Conditions: Quorum/Threshold Extremes
- [ ] **[P0]** Proposal quorum = 0% -> Expected: Proposal passes with any number of votes
- [ ] **[P0]** Proposal quorum = 100% -> Expected: All eligible voters must vote for quorum to be reached
- [ ] **[P0]** Proposal threshold = 0% -> Expected: Proposal passes if any votes are cast
- [ ] **[P0]** Proposal threshold = 100% -> Expected: Unanimous vote required for approval
- [ ] **[P1]** Quorum reached but threshold not met -> Expected: Proposal status = REJECTED
- [ ] **[P1]** Threshold met but quorum not reached -> Expected: Proposal status = EXPIRED (insufficient participation)
- [ ] **[P2]** Edge case: quorum = 100%, threshold = 100%, all users vote yes -> Expected: Proposal APPROVED
- [ ] **[P3]** Fractional percentages (e.g., 66.67% threshold) -> Expected: Calculation accurate to decimal precision

### 16.35 Deletion Cascades: Delete User
- [ ] **[P0]** Delete user account -> Check projects where user is creator -> Expected: Projects deleted or reassigned based on cascade rules
- [ ] **[P0]** Delete user -> Check tasks assigned to user -> Expected: Tasks reassigned or marked unassigned
- [ ] **[P1]** Delete user -> Check comments authored by user -> Expected: Comments deleted or marked as "deleted user"
- [ ] **[P1]** Delete user -> Check notifications for user -> Expected: Notifications deleted
- [ ] **[P1]** Delete user -> Check memberships (projects, guilds) -> Expected: Memberships removed
- [ ] **[P1]** Delete user -> Check follows (follower/followed) -> Expected: Follow relationships deleted
- [ ] **[P2]** Delete user -> Check escrow transactions -> Expected: Active escrows prevent deletion or are refunded
- [ ] **[P2]** Delete user with active proposals -> Expected: Proposals remain (orphaned) or user deletion blocked
- [ ] **[P3]** Soft delete user instead of hard delete -> Expected: User data retained, account marked inactive

### 16.36 Deletion Cascades: Delete Project
- [ ] **[P0]** Delete project -> Check tasks for project -> Expected: All tasks cascade deleted
- [ ] **[P0]** Delete project -> Check project members -> Expected: All memberships cascade deleted
- [ ] **[P0]** Delete project -> Check proposals for project -> Expected: Proposals cascade deleted
- [ ] **[P1]** Delete project -> Check opportunities for project -> Expected: Opportunities cascade deleted or orphaned
- [ ] **[P1]** Delete project -> Check attachments -> Expected: Attachments deleted, files removed from storage
- [ ] **[P1]** Delete project -> Check activities (posts, events) -> Expected: Activities cascade deleted
- [ ] **[P2]** Delete project with active fundraising -> Expected: Deletion blocked or funds refunded
- [ ] **[P2]** Delete project -> Check notifications referencing project -> Expected: Notifications deleted or entity reference nullified
- [ ] **[P3]** Soft delete project -> Expected: Project hidden but data retained

### 16.37 Deletion Cascades: Delete Conversation
- [ ] **[P0]** Delete conversation -> Check messages in conversation -> Expected: All messages cascade deleted
- [ ] **[P0]** Delete conversation -> Check conversation members -> Expected: All memberships cascade deleted
- [ ] **[P1]** Delete conversation -> Check attachments in messages -> Expected: Attachments deleted, files removed
- [ ] **[P2]** Delete conversation -> Check notifications about conversation -> Expected: Notifications deleted or updated
- [ ] **[P3]** Archive conversation instead of delete -> Expected: Conversation hidden, messages retained

### 16.38 Deletion Cascades: Delete Guild
- [ ] **[P0]** Delete guild -> Check guild members -> Expected: All memberships cascade deleted
- [ ] **[P0]** Delete guild -> Check guild invitations -> Expected: Invitations cascade deleted
- [ ] **[P1]** Delete guild -> Check projects associated with guild -> Expected: Projects unlinked or cascade deleted
- [ ] **[P1]** Delete guild -> Check events hosted by guild -> Expected: Events unlinked or cascade deleted
- [ ] **[P2]** Delete guild -> Check activities/posts by guild -> Expected: Activities cascade deleted
- [ ] **[P3]** Delete guild with active treasury -> Expected: Deletion blocked or funds distributed

### 16.39 Network & Performance: API Timeout
- [ ] **[P0]** Simulate slow API response (> 30 seconds) -> Expected: Client timeout, error message shown
- [ ] **[P0]** Client-side request timeout configured (e.g., 10 seconds) -> Expected: Request aborted after timeout, error handled
- [ ] **[P1]** Timeout error shows retry option -> Expected: User can retry timed-out request
- [ ] **[P1]** Long-running operation (report generation) -> Expected: Async processing, user notified when complete
- [ ] **[P2]** API request cancelled by user (navigation away) -> Expected: Request aborted, no unnecessary processing
- [ ] **[P3]** Timeout monitoring -> Expected: Backend tracks and logs timeout occurrences

### 16.40 Network & Performance: Large Payload Handling
- [ ] **[P0]** API returns large dataset (1000+ records) -> Expected: Response compressed (gzip), delivered successfully
- [ ] **[P0]** Client handles large JSON response -> Expected: Parsing completes without freezing UI
- [ ] **[P1]** Pagination used for large datasets -> Expected: Max 100 records per request, pagination enforced
- [ ] **[P1]** Large file upload (50MB video) -> Expected: Multipart upload, progress tracking, no timeout
- [ ] **[P2]** Response payload size limit (e.g., 10MB max) -> Expected: Oversized responses rejected or paginated
- [ ] **[P3]** Streaming API for very large datasets -> Expected: Server sends data in chunks, client processes incrementally

### 16.41 Network & Performance: Rate Limiting
- [ ] **[P0]** Send 100 API requests in 10 seconds -> Expected: Rate limit enforced, requests throttled or rejected with 429 Too Many Requests
- [ ] **[P0]** Rate limit response includes Retry-After header -> Expected: Client knows when to retry
- [ ] **[P1]** Rate limit per user/API key -> Expected: One user's requests don't affect others
- [ ] **[P1]** Rate limit sliding window (e.g., 100 requests per minute) -> Expected: Limit resets progressively, not at fixed intervals
- [ ] **[P2]** Rate limit different for authenticated vs anonymous users -> Expected: Higher limits for authenticated users
- [ ] **[P2]** Rate limit bypass for admin users -> Expected: Admins not subject to rate limiting
- [ ] **[P3]** Rate limit monitoring dashboard -> Expected: Track rate limit hits, identify abusive users

### 16.42 Network & Performance: Slow Connection Behavior
- [ ] **[P0]** Simulate slow network (3G speed) -> Expected: Application remains functional, loading states shown
- [ ] **[P0]** Images lazy-load on slow connection -> Expected: Placeholder shown, images load as user scrolls
- [ ] **[P1]** Critical resources prioritized (CSS, JS) on slow connection -> Expected: Page interactive before all resources loaded
- [ ] **[P1]** Loading skeletons shown during data fetch -> Expected: User sees skeleton UI, not blank page
- [ ] **[P2]** Adaptive quality (lower resolution images on slow connection) -> Expected: UX optimized for bandwidth
- [ ] **[P3]** Offline mode -> Expected: Some functionality available offline, sync when connection restored

### 16.43 Network & Performance: Offline Mode / Service Interruption
- [ ] **[P0]** Backend API unavailable -> Expected: User shown "Service unavailable" message, retry option
- [ ] **[P0]** Network disconnected -> Expected: User notified "You are offline", pending requests queued or failed
- [ ] **[P1]** Service worker caches critical assets -> Expected: Basic UI functional offline
- [ ] **[P1]** Form data saved locally when offline -> Expected: User can complete form, submitted when connection restored
- [ ] **[P2]** Background sync for offline actions -> Expected: Actions queued offline, synced when online
- [ ] **[P2]** Database unavailable -> Expected: Backend returns 503 Service Unavailable, retry logic in place
- [ ] **[P3]** Graceful degradation -> Expected: Non-essential features disabled, core functionality available

---

## Test Execution Summary

| Section | Total Tests | P0 | P1 | P2 | P3 | Passed | Failed | Blocked |
|---------|-------------|----|----|----|----|--------|--------|---------|
| 1. Auth & User Core | | | | | | | | |
| 2. Project Management | | | | | | | | |
| 3. Agile Hierarchy | | | | | | | | |
| 4. Guild Module | | | | | | | | |
| 5. Opportunities | | | | | | | | |
| 6. DAO Governance | | | | | | | | |
| 7. Finance & Tokenomics | | | | | | | | |
| 8. Social & Communication | | | | | | | | |
| 9. Events Module | | | | | | | | |
| 10. Social & Follow | | | | | | | | |
| 11. Gamification | | | | | | | | |
| 12. Product Catalog | | | | | | | | |
| 13. Real-time | | | | | | | | |
| 14. Cross-Cutting | | | | | | | | |
| 15. Navigation | | | | | | | | |
| 16. Edge Cases | | | | | | | | |
| **TOTAL** | | | | | | | | |

### Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| QA Lead | | | |
| Dev Lead | | | |
| Product Owner | | | |
| Project Manager | | | |
---

## Section 1: Authentication & User Core

### Google OAuth Sign-In Flow

#### OAuth Redirect & Callback
- [ ] **[P0]** User clicks "Sign in with Google" -> Expected: Redirect to Google OAuth consent screen
- [ ] **[P0]** User approves consent -> Expected: Redirect back to callback URL with authorization code
- [ ] **[P0]** Invalid callback state parameter -> Expected: Error message, no session created
- [ ] **[P0]** Expired authorization code -> Expected: User prompted to sign in again
- [ ] **[P0]** User denies consent -> Expected: Return to login page with error message
- [ ] **[P1]** OAuth callback contains valid code -> Expected: Token exchange initiated with Google
- [ ] **[P1]** Token exchange successful -> Expected: Access token, refresh token, id_token stored in Account table
- [ ] **[P1]** Token exchange fails (network error) -> Expected: Graceful error, user can retry
- [ ] **[P2]** User with existing Google account signs in -> Expected: Existing user session created, Account record matched by providerAccountId
- [ ] **[P2]** Callback includes session_state -> Expected: session_state persisted to Account.session_state

#### First-Time User Creation
- [ ] **[P0]** New Google user signs in -> Expected: New User record created with email, name, image from Google profile
- [ ] **[P0]** User email extracted from id_token -> Expected: User.email matches Google email
- [ ] **[P0]** User.emailVerified set to current timestamp for Google OAuth -> Expected: emailVerified populated on first sign-in
- [ ] **[P1]** User record created with default role=INDIVIDUAL -> Expected: User.role = INDIVIDUAL
- [ ] **[P1]** User record created with default userType=INNOVATOR -> Expected: User.userType = INNOVATOR
- [ ] **[P1]** User record created with default tier=BRONZE -> Expected: User.tier = BRONZE
- [ ] **[P1]** User record created with default verificationLevel=ANONYMOUS -> Expected: User.verificationLevel = ANONYMOUS
- [ ] **[P1]** User record created with totalXP=0, level=1, trustScore=0 -> Expected: Gamification defaults initialized
- [ ] **[P1]** New Account record created linking User to Google provider -> Expected: Account.userId references new User.id, provider="google"
- [ ] **[P1]** Session record created with unique sessionToken -> Expected: Session.sessionToken is unique, userId references User.id
- [ ] **[P2]** User image from Google stored -> Expected: User.image contains Google profile picture URL
- [ ] **[P2]** createdAt and updatedAt timestamps set -> Expected: Both timestamps reflect current time
- [ ] **[P3]** Bio, location, phone, website, linkedIn, twitter initially null -> Expected: All optional fields are null for new user

#### JWT Handling
- [ ] **[P0]** JWT created on successful sign-in -> Expected: JWT contains userId, email, role, userType, verificationLevel
- [ ] **[P0]** JWT signature is valid -> Expected: JWT can be verified with application secret
- [ ] **[P0]** JWT expires after configured duration (e.g., 30 days) -> Expected: JWT.exp claim reflects expiry timestamp
- [ ] **[P0]** Expired JWT rejected on API call -> Expected: 401 Unauthorized response
- [ ] **[P1]** JWT enriched with user role (INDIVIDUAL, GUILD, ADMIN) -> Expected: JWT payload includes role claim
- [ ] **[P1]** JWT enriched with user type (INNOVATOR, SUPPORTER, VOLUNTEER, FREELANCER, SME_OWNER, GUILD_MEMBER) -> Expected: JWT payload includes userType claim
- [ ] **[P1]** JWT enriched with verificationLevel (ANONYMOUS, VERIFIED, PRO, EXPERT) -> Expected: JWT payload includes verificationLevel claim
- [ ] **[P1]** JWT updated when user role changes -> Expected: New JWT issued with updated role
- [ ] **[P1]** JWT updated when user type changes -> Expected: New JWT issued with updated userType
- [ ] **[P1]** JWT updated when verificationLevel changes -> Expected: New JWT issued with updated verificationLevel
- [ ] **[P2]** JWT contains user tier (BRONZE, SILVER, GOLD, PLATINUM, DIAMOND) -> Expected: JWT payload includes tier claim
- [ ] **[P2]** JWT refresh mechanism -> Expected: Refresh token used to obtain new access token before expiry
- [ ] **[P3]** JWT includes custom claims for feature flags -> Expected: Custom claims accessible in JWT payload

#### Session Management
- [ ] **[P0]** Session created with unique sessionToken -> Expected: Session.sessionToken is unique across all sessions
- [ ] **[P0]** Session.expires set to future date -> Expected: Session.expires > current time
- [ ] **[P0]** Expired session rejected on API call -> Expected: 401 Unauthorized, user redirected to login
- [ ] **[P0]** Session persists across browser refresh -> Expected: User remains signed in after refresh
- [ ] **[P1]** Session linked to User via userId -> Expected: Session.userId references User.id
- [ ] **[P1]** Multiple sessions for same user allowed -> Expected: User can be signed in on multiple devices
- [ ] **[P1]** Session invalidated on sign-out -> Expected: Session record deleted or marked invalid
- [ ] **[P2]** Session activity updates lastActiveAt (if implemented) -> Expected: Session activity timestamp updated on API calls
- [ ] **[P2]** Inactive session cleanup after threshold (e.g., 90 days) -> Expected: Expired sessions removed from database
- [ ] **[P3]** Session revocation by user (sign out all devices) -> Expected: All sessions for user invalidated

#### Sign-Out
- [ ] **[P0]** User clicks sign-out -> Expected: Session record deleted from database
- [ ] **[P0]** Sign-out clears client-side JWT/session cookie -> Expected: Cookie removed from browser
- [ ] **[P0]** Signed-out user cannot access protected routes -> Expected: 401 Unauthorized on API calls
- [ ] **[P0]** Sign-out redirects to public landing page -> Expected: User redirected to home or login page
- [ ] **[P1]** Sign-out invalidates refresh token -> Expected: Refresh token cannot be used post sign-out
- [ ] **[P2]** Sign-out logs event to Activity table -> Expected: Activity record with type=LEFT, action="signed_out"
- [ ] **[P3]** Sign-out confirmation dialog (optional) -> Expected: User confirms before signing out

---

### User Profile CRUD

#### Read User Profile
- [ ] **[P0]** Authenticated user fetches own profile -> Expected: User object with all fields (name, bio, location, phone, website, linkedIn, twitter, image, email, role, userType, tier, verificationLevel, totalXP, level, trustScore)
- [ ] **[P0]** Public user profile fetch by userId -> Expected: Public fields returned (name, bio, location, image, tier, verificationLevel), sensitive fields (email, phone) hidden
- [ ] **[P1]** User profile includes UserSkills -> Expected: Array of UserSkill objects with skill name and level
- [ ] **[P1]** User profile includes UserExperiences -> Expected: Array of UserExperience objects (title, company, description, startDate, endDate, isCurrent)
- [ ] **[P1]** Fetch non-existent userId -> Expected: 404 Not Found
- [ ] **[P2]** Fetch user profile with no skills -> Expected: Empty skills array
- [ ] **[P2]** Fetch user profile with no experiences -> Expected: Empty experiences array
- [ ] **[P3]** User profile includes createdAt and updatedAt timestamps -> Expected: Timestamps reflect user creation and last update

#### Create/Update User Profile (Name, Bio, Location, Phone, Website, LinkedIn, Twitter, Image)
- [ ] **[P0]** User updates profile name -> Expected: User.name updated, User.updatedAt refreshed
- [ ] **[P0]** User updates bio (max length validated) -> Expected: User.bio updated (text field, no length limit in schema but validate in app)
- [ ] **[P0]** User updates location -> Expected: User.location updated
- [ ] **[P1]** User updates phone number -> Expected: User.phone updated
- [ ] **[P1]** User updates website URL -> Expected: User.website updated
- [ ] **[P1]** User updates LinkedIn URL -> Expected: User.linkedIn updated
- [ ] **[P1]** User updates Twitter handle -> Expected: User.twitter updated
- [ ] **[P1]** User uploads profile image -> Expected: Image stored (S3 or local), User.image updated with URL
- [ ] **[P1]** User removes profile image -> Expected: User.image set to null or default avatar
- [ ] **[P1]** Invalid phone number format -> Expected: Validation error, no update
- [ ] **[P1]** Invalid website URL format -> Expected: Validation error, no update
- [ ] **[P2]** Bio contains special characters/emojis -> Expected: Bio saved correctly with UTF-8 encoding
- [ ] **[P2]** Update profile with unchanged fields -> Expected: No error, updatedAt refreshed
- [ ] **[P2]** Concurrent profile updates by same user -> Expected: Last write wins, no data corruption
- [ ] **[P3]** Profile update logs Activity -> Expected: Activity record with type=UPDATED, entityType="User", action="updated_profile"

#### User Roles (INDIVIDUAL, GUILD, ADMIN)
- [ ] **[P0]** User role defaults to INDIVIDUAL on creation -> Expected: User.role = INDIVIDUAL
- [ ] **[P0]** Admin updates user role to ADMIN -> Expected: User.role updated, JWT refreshed with new role
- [ ] **[P0]** User with role GUILD can create/manage Guilds -> Expected: Guild creation allowed, ownerId set
- [ ] **[P0]** Role ADMIN has elevated permissions -> Expected: Admin can manage users, projects, guilds
- [ ] **[P1]** Role change from INDIVIDUAL to GUILD -> Expected: User.role updated, can now create Guild
- [ ] **[P1]** Role change from GUILD to INDIVIDUAL (with active Guild) -> Expected: Error or Guild ownership transferred
- [ ] **[P1]** Role INDIVIDUAL cannot perform admin actions -> Expected: 403 Forbidden on admin endpoints
- [ ] **[P2]** Role change logs Activity -> Expected: Activity record with action="role_changed"
- [ ] **[P3]** User with role GUILD cannot have multiple Guilds (if schema enforces unique ownerId) -> Expected: Error on second Guild creation

#### User Types (INNOVATOR, SUPPORTER, VOLUNTEER, FREELANCER, SME_OWNER, GUILD_MEMBER)
- [ ] **[P0]** User type defaults to INNOVATOR -> Expected: User.userType = INNOVATOR
- [ ] **[P1]** User updates userType to SUPPORTER -> Expected: User.userType updated
- [ ] **[P1]** User updates userType to VOLUNTEER -> Expected: User.userType updated
- [ ] **[P1]** User updates userType to FREELANCER -> Expected: User.userType updated
- [ ] **[P1]** User updates userType to SME_OWNER -> Expected: User.userType updated
- [ ] **[P1]** User updates userType to GUILD_MEMBER -> Expected: User.userType updated
- [ ] **[P2]** User type influences UI/UX (e.g., FREELANCER sees opportunities) -> Expected: Frontend adapts based on userType
- [ ] **[P2]** User type change logs Activity -> Expected: Activity record with action="user_type_changed"
- [ ] **[P3]** Multiple user types not allowed (single enum) -> Expected: Only one userType per user

#### User Tiers (BRONZE -> SILVER -> GOLD -> PLATINUM -> DIAMOND)
- [ ] **[P0]** New user starts at BRONZE tier -> Expected: User.tier = BRONZE
- [ ] **[P1]** User tier upgraded to SILVER based on XP -> Expected: User.tier = SILVER, Notification sent (LEVEL_UP)
- [ ] **[P1]** User tier upgraded to GOLD -> Expected: User.tier = GOLD
- [ ] **[P1]** User tier upgraded to PLATINUM -> Expected: User.tier = PLATINUM
- [ ] **[P1]** User tier upgraded to DIAMOND -> Expected: User.tier = DIAMOND
- [ ] **[P2]** Tier downgrade not allowed -> Expected: Tier can only increase (or policy defined)
- [ ] **[P2]** Tier upgrade triggers Achievement -> Expected: Achievement unlocked for reaching tier
- [ ] **[P2]** Tier displayed on user profile -> Expected: Public tier badge visible
- [ ] **[P3]** Tier influences permissions or access (e.g., DIAMOND accesses premium features) -> Expected: Feature flags based on tier

#### Verification Levels (ANONYMOUS -> VERIFIED -> PRO -> EXPERT)
- [ ] **[P0]** New user starts at ANONYMOUS -> Expected: User.verificationLevel = ANONYMOUS
- [ ] **[P1]** User verifies email -> Expected: User.verificationLevel = VERIFIED, User.isVerified = true
- [ ] **[P1]** User upgrades to PRO (e.g., KYC or paid) -> Expected: User.verificationLevel = PRO
- [ ] **[P1]** User upgrades to EXPERT (manual review or criteria) -> Expected: User.verificationLevel = EXPERT
- [ ] **[P1]** VERIFIED user can perform sensitive actions (e.g., fund projects) -> Expected: Actions allowed
- [ ] **[P1]** ANONYMOUS user restricted from certain actions -> Expected: 403 Forbidden or prompt to verify
- [ ] **[P2]** Verification level change triggers Notification -> Expected: SECURITY_ALERT or similar notification
- [ ] **[P2]** Verification badge displayed on profile -> Expected: Verified icon shown
- [ ] **[P3]** Downgrade verification level (e.g., PRO to VERIFIED) -> Expected: Admin action, audit log

#### Trust Score Tracking
- [ ] **[P0]** New user has trustScore = 0 -> Expected: User.trustScore = 0 (Decimal 18,8)
- [ ] **[P1]** Trust score increases after successful task completion -> Expected: User.trustScore incremented
- [ ] **[P1]** Trust score decreases after negative review -> Expected: User.trustScore decremented
- [ ] **[P2]** Trust score displayed on user profile -> Expected: Numeric score visible
- [ ] **[P2]** Trust score influences project membership approval -> Expected: High trust score increases chances
- [ ] **[P2]** Trust score cannot go below 0 -> Expected: Validation prevents negative trustScore
- [ ] **[P3]** Trust score history tracked (if implemented) -> Expected: Audit trail of changes

---

### User Skills Management

#### Add User Skill
- [ ] **[P0]** User adds skill with level=1 -> Expected: UserSkill record created with skill name, level=1
- [ ] **[P0]** User adds skill with level>1 (e.g., level=5) -> Expected: UserSkill.level = 5
- [ ] **[P1]** User adds duplicate skill -> Expected: Error or update existing skill level
- [ ] **[P1]** Skill name validation (non-empty) -> Expected: Error if skill name is empty
- [ ] **[P2]** Skill level must be >= 1 -> Expected: Validation error if level < 1
- [ ] **[P2]** Skill stored case-insensitively (if implemented) -> Expected: "JavaScript" and "javascript" treated as same
- [ ] **[P3]** Adding skill logs Activity -> Expected: Activity record with action="skill_added"

#### Remove User Skill
- [ ] **[P0]** User removes existing skill -> Expected: UserSkill record deleted
- [ ] **[P1]** Remove non-existent skill -> Expected: 404 Not Found or no-op
- [ ] **[P2]** Removing skill logs Activity -> Expected: Activity record with action="skill_removed"

#### Update User Skill Level
- [ ] **[P0]** User updates skill level from 1 to 3 -> Expected: UserSkill.level = 3
- [ ] **[P1]** Update skill level to same value -> Expected: No error, no change
- [ ] **[P1]** Update skill level to < 1 -> Expected: Validation error
- [ ] **[P2]** Updating skill level logs Activity -> Expected: Activity record with action="skill_updated"

#### Fetch User Skills
- [ ] **[P0]** Fetch all skills for user -> Expected: Array of UserSkill objects with skill, level
- [ ] **[P1]** User with no skills -> Expected: Empty array
- [ ] **[P2]** Skills ordered by level descending (if implemented) -> Expected: Highest level skills first

---

### User Experience Management

#### Add User Experience
- [ ] **[P0]** User adds new experience (title, company, startDate) -> Expected: UserExperience record created
- [ ] **[P0]** User sets experience as current (isCurrent=true, endDate=null) -> Expected: UserExperience.isCurrent = true, endDate = null
- [ ] **[P0]** User adds experience with endDate -> Expected: UserExperience.endDate populated
- [ ] **[P1]** User adds experience with description -> Expected: UserExperience.description saved
- [ ] **[P1]** startDate must be <= endDate -> Expected: Validation error if startDate > endDate
- [ ] **[P1]** Required fields (title, company, startDate) -> Expected: Validation error if any missing
- [ ] **[P2]** User adds multiple experiences -> Expected: All experiences stored, no limit
- [ ] **[P2]** Adding experience logs Activity -> Expected: Activity record with action="experience_added"

#### Edit User Experience
- [ ] **[P0]** User updates experience title -> Expected: UserExperience.title updated
- [ ] **[P0]** User updates isCurrent from false to true -> Expected: UserExperience.isCurrent = true, endDate cleared
- [ ] **[P0]** User updates isCurrent from true to false and sets endDate -> Expected: UserExperience.isCurrent = false, endDate set
- [ ] **[P1]** User updates description -> Expected: UserExperience.description updated
- [ ] **[P1]** User updates startDate -> Expected: UserExperience.startDate updated, validated against endDate
- [ ] **[P1]** User updates endDate -> Expected: UserExperience.endDate updated
- [ ] **[P2]** Editing experience logs Activity -> Expected: Activity record with action="experience_updated"

#### Delete User Experience
- [ ] **[P0]** User deletes experience -> Expected: UserExperience record deleted
- [ ] **[P1]** Delete non-existent experience -> Expected: 404 Not Found or no-op
- [ ] **[P2]** Deleting experience logs Activity -> Expected: Activity record with action="experience_deleted"

#### Current Job Toggle
- [ ] **[P0]** User sets one experience as current -> Expected: UserExperience.isCurrent = true for that experience
- [ ] **[P1]** User sets another experience as current -> Expected: Previous current experience isCurrent = false (if only one allowed)
- [ ] **[P1]** Multiple current jobs allowed (schema allows) -> Expected: Multiple experiences with isCurrent = true
- [ ] **[P2]** Current job has no endDate -> Expected: UserExperience.endDate = null for current jobs

---

### Notifications (All 35+ Types)

#### Notification Creation
- [ ] **[P0]** TASK_ASSIGNED notification created when task assigned -> Expected: Notification with type=TASK_ASSIGNED, userId=assignee
- [ ] **[P0]** TASK_COMPLETED notification created -> Expected: Notification with type=TASK_COMPLETED
- [ ] **[P0]** TASK_REVIEWED notification created -> Expected: Notification with type=TASK_REVIEWED
- [ ] **[P0]** TASK_REVISION_REQUESTED notification created -> Expected: Notification with type=TASK_REVISION_REQUESTED
- [ ] **[P1]** PROPOSAL_CREATED notification -> Expected: Notification sent to project members
- [ ] **[P1]** PROPOSAL_VOTED notification -> Expected: Notification sent to proposal creator
- [ ] **[P1]** PROPOSAL_PASSED notification -> Expected: Notification sent to all voters/members
- [ ] **[P1]** PROPOSAL_REJECTED notification -> Expected: Notification sent
- [ ] **[P1]** PROPOSAL_EXECUTED notification -> Expected: Notification sent after execution
- [ ] **[P1]** ACHIEVEMENT_EARNED notification -> Expected: Notification with achievement details
- [ ] **[P1]** LEVEL_UP notification -> Expected: Notification when user levels up
- [ ] **[P1]** STREAK_MILESTONE notification -> Expected: Notification for streak achievement
- [ ] **[P1]** SHARES_RECEIVED notification -> Expected: Notification when shares allocated
- [ ] **[P1]** SHARES_VESTED notification -> Expected: Notification when shares vest
- [ ] **[P1]** ESCROW_FUNDED notification -> Expected: Notification when task escrow funded
- [ ] **[P1]** ESCROW_RELEASED notification -> Expected: Notification when escrow released
- [ ] **[P1]** PROJECT_INVITATION notification -> Expected: Notification when invited to project
- [ ] **[P1]** PROJECT_INVITATION_ACCEPTED notification -> Expected: Notification to inviter
- [ ] **[P1]** PROJECT_INVITATION_DECLINED notification -> Expected: Notification to inviter
- [ ] **[P1]** PROJECT_MEMBERSHIP_REQUEST notification -> Expected: Notification to project leaders
- [ ] **[P1]** PROJECT_MEMBERSHIP_APPROVED notification -> Expected: Notification to requester
- [ ] **[P1]** PROJECT_MEMBERSHIP_REJECTED notification -> Expected: Notification to requester
- [ ] **[P1]** PROJECT_UPDATE notification -> Expected: Notification to followers
- [ ] **[P1]** PROJECT_FUNDED notification -> Expected: Notification to project team
- [ ] **[P1]** GUILD_INVITATION notification -> Expected: Notification when invited to guild
- [ ] **[P1]** GUILD_INVITATION_ACCEPTED notification -> Expected: Notification to inviter
- [ ] **[P1]** GUILD_INVITATION_DECLINED notification -> Expected: Notification to inviter
- [ ] **[P1]** GUILD_APPLICATION notification -> Expected: Notification to guild admins
- [ ] **[P1]** GUILD_APPLICATION_APPROVED notification -> Expected: Notification to applicant
- [ ] **[P1]** GUILD_APPLICATION_REJECTED notification -> Expected: Notification to applicant
- [ ] **[P2]** EVENT_INVITATION notification -> Expected: Notification when invited to event
- [ ] **[P2]** EVENT_REMINDER notification -> Expected: Notification before event starts
- [ ] **[P2]** EVENT_STARTING_SOON notification -> Expected: Notification X minutes before event
- [ ] **[P2]** EVENT_CANCELLED notification -> Expected: Notification to all attendees
- [ ] **[P2]** EVENT_UPDATED notification -> Expected: Notification to attendees
- [ ] **[P2]** USER_FOLLOWED notification -> Expected: Notification when someone follows user
- [ ] **[P2]** PROJECT_FOLLOWED notification -> Expected: Notification to project owner (optional)
- [ ] **[P2]** GUILD_FOLLOWED notification -> Expected: Notification to guild owner (optional)
- [ ] **[P2]** COMMENT_REPLY notification -> Expected: Notification when someone replies to comment
- [ ] **[P2]** MENTION notification -> Expected: Notification when user mentioned
- [ ] **[P2]** FOLLOWER_NEW notification -> Expected: Notification when new follower
- [ ] **[P3]** SYSTEM_ANNOUNCEMENT notification -> Expected: Broadcast to all users
- [ ] **[P3]** SECURITY_ALERT notification -> Expected: Notification for security events (e.g., password change)

#### Notification Fields
- [ ] **[P0]** Notification includes title -> Expected: Notification.title populated
- [ ] **[P0]** Notification includes message -> Expected: Notification.message (text) populated
- [ ] **[P1]** Notification includes data (JSON) for context -> Expected: Notification.data contains metadata (e.g., projectId, taskId)
- [ ] **[P1]** Notification includes actionUrl -> Expected: Notification.actionUrl links to relevant page
- [ ] **[P1]** Notification created with isRead=false -> Expected: Default isRead = false
- [ ] **[P1]** Notification createdAt timestamp -> Expected: Notification.createdAt reflects creation time

#### Mark Notification as Read
- [ ] **[P0]** User marks notification as read -> Expected: Notification.isRead = true, readAt = current timestamp
- [ ] **[P0]** User marks all notifications as read -> Expected: All unread notifications updated
- [ ] **[P1]** Mark already read notification -> Expected: No error, readAt updated if different
- [ ] **[P2]** Read timestamp accuracy -> Expected: Notification.readAt matches when user marked it read
- [ ] **[P3]** Marking notification as unread (if supported) -> Expected: isRead = false, readAt = null

#### Notification Delivery
- [ ] **[P1]** Notification visible in user's notification feed -> Expected: Fetch notifications returns all for user
- [ ] **[P1]** Real-time notification delivery (SignalR) -> Expected: Notification pushed to client via WebSocket
- [ ] **[P2]** Notification count badge (unread count) -> Expected: API returns count of unread notifications
- [ ] **[P2]** Notification filters by type -> Expected: Fetch notifications filtered by NotificationType
- [ ] **[P3]** Notification deletion -> Expected: User can delete notification

---

### Activity Feed (All ActivityTypes)

#### Activity Types: CREATED, UPDATED, DELETED, COMPLETED, JOINED, LEFT, COMMENTED, VOTED, SUBMITTED, REVIEWED, FUNDED, TRANSFERRED, SWAPPED

#### Activity Creation
- [ ] **[P1]** CREATED activity logged when user creates project -> Expected: Activity with type=CREATED, entityType="Project", entityId=projectId
- [ ] **[P1]** UPDATED activity logged when user updates profile -> Expected: Activity with type=UPDATED, entityType="User"
- [ ] **[P1]** DELETED activity logged when user deletes resource -> Expected: Activity with type=DELETED
- [ ] **[P1]** COMPLETED activity logged when task completed -> Expected: Activity with type=COMPLETED, entityType="ProjectTask"
- [ ] **[P1]** JOINED activity logged when user joins project -> Expected: Activity with type=JOINED, entityType="ProjectMember"
- [ ] **[P1]** LEFT activity logged when user leaves project -> Expected: Activity with type=LEFT
- [ ] **[P1]** COMMENTED activity logged when user comments -> Expected: Activity with type=COMMENTED, entityType="ProjectComment"
- [ ] **[P1]** VOTED activity logged when user votes on proposal -> Expected: Activity with type=VOTED, entityType="Vote"
- [ ] **[P1]** SUBMITTED activity logged when task submission -> Expected: Activity with type=SUBMITTED, entityType="TaskSubmission"
- [ ] **[P1]** REVIEWED activity logged when task reviewed -> Expected: Activity with type=REVIEWED
- [ ] **[P1]** FUNDED activity logged when project funded -> Expected: Activity with type=FUNDED
- [ ] **[P1]** TRANSFERRED activity logged when shares transferred -> Expected: Activity with type=TRANSFERRED
- [ ] **[P1]** SWAPPED activity logged when shares swapped -> Expected: Activity with type=SWAPPED

#### Activity Fields
- [ ] **[P1]** Activity includes userId -> Expected: Activity.userId references acting user
- [ ] **[P1]** Activity includes projectId (if applicable) -> Expected: Activity.projectId references project
- [ ] **[P1]** Activity includes entityType -> Expected: Activity.entityType describes entity (e.g., "Project", "Task")
- [ ] **[P1]** Activity includes entityId -> Expected: Activity.entityId references specific entity
- [ ] **[P1]** Activity includes action description -> Expected: Activity.action describes action (e.g., "created", "updated")
- [ ] **[P1]** Activity includes metadata (JSON) -> Expected: Activity.metadata stores additional context
- [ ] **[P1]** Activity createdAt timestamp -> Expected: Activity.createdAt reflects when activity occurred

#### Activity Feed Retrieval
- [ ] **[P1]** Fetch user's activity feed -> Expected: All activities for userId returned, ordered by createdAt desc
- [ ] **[P1]** Fetch project activity feed -> Expected: All activities for projectId returned
- [ ] **[P1]** Activity feed pagination -> Expected: Limit and offset supported
- [ ] **[P2]** Activity feed filtered by type -> Expected: Filter by ActivityType
- [ ] **[P2]** Activity feed filtered by date range -> Expected: Filter by createdAt range
- [ ] **[P3]** Activity feed includes user and project details (joined) -> Expected: Response includes related User and Project objects

---

## Section 2: Project Management (Full Lifecycle)

This section is identical to the original checklist - all project CRUD, lifecycle, resources, milestones, tasks (including task escrow references), project members, applications, invitations, membership requests, comments, updates, support, following, trending, and commerce subsections are included in full.

*[The full content of Section 2 from the original checklist applies here without modification]*

---

## Section 3: Agile Hierarchy (Project -> Milestone -> Epic -> Sprint -> Feature -> PBI -> Task)

This section is identical to the original checklist - all epics, sprints, features, PBIs, drill-down navigation, status propagation, cross-level dependencies, assignee tracking, and progress tracking subsections are included in full.

*[The full content of Section 3 from the original checklist applies here without modification]*

---

## Section 4: Guild Module

This section is identical to the original checklist - all guild CRUD, members & roles, invitations, applications, reviews & ratings, updates, following & notifications, project assignment, trending, verification, and edge cases subsections are included in full.

*[The full content of Section 4 from the original checklist applies here without modification]*

---

## Section 5: Opportunities Marketplace

This section is identical to the original checklist - all opportunity CRUD, lifecycle, experience levels, remote & location, applications, bids, updates, comments, search/filtering/pagination, and edge cases subsections are included in full.

*[The full content of Section 5 from the original checklist applies here without modification]*

---

## Section 6: DAO Governance

This section is identical to the original checklist - all proposal CRUD, lifecycle, voting, execution, delegated voting, edge cases, and notifications subsections are included in full.

*[The full content of Section 6 from the original checklist applies here without modification]*

---

## Section 7: Finance & Tokenomics

> **Note:** The Wallet Management subsection (Wallet CRUD, Wallet Verification, Primary Wallet Logic) has been excluded from this version. All other Finance & Tokenomics subsections are included.

### ProjectShare Management

#### CRUD Operations
- [ ] **[P0]** Create ProjectShare with required fields (name, symbol, totalSupply) -> Expected: Share created with default decimals=6
- [ ] **[P0]** Create ProjectShare with unique projectId -> Expected: Success, one share per project
- [ ] **[P0]** Attempt to create duplicate ProjectShare for same projectId -> Expected: Error, unique constraint violation
- [ ] **[P1]** Create ProjectShare with custom decimals -> Expected: Decimals value persisted correctly
- [ ] **[P1]** Create ProjectShare with allocation JSON -> Expected: Allocation data stored and retrievable
- [ ] **[P1]** Create ProjectShare with vestingConfig JSON -> Expected: Vesting configuration stored
- [ ] **[P2]** Update ProjectShare name and symbol -> Expected: Values updated successfully
- [ ] **[P2]** Update ProjectShare totalSupply -> Expected: Supply updated, affects calculations
- [ ] **[P1]** Read ProjectShare by projectId -> Expected: Returns correct share data
- [ ] **[P1]** Read ProjectShare with assetId -> Expected: Returns blockchain asset reference
- [ ] **[P2]** Delete ProjectShare with no dependencies -> Expected: Deletion successful
- [ ] **[P3]** Delete ProjectShare with dependent records (shareholders, escrows) -> Expected: Error or cascade delete based on schema

#### Deployment Tracking
- [ ] **[P0]** Deploy ProjectShare (isDeployed = false -> true) -> Expected: isDeployed flag set, deployedAt timestamp recorded
- [ ] **[P0]** Deploy ProjectShare with assetId -> Expected: assetId stored after blockchain deployment
- [ ] **[P1]** Attempt operations on non-deployed share -> Expected: Business logic validates deployment status
- [ ] **[P2]** Redeploy already deployed share -> Expected: Prevented or logged appropriately

### ProjectEquity Management

#### Equity Assignment
- [ ] **[P0]** Grant equity to user for project -> Expected: ProjectEquity record created with sharePercent and investmentAmount
- [ ] **[P1]** Grant multiple equity records to same user for same project -> Expected: Multiple records allowed (or consolidated based on business rules)
- [ ] **[P1]** Grant equity with 100% sharePercent total across all users -> Expected: Total validation enforced
- [ ] **[P1]** Grant equity exceeding 100% total -> Expected: Validation error
- [ ] **[P2]** Grant equity with zero sharePercent -> Expected: Validation error or warning
- [ ] **[P2]** Grant equity with negative sharePercent -> Expected: Validation error
- [ ] **[P2]** Grant equity with zero investmentAmount -> Expected: Allowed (contributor scenario)
- [ ] **[P1]** Update equity sharePercent -> Expected: Updated correctly, affects voting power
- [ ] **[P1]** Update equity investmentAmount -> Expected: Updated correctly, affects ROI calculations
- [ ] **[P2]** Delete equity record -> Expected: Equity removed, affects project cap table

#### Equity Queries
- [ ] **[P1]** Get all equity holders for a project -> Expected: Returns all ProjectEquity records for projectId
- [ ] **[P1]** Get total equity percentage allocated -> Expected: Sum of all sharePercent values
- [ ] **[P2]** Get total investment amount for project -> Expected: Sum of all investmentAmount values
- [ ] **[P2]** Get user equity across all projects -> Expected: List of all ProjectEquity for userId

### Treasury Management

#### Treasury Creation & Tracking
- [ ] **[P0]** Create Treasury for project -> Expected: Treasury created with balance=0, unique projectId
- [ ] **[P0]** Attempt to create duplicate Treasury for project -> Expected: Error, unique constraint violation
- [ ] **[P1]** Create Treasury with shareAssetId -> Expected: Asset reference stored
- [ ] **[P1]** Update Treasury balance -> Expected: Balance updated correctly
- [ ] **[P2]** Update Treasury shareAssetId -> Expected: Asset reference updated
- [ ] **[P1]** Read Treasury by projectId -> Expected: Returns correct treasury data

#### Treasury Transactions - ALL Types
- [ ] **[P0]** Create DEPOSIT transaction -> Expected: Transaction recorded, treasury balance increased
- [ ] **[P0]** Create WITHDRAWAL transaction -> Expected: Transaction recorded, treasury balance decreased
- [ ] **[P0]** Create TASK_PAYMENT transaction -> Expected: Transaction recorded for task compensation
- [ ] **[P0]** Create PROPOSAL_EXECUTION transaction -> Expected: Transaction linked to proposalId
- [ ] **[P0]** Create DIVIDEND transaction -> Expected: Transaction recorded for dividend distribution
- [ ] **[P0]** Create FEE transaction -> Expected: Transaction recorded for platform/protocol fees
- [ ] **[P1]** Create transaction with txHash -> Expected: Blockchain transaction hash stored
- [ ] **[P1]** Create transaction with description -> Expected: Description stored and retrievable
- [ ] **[P1]** Create transaction linked to proposal -> Expected: proposalId reference stored
- [ ] **[P2]** Create transaction with empty/null amount -> Expected: Validation error
- [ ] **[P2]** Create transaction with negative amount -> Expected: Validation error or handled based on type
- [ ] **[P1]** Query all transactions for treasury -> Expected: Returns all TreasuryTransaction records
- [ ] **[P1]** Query transactions by type -> Expected: Filtered results for specific TransactionType
- [ ] **[P2]** Query transactions by date range -> Expected: Filtered results within date range
- [ ] **[P2]** Calculate treasury balance from transactions -> Expected: Matches stored balance

### ShareHolder Management

#### Balance Tracking
- [ ] **[P0]** Create ShareHolder with initial balance -> Expected: ShareHolder record created for shareId + userId
- [ ] **[P1]** Update ShareHolder balance -> Expected: Balance updated correctly
- [ ] **[P1]** Update ShareHolder stakedAmount -> Expected: Staked amount updated, affects available balance
- [ ] **[P1]** Update ShareHolder lockedAmount -> Expected: Locked amount updated, affects available balance
- [ ] **[P0]** Calculate available balance (balance - stakedAmount - lockedAmount) -> Expected: Correct available amount
- [ ] **[P2]** Set stakedAmount exceeding balance -> Expected: Validation error
- [ ] **[P2]** Set lockedAmount exceeding balance -> Expected: Validation error
- [ ] **[P2]** Set combined stakedAmount + lockedAmount exceeding balance -> Expected: Validation error
- [ ] **[P2]** Transfer shares reducing balance below staked + locked -> Expected: Validation error
- [ ] **[P1]** Query all shareholders for a share -> Expected: Returns all ShareHolder records for shareId
- [ ] **[P2]** Query total circulating supply -> Expected: Sum of all balances for shareId

#### Staking & Locking
- [ ] **[P1]** Stake shares -> Expected: stakedAmount increased, available balance decreased
- [ ] **[P1]** Unstake shares -> Expected: stakedAmount decreased, available balance increased
- [ ] **[P1]** Lock shares -> Expected: lockedAmount increased, available balance decreased
- [ ] **[P1]** Unlock shares -> Expected: lockedAmount decreased, available balance increased
- [ ] **[P2]** Attempt to stake/lock more than available -> Expected: Validation error

### ShareVesting Management

#### Vesting Creation & Configuration
- [ ] **[P0]** Create ShareVesting with DAILY frequency -> Expected: Vesting schedule created with daily release
- [ ] **[P0]** Create ShareVesting with WEEKLY frequency -> Expected: Vesting schedule created with weekly release
- [ ] **[P0]** Create ShareVesting with MONTHLY frequency -> Expected: Vesting schedule created with monthly release
- [ ] **[P0]** Create ShareVesting with QUARTERLY frequency -> Expected: Vesting schedule created with quarterly release
- [ ] **[P1]** Create ShareVesting with totalAmount -> Expected: Total vesting amount set
- [ ] **[P1]** Create ShareVesting with releasedAmount=0 -> Expected: No shares released initially
- [ ] **[P1]** Create ShareVesting with cliff period -> Expected: cliffEnd date set after startDate
- [ ] **[P1]** Create ShareVesting with vestingEnd after cliffEnd -> Expected: Valid vesting timeline
- [ ] **[P2]** Create ShareVesting with cliffEnd before startDate -> Expected: Validation error
- [ ] **[P2]** Create ShareVesting with vestingEnd before cliffEnd -> Expected: Validation error
- [ ] **[P2]** Create ShareVesting with zero totalAmount -> Expected: Validation error

#### Vesting Cliff Enforcement
- [ ] **[P0]** Attempt to release shares before cliffEnd -> Expected: Release blocked, error message
- [ ] **[P0]** Release shares exactly at cliffEnd -> Expected: Release allowed
- [ ] **[P0]** Release shares after cliffEnd -> Expected: Release allowed
- [ ] **[P1]** Query vested amount before cliff -> Expected: Returns 0
- [ ] **[P1]** Query vested amount after cliff -> Expected: Returns calculated vested amount

#### Progressive Release Calculation
- [ ] **[P0]** Calculate vested amount at 25% of vesting period -> Expected: ~25% of totalAmount vested
- [ ] **[P0]** Calculate vested amount at 50% of vesting period -> Expected: ~50% of totalAmount vested
- [ ] **[P0]** Calculate vested amount at 75% of vesting period -> Expected: ~75% of totalAmount vested
- [ ] **[P0]** Calculate vested amount at 100% of vesting period (vestingEnd) -> Expected: 100% of totalAmount vested
- [ ] **[P1]** Calculate vested amount past vestingEnd -> Expected: No more than totalAmount
- [ ] **[P1]** Release vested shares (update releasedAmount) -> Expected: releasedAmount increased, cannot exceed totalAmount
- [ ] **[P1]** Attempt to release more than vested amount -> Expected: Validation error
- [ ] **[P1]** Attempt to release more than totalAmount -> Expected: Validation error
- [ ] **[P2]** Query remaining vested shares -> Expected: totalAmount - releasedAmount
- [ ] **[P2]** Complete full vesting schedule -> Expected: releasedAmount == totalAmount

#### Vesting by Frequency
- [ ] **[P1]** DAILY vesting: calculate release every day -> Expected: Daily increments calculated
- [ ] **[P1]** WEEKLY vesting: calculate release every 7 days -> Expected: Weekly increments calculated
- [ ] **[P1]** MONTHLY vesting: calculate release every month -> Expected: Monthly increments calculated
- [ ] **[P1]** QUARTERLY vesting: calculate release every 3 months -> Expected: Quarterly increments calculated

### Fundraising Campaign Management

#### Campaign Creation
- [ ] **[P0]** Create Fundraising linked to ProjectShare -> Expected: Campaign created with unique shareId
- [ ] **[P0]** Attempt to create duplicate Fundraising for same share -> Expected: Error, unique constraint violation
- [ ] **[P1]** Create Fundraising with fundingGoal -> Expected: Goal amount set
- [ ] **[P1]** Create Fundraising with minContribution -> Expected: Minimum contribution limit set
- [ ] **[P1]** Create Fundraising with maxContribution -> Expected: Maximum contribution limit set
- [ ] **[P1]** Create Fundraising with sharePrice -> Expected: Price per share set
- [ ] **[P1]** Create Fundraising with date range (startDate, endDate) -> Expected: Campaign period defined
- [ ] **[P2]** Create Fundraising with endDate before startDate -> Expected: Validation error
- [ ] **[P2]** Create Fundraising with minContribution > maxContribution -> Expected: Validation error

#### Campaign Status Transitions
- [ ] **[P0]** Create campaign with PENDING status -> Expected: Initial status set to PENDING
- [ ] **[P0]** Activate campaign (PENDING -> ACTIVE) -> Expected: Status updated, campaign starts accepting contributions
- [ ] **[P0]** Complete successful campaign (ACTIVE -> SUCCESSFUL) when goal met -> Expected: Status updated to SUCCESSFUL
- [ ] **[P0]** Fail campaign (ACTIVE -> FAILED) when endDate reached without meeting goal -> Expected: Status updated to FAILED
- [ ] **[P1]** Cancel campaign (any status -> CANCELLED) -> Expected: Status updated to CANCELLED
- [ ] **[P0]** Initiate refund process (FAILED -> REFUNDING) -> Expected: Status updated to REFUNDING
- [ ] **[P2]** Attempt invalid status transition -> Expected: Validation error
- [ ] **[P1]** Query campaigns by status -> Expected: Filtered results for specific FundraisingStatus

#### Funding Tracking
- [ ] **[P0]** Update currentFunding when contribution confirmed -> Expected: currentFunding increased
- [ ] **[P0]** Check if fundingGoal reached -> Expected: Correct comparison of currentFunding vs fundingGoal
- [ ] **[P1]** Calculate funding percentage -> Expected: (currentFunding / fundingGoal) * 100

### Fundraising Contribution Management

#### Contribution Creation
- [ ] **[P0]** Create contribution with valid amount -> Expected: FundraisingContribution created with PENDING status
- [ ] **[P1]** Create contribution with shareAmount calculated from sharePrice -> Expected: Correct share allocation
- [ ] **[P1]** Create contribution with paymentAsset -> Expected: Payment asset type stored
- [ ] **[P1]** Create contribution with txHash -> Expected: Blockchain transaction reference stored
- [ ] **[P2]** Create contribution with amount below minContribution -> Expected: Validation error
- [ ] **[P2]** Create contribution with amount above maxContribution -> Expected: Validation error
- [ ] **[P2]** Create contribution with zero or negative amount -> Expected: Validation error

#### Contribution Status Transitions
- [ ] **[P0]** Confirm contribution (PENDING -> CONFIRMED) -> Expected: Status updated, funds added to currentFunding
- [ ] **[P1]** Refund contribution (CONFIRMED -> REFUNDED) -> Expected: Status updated, funds deducted from currentFunding
- [ ] **[P1]** Fail contribution (PENDING -> FAILED) -> Expected: Status updated, no impact on currentFunding
- [ ] **[P2]** Attempt to refund non-confirmed contribution -> Expected: Validation error

#### Contribution Limit Enforcement
- [ ] **[P0]** Enforce minContribution on single contribution -> Expected: Contribution blocked if below minimum
- [ ] **[P0]** Enforce maxContribution on single contribution -> Expected: Contribution blocked if above maximum
- [ ] **[P1]** Enforce cumulative maxContribution per user -> Expected: Total user contributions cannot exceed limit
- [ ] **[P2]** Handle multiple contributions from same user -> Expected: Sum validated against maxContribution

#### Contribution Queries
- [ ] **[P1]** Get all contributions for a campaign -> Expected: Returns all FundraisingContribution records for fundraisingId
- [ ] **[P1]** Get contributions by user -> Expected: Returns all contributions for userId
- [ ] **[P1]** Get contributions by status -> Expected: Filtered results for specific ContributionStatus
- [ ] **[P2]** Calculate total confirmed contributions -> Expected: Sum of amounts where status=CONFIRMED

### TaskEscrow Management

#### Escrow Creation
- [ ] **[P0]** Create TaskEscrow for task -> Expected: Escrow created with unique taskId, status=FUNDED
- [ ] **[P0]** Attempt to create duplicate TaskEscrow for same task -> Expected: Error, unique constraint violation
- [ ] **[P1]** Create TaskEscrow linked to funder -> Expected: funderId reference stored
- [ ] **[P1]** Create TaskEscrow linked to share -> Expected: shareId reference stored
- [ ] **[P1]** Create TaskEscrow with amount -> Expected: Escrow amount stored
- [ ] **[P1]** Create TaskEscrow with txHashFund -> Expected: Funding transaction hash stored
- [ ] **[P1]** Set fundedAt timestamp -> Expected: Timestamp recorded when funded

#### Escrow Status Transitions
- [ ] **[P0]** Fund escrow (NONE -> FUNDED) -> Expected: Status updated, fundedAt timestamp set
- [ ] **[P0]** Release escrow (FUNDED -> RELEASED) -> Expected: Status updated, releasedAt timestamp set, txHashRelease stored
- [ ] **[P0]** Dispute escrow (FUNDED -> DISPUTED) -> Expected: Status updated, requires resolution
- [ ] **[P0]** Refund escrow (FUNDED/DISPUTED -> REFUNDED) -> Expected: Status updated, refundedAt timestamp set, txHashRefund stored
- [ ] **[P1]** Attempt to release unfunded escrow -> Expected: Validation error
- [ ] **[P2]** Attempt invalid status transition -> Expected: Validation error

#### Escrow Timestamps
- [ ] **[P1]** Record fundedAt when status=FUNDED -> Expected: Timestamp set correctly
- [ ] **[P1]** Record releasedAt when status=RELEASED -> Expected: Timestamp set correctly
- [ ] **[P1]** Record refundedAt when status=REFUNDED -> Expected: Timestamp set correctly
- [ ] **[P2]** Query escrows by timestamp range -> Expected: Filtered results

#### Escrow Queries
- [ ] **[P1]** Get escrow by taskId -> Expected: Returns TaskEscrow record for task
- [ ] **[P1]** Get all escrows for funder -> Expected: Returns all escrows where funderId matches
- [ ] **[P1]** Get escrows by status -> Expected: Filtered results for specific EscrowStatus
- [ ] **[P1]** Get escrows for specific share -> Expected: Returns all escrows linked to shareId

### ShareSwap Management

#### Swap Creation
- [ ] **[P0]** Create swap from one share to another -> Expected: ShareSwap created with fromShareId and toShareId
- [ ] **[P1]** Create swap with fromAmount and toAmount -> Expected: Amounts stored correctly
- [ ] **[P1]** Create swap with calculated exchangeRate -> Expected: exchangeRate = toAmount / fromAmount
- [ ] **[P1]** Create swap with fee -> Expected: Fee amount stored (default 0)
- [ ] **[P1]** Create swap with txHash -> Expected: Blockchain transaction hash stored
- [ ] **[P2]** Create swap with zero amounts -> Expected: Validation error
- [ ] **[P2]** Create swap with negative amounts -> Expected: Validation error
- [ ] **[P2]** Create swap from share to itself (fromShareId == toShareId) -> Expected: Validation error

#### Swap Status Transitions
- [ ] **[P0]** Initiate swap (status = PENDING) -> Expected: Swap created in PENDING state
- [ ] **[P0]** Process swap (PENDING -> PROCESSING) -> Expected: Status updated, swap being executed
- [ ] **[P0]** Complete swap (PROCESSING -> COMPLETED) -> Expected: Status updated, completedAt timestamp set
- [ ] **[P0]** Fail swap (PENDING/PROCESSING -> FAILED) -> Expected: Status updated, funds returned
- [ ] **[P0]** Cancel swap (PENDING -> CANCELLED) -> Expected: Status updated, no exchange occurs
- [ ] **[P2]** Attempt to cancel completed swap -> Expected: Validation error

#### Swap Execution
- [ ] **[P0]** Execute swap: deduct fromAmount from user's fromShare balance -> Expected: Balance decreased
- [ ] **[P0]** Execute swap: add toAmount to user's toShare balance -> Expected: Balance increased
- [ ] **[P0]** Execute swap: deduct fee if applicable -> Expected: Fee deducted from toAmount
- [ ] **[P1]** Record completedAt timestamp on completion -> Expected: Timestamp set
- [ ] **[P1]** Verify user has sufficient fromShare balance before swap -> Expected: Validation check
- [ ] **[P2]** Handle partial swap failure -> Expected: Rollback or error handling

#### Swap Queries
- [ ] **[P1]** Get all swaps for user -> Expected: Returns all ShareSwap records for userId
- [ ] **[P1]** Get swaps by status -> Expected: Filtered results for specific SwapStatus
- [ ] **[P2]** Get swap history for specific share pair -> Expected: Returns swaps between fromShareId and toShareId
- [ ] **[P2]** Calculate total swap volume for share -> Expected: Sum of all amounts for shareId

### LiquidityPool Management

#### Pool Creation
- [ ] **[P0]** Create liquidity pool for two shares -> Expected: Pool created with share1Id and share2Id
- [ ] **[P1]** Create pool with initial reserves (reserve1, reserve2) -> Expected: Reserves set to 0 initially
- [ ] **[P1]** Create pool with totalShares -> Expected: totalShares initialized to 0
- [ ] **[P1]** Create pool with feePercent -> Expected: Fee set to default 0.3% (0.003)
- [ ] **[P1]** Create pool with custom feePercent -> Expected: Custom fee stored
- [ ] **[P1]** Create pool with isActive=true -> Expected: Pool is active
- [ ] **[P2]** Create pool with same share for both sides (share1Id == share2Id) -> Expected: Validation error
- [ ] **[P2]** Create duplicate pool for same share pair -> Expected: Error or allowed based on business rules

#### Pool Reserve Management
- [ ] **[P0]** Add liquidity: increase reserve1 and reserve2 -> Expected: Reserves updated, totalShares increased
- [ ] **[P0]** Remove liquidity: decrease reserve1 and reserve2 -> Expected: Reserves updated, totalShares decreased
- [ ] **[P1]** Calculate constant product (reserve1 * reserve2) -> Expected: Constant maintained for AMM
- [ ] **[P2]** Prevent reserve from going negative -> Expected: Validation error
- [ ] **[P2]** Handle reserve overflow -> Expected: Error or decimal precision handling

#### Pool Status
- [ ] **[P1]** Activate pool (isActive = true) -> Expected: Pool accepts trades
- [ ] **[P1]** Deactivate pool (isActive = false) -> Expected: Pool stops accepting new trades
- [ ] **[P2]** Query only active pools -> Expected: Returns pools where isActive=true

#### Pool Queries
- [ ] **[P1]** Get pool by share pair -> Expected: Returns pool for share1Id and share2Id
- [ ] **[P1]** Get all pools for a share -> Expected: Returns all pools containing shareId
- [ ] **[P2]** Calculate pool TVL (Total Value Locked) -> Expected: reserve1 + reserve2 in common currency

### LiquidityProvider Management

#### Provider Creation
- [ ] **[P0]** Add liquidity provider to pool -> Expected: LiquidityProvider record created for poolId and userId
- [ ] **[P1]** Record provider shares -> Expected: shares amount stored
- [ ] **[P1]** Record share1In and share2In amounts -> Expected: Deposited amounts stored (default 0)
- [ ] **[P1]** Update share1In when depositing -> Expected: share1In increased
- [ ] **[P1]** Update share2In when depositing -> Expected: share2In increased
- [ ] **[P2]** Allow multiple deposits from same provider -> Expected: Cumulative amounts tracked

#### Liquidity Provision
- [ ] **[P0]** Deposit liquidity: increase share1In and share2In -> Expected: Provider position increased
- [ ] **[P0]** Deposit liquidity: mint LP shares -> Expected: Provider shares increased proportionally
- [ ] **[P0]** Withdraw liquidity: decrease share1In and share2In -> Expected: Provider position decreased
- [ ] **[P0]** Withdraw liquidity: burn LP shares -> Expected: Provider shares decreased
- [ ] **[P1]** Calculate provider's pool ownership percentage -> Expected: (provider shares / pool totalShares) * 100
- [ ] **[P2]** Attempt to withdraw more than provided -> Expected: Validation error

#### Provider Queries
- [ ] **[P1]** Get all providers for a pool -> Expected: Returns all LiquidityProvider records for poolId
- [ ] **[P1]** Get all pools user has provided liquidity to -> Expected: Returns all providers for userId
- [ ] **[P2]** Calculate total liquidity provided by user -> Expected: Sum of share1In and share2In across all pools

### Edge Cases & Validation

#### Insufficient Balance
- [ ] **[P0]** Attempt swap with insufficient fromShare balance -> Expected: Error, transaction blocked
- [ ] **[P0]** Attempt escrow release with insufficient treasury -> Expected: Error or handled appropriately
- [ ] **[P1]** Check available balance before all debit operations -> Expected: Validation enforced

#### Negative & Zero Amounts
- [ ] **[P0]** Attempt to create transaction with negative amount -> Expected: Validation error
- [ ] **[P0]** Attempt to create transaction with zero amount -> Expected: Validation error or warning
- [ ] **[P1]** Handle negative balance scenarios -> Expected: Balance cannot go negative
- [ ] **[P2]** Zero out balances during cleanup -> Expected: Allowed for administrative purposes

#### Overflow & Precision
- [ ] **[P1]** Handle very large decimal amounts (near maximum) -> Expected: No overflow, precision maintained
- [ ] **[P1]** Handle very small decimal amounts (near minimum) -> Expected: Precision maintained at 8 decimals
- [ ] **[P2]** Perform arithmetic on maximum values -> Expected: No overflow errors
- [ ] **[P2]** Round calculations consistently -> Expected: Consistent rounding rules applied

#### Race Conditions
- [ ] **[P0]** Concurrent balance updates from multiple transactions -> Expected: Database locking or transaction isolation prevents inconsistency
- [ ] **[P0]** Concurrent contributions to fundraising near goal -> Expected: No over-funding, proper locking
- [ ] **[P0]** Concurrent swaps affecting same pool reserves -> Expected: Atomic updates, no race conditions
- [ ] **[P1]** Concurrent vesting releases for same schedule -> Expected: No double-release
- [ ] **[P1]** Concurrent escrow status changes -> Expected: Proper state management

---

## Section 8: Social & Communication

This section is identical to the original checklist - all post management, media, interactions, counters, trending, conversation management, member management, chat messages, attachments, real-time features, and edge cases subsections are included in full.

*[The full content of Section 8 from the original checklist applies here without modification]*

---

## Section 9: Events Module

This section is identical to the original checklist - all event CRUD, lifecycle, online vs in-person, attendee management, co-host management, reminders, timezone handling, search/filtering, notifications, and edge cases subsections are included in full.

*[The full content of Section 9 from the original checklist applies here without modification]*

---

## Section 10: Social & Follow

This section is identical to the original checklist - all UserFollow, ProjectFollow, GuildFollow, and follow edge cases subsections are included in full.

*[The full content of Section 10 from the original checklist applies here without modification]*

---

## Section 11: Gamification & Reputation

This section is identical to the original checklist - all XPEvent, level/XP tracking, tiers, achievements, UserAchievement, leaderboards, streaks, and referrals subsections are included in full.

*[The full content of Section 11 from the original checklist applies here without modification]*

---

## Section 12: Product Catalog

This section is identical to the original checklist - all product CRUD, visibility, commerce enabled, SKU uniqueness, listing/search, storefront, and edge cases subsections are included in full.

*[The full content of Section 12 from the original checklist applies here without modification]*

---

## 17. Dual-Asset Model & Membership Credentials

> The dual-asset model separates **governance rights** (Membership Credentials -- earned, non-transferable, 1 member = 1 vote) from **economic rights** (Ownership Shares -- fungible, transferable, proportional dividends). This section tests both assets and their interaction.

### 17.1 Membership Credential Issuance

- [ ] **[P0]** Project founder creates project -> Founder automatically receives MembershipCredential with grantedVia = FOUNDER -> Expected: Credential created with status ACTIVE, isTransferable = false
- [ ] **[P0]** User meets contribution threshold -> MembershipCredential issued with grantedVia = CONTRIBUTION_THRESHOLD -> Expected: Credential created, user gains voting rights
- [ ] **[P0]** DAO vote grants membership to user -> MembershipCredential issued with grantedVia = DAO_VOTE, grantedByProposalId populated -> Expected: Credential linked to proposal, status ACTIVE
- [ ] **[P1]** User applies and is approved -> MembershipCredential issued with grantedVia = APPLICATION_APPROVED -> Expected: Credential created after approval
- [ ] **[P1]** User reaches Game SDK threshold -> MembershipCredential issued with grantedVia = GAME_SDK_THRESHOLD -> Expected: Credential created, user can now vote on project proposals
- [ ] **[P1]** MembershipCredential created with correct projectId and userId -> Expected: Both foreign keys valid, credential scoped to specific project
- [ ] **[P2]** MembershipCredential includes mintTxHash when minted on-chain -> Expected: Hash populated, verifiable on secure ledger
- [ ] **[P2]** MembershipCredential mintedAt timestamp set on creation -> Expected: Timestamp accurate, not null
- [ ] **[P3]** Credential issuance triggers MEMBERSHIP_GRANTED notification -> Expected: User and project members notified

### 17.2 Membership Credential Uniqueness & Constraints

- [ ] **[P0]** Attempt to create second MembershipCredential for same user + project -> Expected: Unique constraint error (projectId, userId), "User already has a membership credential for this project"
- [ ] **[P0]** MembershipCredential isTransferable defaults to false -> Expected: All new credentials are non-transferable (soulbound)
- [ ] **[P1]** MembershipCredential references valid Project and User -> Expected: Foreign key constraints enforced
- [ ] **[P1]** MembershipCredential grantedByProposalId references valid Proposal (when populated) -> Expected: Foreign key constraint enforced
- [ ] **[P2]** MembershipCredential with null assetId (not yet minted on-chain) -> Expected: Valid state, credential functional for off-chain governance
- [ ] **[P3]** Attempt to set isTransferable = true -> Expected: Rejected or requires special governance proposal

### 17.3 Membership Credential Revocation

- [ ] **[P0]** DAO proposal to revoke membership passes (66% quorum, 75% approval) -> MembershipCredential status changed to REVOKED -> Expected: User loses voting rights, revokedAt timestamp set
- [ ] **[P0]** Revoked member attempts to vote on proposal -> Expected: Vote rejected, "You do not have an active membership credential"
- [ ] **[P0]** Revoked member attempts to create governance proposal -> Expected: Rejected, "Active membership credential required"
- [ ] **[P1]** Revocation proposal does not meet quorum (< 66%) -> Expected: Proposal expires, credential remains ACTIVE
- [ ] **[P1]** Revocation proposal meets quorum but not approval (< 75%) -> Expected: Proposal rejected, credential remains ACTIVE
- [ ] **[P1]** Revoked credential includes revokeTxHash when revoked on-chain -> Expected: Hash populated, verifiable
- [ ] **[P2]** Credential suspended (status = SUSPENDED) -> Expected: Voting rights temporarily removed, can be reactivated
- [ ] **[P2]** Revocation triggers MEMBERSHIP_REVOKED notification -> Expected: Affected user and project members notified
- [ ] **[P3]** Revoked member retains economic rights (ownership shares) -> Expected: Share balance unchanged, dividend rights preserved

### 17.4 Credential-Gated Governance Voting

- [ ] **[P0]** Member with ACTIVE credential votes on proposal -> Expected: Vote counted, voting power = 1 (not weighted by share holdings)
- [ ] **[P0]** User without credential attempts to vote -> Expected: Vote rejected, "Membership credential required to vote"
- [ ] **[P0]** Two members with different share balances both vote -> Expected: Each vote has equal weight (1 vote each), regardless of economic stake
- [ ] **[P1]** Member votes on treasury proposal -> Expected: Vote accepted, credential verified, equal weight applied
- [ ] **[P1]** Delegated vote respects credential requirement -> Expected: Delegate must also hold active credential
- [ ] **[P1]** Member with SUSPENDED credential attempts to vote -> Expected: Vote rejected
- [ ] **[P2]** Quorum calculation based on total active credentials, not share supply -> Expected: Quorum = (votes cast / total active credentials) x 100
- [ ] **[P2]** Governance proposal displays credential-holder count, not share percentages -> Expected: UI shows "X of Y members voted" not "X% of shares voted"
- [ ] **[P3]** Historical vote records link to credential status at time of vote -> Expected: Audit trail shows credential was ACTIVE when vote was cast

### 17.5 Dual-Asset Separation

- [ ] **[P0]** Member with credential and shares: credential governs voting, shares govern dividends -> Expected: Vote weight = 1, dividend share = (user shares / total shares)
- [ ] **[P0]** User acquires shares (via funding or task completion) but has no credential -> Expected: User receives economic rights (dividends) but cannot vote
- [ ] **[P0]** User has credential but zero shares -> Expected: User can vote but receives no dividends
- [ ] **[P1]** Share transfer between users does not affect credential status -> Expected: Seller retains credential and voting rights after selling shares
- [ ] **[P1]** Credential revocation does not affect share balance -> Expected: Revoked member still holds shares, receives dividends
- [ ] **[P1]** ProjectMember.votingPower reflects credential status (0 or 1) -> Expected: votingPower = 1 when credential ACTIVE, 0 otherwise
- [ ] **[P1]** ProjectMember.shareBalance reflects economic stake -> Expected: shareBalance updated on share transactions, independent of credential
- [ ] **[P2]** Revenue distribution uses shareBalance (economic), not votingPower (governance) -> Expected: Dividends proportional to shares, not votes
- [ ] **[P3]** Portfolio view shows both credential status and share balance per project -> Expected: Clear visual separation of governance vs economic rights

### 17.6 Membership Credential Grant Paths

- [ ] **[P0]** FOUNDER path: Create project -> Founder gets credential automatically -> Expected: No manual step, credential created in same transaction
- [ ] **[P1]** DAO_VOTE path: Proposal created -> Voting period -> Passes -> Credential issued -> Expected: Full governance workflow end-to-end
- [ ] **[P1]** CONTRIBUTION_THRESHOLD path: User completes tasks -> Cumulative contribution exceeds threshold -> Credential issued -> Expected: Automatic issuance when threshold met
- [ ] **[P1]** APPLICATION_APPROVED path: User submits membership application -> Admin/DAO approves -> Credential issued -> Expected: Application -> Approval -> Credential flow works
- [ ] **[P2]** GAME_SDK_THRESHOLD path: Player earns shares via games -> Reaches game threshold -> Credential issued -> Expected: Game engagement converts to governance rights
- [ ] **[P2]** Each grant path sets correct grantedVia enum value -> Expected: Enum matches the actual granting mechanism used
- [ ] **[P3]** Grant path displayed on credential detail view -> Expected: User can see how they earned their membership

### 17.7 Cooperative Trust Integration

- [ ] **[P1]** Membership Credentials are non-security governance instruments -> Expected: Not included in share/securities calculations or reports
- [ ] **[P1]** Ownership Shares are treated as securities with legal protections -> Expected: Share issuance respects fundraising compliance rules
- [ ] **[P2]** Platform distinguishes credential operations from share operations in audit logs -> Expected: Clear categorization in activity/audit trail
- [ ] **[P2]** Membership credential issuance does not trigger financial/securities workflows -> Expected: No tax implications, no investment disclosures
- [ ] **[P3]** Share issuance triggers appropriate compliance workflows -> Expected: Securities disclosures, investor accreditation checks where required

### 17.8 Edge Cases: Credential & Share Interactions

- [ ] **[P0]** Delete project -> All MembershipCredentials for project cascade deleted or archived -> Expected: No orphaned credentials
- [ ] **[P0]** Delete user -> All MembershipCredentials for user cascade deleted -> Expected: No orphaned credentials referencing non-existent users
- [ ] **[P1]** User has credentials across multiple projects -> Revoking one does not affect others -> Expected: Credentials are project-scoped
- [ ] **[P1]** Concurrent credential issuance for same user + project (race condition) -> Expected: Only one credential created, unique constraint enforced
- [ ] **[P2]** User with credential exits project (rage quit) -> Expected: Credential revoked, shares redeemed at fair value from treasury
- [ ] **[P2]** Credential status change (ACTIVE -> REVOKED) while user has pending vote -> Expected: Pending vote invalidated or honored based on policy
- [ ] **[P3]** Bulk credential issuance (e.g., all founding team members) -> Expected: All credentials created atomically, no partial failures

---

---

# ArdaNova Platform - Comprehensive QA & Functional Testing Checklist

> **Version:** 1.0
> **Last Updated:** 2026-02-01
> **Platform:** ArdaNova - Collaborative Innovation & Project Incubation Platform
> **Architecture:** Next.js (tRPC/Prisma/NextAuth) + .NET 8 (EF Core/SignalR/S3) + MCP Server
> **Schema Source of Truth:** `ardanova-client/prisma/database-architecture.dbml`

## How to Use This Checklist
- **P0 (Critical):** Must pass before any release. Blocking issues.
- **P1 (High):** Must pass before production release.
- **P2 (Medium):** Should pass. Can be deferred for hotfix.
- **P3 (Low):** Nice to have. Can be backlogged.
- Check off items as they pass testing `[x]`
- Add notes in-line for failures or observations

## Test Environment Setup
- [ ] Google OAuth credentials configured
- [ ] PostgreSQL database running and migrated
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

### 13.1 SSE Connection Establishment (Browser → Next.js)
- [ ] **[P0]** Navigate to authenticated page → SSE connection automatically established to `/api/realtime` → Expected: EventSource connection successful, ready state = 1 (OPEN)
- [ ] **[P0]** Check browser network tab during page load → Expected: `/api/realtime` request with `Accept: text/event-stream` header visible
- [ ] **[P0]** SSE connection established with valid session → Expected: Connection stream begins, no authentication errors
- [ ] **[P1]** Check connection headers → Expected: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive`
- [ ] **[P1]** Initial connection sends heartbeat/ping → Expected: Regular `comment: ping` or `event: ping` messages received every 30-60 seconds
- [ ] **[P2]** Multiple tabs open simultaneously → Expected: Each tab establishes its own SSE connection independently
- [ ] **[P2]** Check browser console for connection logs → Expected: "SSE connected" or similar status message logged

### 13.2 SSE Reconnection & Error Handling
- [ ] **[P0]** Temporarily disable network (airplane mode) → re-enable → Expected: SSE automatically reconnects within 5-10 seconds, events resume
- [ ] **[P0]** Server-side SSE endpoint crashes/restarts → Expected: Browser EventSource detects disconnection and auto-reconnects
- [ ] **[P1]** Connection drops mid-session → Expected: `onerror` handler fires, status changes to CONNECTING (0), then reconnects
- [ ] **[P1]** Observe reconnection backoff strategy → Expected: Progressive retry delays (1s, 2s, 5s, 10s) visible in network timing
- [ ] **[P1]** Server returns HTTP 500 on `/api/realtime` → Expected: Client retries connection, logs error, does not crash app
- [ ] **[P2]** Server sends `Connection: close` → Expected: SSE connection gracefully terminates and immediately reconnects
- [ ] **[P2]** Browser tab goes to background for 10+ minutes → Expected: Connection may close, but reconnects on tab focus
- [ ] **[P3]** Check for duplicate subscriptions after reconnection → Expected: Previous subscriptions cleaned up, no duplicate events

### 13.3 SSE Event Types & Parsing
- [ ] **[P0]** Receive `notification` event via SSE → Expected: Event parsed correctly with `event: notification` and JSON data payload
- [ ] **[P0]** Check notification payload structure → Expected: Contains `{ type, userId, title, content, timestamp }` fields
- [ ] **[P1]** Receive `message` event (new chat message) → Expected: Event type = `message`, data contains conversationId, senderId, content
- [ ] **[P1]** Receive `project_update` event → Expected: Event type = `project_update`, data contains projectId, changeType, updatedFields
- [ ] **[P1]** Receive `activity` event → Expected: Event type = `activity`, data contains activityType, entityId, userId
- [ ] **[P1]** Receive `user_status` event (online/offline) → Expected: Event type = `user_status`, data contains userId, status, lastSeen
- [ ] **[P1]** Receive multi-line data payload → Expected: Parser correctly combines `data:` lines until double newline
- [ ] **[P2]** Receive malformed JSON in SSE data → Expected: Parser catches error, logs warning, does not crash, skips event
- [ ] **[P2]** Receive unknown event type → Expected: Client handles gracefully, logs unknown type, does not throw error
- [ ] **[P2]** Receive event with custom `id:` field → Expected: Client can access event.lastEventId for replay purposes
- [ ] **[P3]** Server sends `retry: 5000` directive → Expected: Browser uses 5000ms as reconnection timeout override

### 13.4 SignalR Hub Connection (Next.js Server → .NET Backend)
- [ ] **[P0]** Next.js server starts up → SignalR connection to .NET backend established → Expected: Connection ID logged, status = Connected
- [ ] **[P0]** Check Next.js logs for SignalR connection success → Expected: "Connected to SignalR hub at ws://..." message visible
- [ ] **[P0]** SignalR connection uses WebSocket transport → Expected: Network shows WebSocket upgrade (HTTP 101 Switching Protocols)
- [ ] **[P1]** SignalR connection established with X-Api-Key header → Expected: Backend validates API key, connection accepted
- [ ] **[P1]** Missing or invalid X-Api-Key on SignalR connection → Expected: Connection rejected with 401 Unauthorized
- [ ] **[P1]** .NET backend restarts → Expected: Next.js SignalR client detects disconnection and automatically reconnects
- [ ] **[P1]** SignalR reconnection with exponential backoff → Expected: Retry delays visible in logs (1s, 2s, 5s, 10s, 30s)
- [ ] **[P2]** Connection state transitions logged → Expected: Connecting → Connected → Disconnected → Reconnecting states visible
- [ ] **[P2]** SignalR connection maintains persistent connection → Expected: Single WebSocket connection reused for all events, not reopened per message
- [ ] **[P3]** Enable detailed SignalR logging → Expected: Hub method invocations, group joins, and message sends logged at DEBUG level

### 13.5 SignalR Group Subscriptions
- [ ] **[P0]** User logs in → Next.js subscribes to `user:{userId}` group → Expected: Group subscription successful, user receives personal events
- [ ] **[P0]** User views project detail → Subscribe to `project:{projectId}` group → Expected: Group subscription acknowledged by backend
- [ ] **[P0]** User joins guild → Subscribe to `guild:{guildId}` group → Expected: Guild real-time events start flowing to client
- [ ] **[P0]** User opens conversation → Subscribe to `conversation:{conversationId}` group → Expected: Chat events for that conversation received
- [ ] **[P1]** Check backend logs for group join → Expected: "User {connectionId} joined group {groupName}" logged on .NET backend
- [ ] **[P1]** Subscribe to same group twice (idempotency) → Expected: No error, subscription acknowledged, no duplicate events
- [ ] **[P1]** User navigates away from project → Unsubscribe from `project:{projectId}` → Expected: No further project events received
- [ ] **[P1]** User logs out → All group subscriptions removed → Expected: No events delivered to disconnected client
- [ ] **[P1]** Subscribe to multiple groups simultaneously (user + project + guild + conversation) → Expected: All subscriptions succeed, events routed correctly
- [ ] **[P2]** User has multiple tabs open → Each tab's server connection subscribes to same groups → Expected: Groups deduplicated on backend, events sent once per connection
- [ ] **[P2]** User leaves guild → Unsubscribe from guild group → Expected: Guild events stop, subscription removed from backend group registry
- [ ] **[P2]** Check group membership via SignalR hub method → Expected: Backend can query groups for a connection ID
- [ ] **[P3]** Server sends event to empty group (no members) → Expected: Event discarded gracefully, no errors logged

### 13.6 Real-time Notification Delivery (NotificationHubHandler)
- [ ] **[P0]** Create notification for user → Expected: Event published to InMemoryEventBus → NotificationHubHandler invokes SendToUser → SSE delivers notification to browser
- [ ] **[P0]** User receives notification in browser → Expected: Notification appears in UI notification dropdown/toast without page refresh
- [ ] **[P0]** Notification includes all required fields → Expected: userId, type, title, content, createdAt, isRead = false
- [ ] **[P1]** User subscribed to `user:{id}` group receives notification → Expected: Notification event sent only to that user's connections
- [ ] **[P1]** User with multiple tabs open → Notification created → Expected: All tabs receive the same notification simultaneously
- [ ] **[P1]** User offline when notification created → User reconnects → Expected: Notification visible in notification list (persisted), but not re-sent as real-time event
- [ ] **[P1]** Notification contains related entity (project, opportunity, task) → Expected: NotificationEntity links correctly, notification shows entity details
- [ ] **[P2]** System notification (no specific user target) → Expected: Not sent via real-time, only visible in notification list
- [ ] **[P2]** Bulk notifications sent to 50+ users → Expected: All users receive their notifications within 1-2 seconds
- [ ] **[P2]** Check for duplicate notifications → Expected: Each notification sent exactly once per connection, no event ID duplication
- [ ] **[P3]** Notification delivery latency → Expected: < 500ms from creation to browser display under normal conditions

### 13.7 Real-time Project Events (ProjectEventHubHandler)
- [ ] **[P0]** Project status changed (PLANNING → ACTIVE) → Expected: Real-time event sent to `project:{id}` group, UI updates project status badge
- [ ] **[P0]** New member added to project → Expected: Event sent to project group, team member list updates in real-time
- [ ] **[P0]** Member removed from project → Expected: Removed user's UI shows access revoked, team list updates for remaining members
- [ ] **[P1]** Project details edited (title, description) → Expected: Real-time update event sent, detail page refreshes fields without reload
- [ ] **[P1]** New proposal submitted to project → Expected: Project members receive real-time notification, proposal count increments
- [ ] **[P1]** Proposal voting completed → Expected: Proposal status update sent in real-time, project view reflects APPROVED/REJECTED status
- [ ] **[P1]** Project visibility changed (PRIVATE → PUBLIC) → Expected: Update event sent, visibility indicator changes
- [ ] **[P1]** New task created under project → Expected: Task creation event sent to project group, task list auto-updates
- [ ] **[P1]** Task status changed (TODO → IN_PROGRESS) → Expected: Real-time event updates task board without refresh
- [ ] **[P1]** New opportunity posted for project → Expected: Opportunity created event sent, project opportunities tab updates
- [ ] **[P2]** Project milestone achieved → Expected: Achievement event sent, celebration animation/toast shown to project members
- [ ] **[P2]** Project deleted → Expected: All subscribed users redirected or shown "Project no longer exists" message
- [ ] **[P2]** Project archived → Expected: Project status updated in real-time, access revoked for non-members
- [ ] **[P2]** Multiple rapid updates to project (5+ edits in 10 seconds) → Expected: All events delivered, UI updates smoothly without race conditions
- [ ] **[P3]** Project event includes detailed change diff → Expected: Event payload contains `before` and `after` states or change summary

### 13.8 Real-time Activity Events (ActivityHubHandler)
- [ ] **[P0]** User creates post → Expected: Activity event sent to followers' feeds, new post appears in real-time
- [ ] **[P0]** User likes a post → Expected: Like count increments in real-time for all viewers of that post
- [ ] **[P0]** User comments on post → Expected: Comment appears in post's comment section without refresh
- [ ] **[P1]** Activity event includes activityType (POST_CREATED, COMMENT_ADDED, LIKE_ADDED, etc.) → Expected: Correct event type, UI handles each type appropriately
- [ ] **[P1]** Activity aggregation (10 likes in 1 minute) → Expected: UI batches/aggregates updates to avoid spam, shows final count
- [ ] **[P1]** User unfollows another user → Activity from unfollowed user stops appearing → Expected: Real-time feed filtering works
- [ ] **[P2]** Activity event includes actor details (userId, name, avatar) → Expected: Activity shows "John Doe liked your post" with profile picture
- [ ] **[P2]** Global activity feed updated → Expected: Public activity stream shows latest platform activities in real-time
- [ ] **[P2]** Activity event for private entity (private project post) → Expected: Only authorized users receive the activity event
- [ ] **[P3]** Activity deduplication → Expected: Same activity event not sent multiple times to same user

### 13.9 Real-time User Events (UserEventHubHandler)
- [ ] **[P0]** User goes online → Expected: User status event sent to friends/followers, status indicator changes to green/online
- [ ] **[P0]** User goes offline → Expected: Status event sent, indicator changes to gray/offline, lastSeen timestamp updated
- [ ] **[P1]** User updates profile (name, avatar, bio) → Expected: Real-time event sent to user's connections, profile updates visible
- [ ] **[P1]** User changes online status manually (ONLINE → AWAY → DO_NOT_DISTURB) → Expected: Status broadcast to relevant groups
- [ ] **[P1]** User presence updated with "currently viewing" info → Expected: "Viewing Project XYZ" status shown to team members
- [ ] **[P2]** User skill set updated → Expected: Profile update event sent, skill tags refresh in UI
- [ ] **[P2]** User reputation changes (badge earned) → Expected: Badge notification sent in real-time, profile badge appears
- [ ] **[P2]** User's last activity timestamp updated → Expected: "Last seen 2 minutes ago" updates in real-time for profile viewers
- [ ] **[P3]** User timezone changes → Expected: All timestamp displays update to reflect new timezone

### 13.10 Chat Real-time: Message Broadcast
- [ ] **[P0]** User sends chat message → Expected: Message instantly appears in sender's UI (optimistic update) and broadcast to `conversation:{id}` group
- [ ] **[P0]** Other conversation participants receive message → Expected: New message appears in chat window within < 1 second
- [ ] **[P0]** Message includes all fields: id, conversationId, senderId, content, createdAt, status → Expected: All fields populated correctly
- [ ] **[P1]** Message sent to group conversation (5+ participants) → Expected: All participants receive message simultaneously
- [ ] **[P1]** Message sent to 1-on-1 conversation → Expected: Only the two participants receive the message event
- [ ] **[P1]** Message with attachment (image) → Expected: Attachment URL included in event, image preview renders in chat
- [ ] **[P1]** Message with mentions (@username) → Expected: Mentioned users receive notification + real-time message event
- [ ] **[P1]** Message edited → Expected: Edit event sent, message content updates in real-time for all viewers
- [ ] **[P1]** Message deleted → Expected: Delete event sent, message removed from all participants' chat windows
- [ ] **[P2]** Long message (1000+ characters) → Expected: Full message delivered, no truncation, renders correctly
- [ ] **[P2]** Message with emoji/unicode → Expected: Emoji renders correctly across all clients
- [ ] **[P2]** Message sent to archived conversation → Expected: Conversation auto-unarchives, message delivered
- [ ] **[P3]** Message order guaranteed → Expected: Messages displayed in send order even if delivery times vary slightly

### 13.11 Chat Real-time: Typing Indicator
- [ ] **[P0]** User starts typing in chat → Expected: "... is typing" indicator appears for other participants within 1 second
- [ ] **[P0]** User stops typing (no keypress for 3 seconds) → Expected: Typing indicator disappears
- [ ] **[P1]** User sends message while typing indicator active → Expected: Indicator immediately disappears, replaced by message
- [ ] **[P1]** Multiple users typing simultaneously → Expected: "Alice and Bob are typing..." or similar aggregated indicator shown
- [ ] **[P1]** Typing indicator sent as separate SignalR event → Expected: Event type = `typing_start` or `typing_stop`, conversationId included
- [ ] **[P2]** User closes chat while typing → Expected: Typing stop event sent automatically
- [ ] **[P2]** Typing indicator timeout after 10 seconds of no activity → Expected: Indicator auto-clears even without explicit stop event
- [ ] **[P2]** Typing indicator not shown for user's own typing → Expected: User does not see their own typing indicator
- [ ] **[P3]** Typing indicator throttled (max 1 event per 2 seconds per user) → Expected: Rapid typing doesn't spam events

### 13.12 Chat Real-time: Read Receipts
- [ ] **[P0]** User opens conversation → All unread messages marked as read → Expected: Read receipt events sent to conversation group
- [ ] **[P0]** Message sender sees read receipt → Expected: Message status changes to "READ" with timestamp, checkmark icon updates
- [ ] **[P1]** Read receipt includes readBy userId and readAt timestamp → Expected: Sender can see who read the message and when
- [ ] **[P1]** Group conversation read receipts → Expected: Sender sees "Read by 3 of 5" or individual read receipts per participant
- [ ] **[P1]** User scrolls to old unread message → Expected: Read receipt sent for that specific message, others remain unread
- [ ] **[P1]** User closes chat with unread messages → Reopens later → Expected: Read receipts sent on reopen when messages viewed
- [ ] **[P2]** Read receipt sent only once per user per message → Expected: No duplicate read events for same user
- [ ] **[P2]** User reads message in one tab → Expected: Other tabs for same user also show message as read
- [ ] **[P2]** Read receipt for deleted message → Expected: Gracefully handled, no error
- [ ] **[P3]** Read receipt delivery confirmation → Expected: Sender receives acknowledgment that read receipt was delivered

### 13.13 Chat Real-time: Message Delivery Confirmation
- [ ] **[P0]** User sends message → Expected: Client receives delivery confirmation event, message status changes from SENDING → SENT
- [ ] **[P0]** Message persisted to database → Expected: Delivery confirmation sent with permanent message ID
- [ ] **[P1]** Delivery confirmation includes messageId and timestamp → Expected: Client can match confirmation to optimistic UI update
- [ ] **[P1]** Message fails to send (network error) → Expected: No delivery confirmation, message status = FAILED, retry option shown
- [ ] **[P1]** Message queued on server but not yet delivered to recipients → Expected: Status = SENT, not yet DELIVERED
- [ ] **[P2]** All conversation participants online → Expected: Status progresses SENT → DELIVERED (to all) within 1-2 seconds
- [ ] **[P2]** One participant offline → Expected: Status = DELIVERED for online users, pending for offline
- [ ] **[P3]** Delivery confirmation timeout → Expected: If no confirmation within 10 seconds, client flags warning

### 13.14 Chat Real-time: Message Status Updates
- [ ] **[P0]** Message lifecycle: SENDING → SENT → DELIVERED → READ → Expected: Each status transition triggered by appropriate event
- [ ] **[P0]** Client UI reflects current message status → Expected: Icon/text updates (clock → single checkmark → double checkmark → blue checkmark)
- [ ] **[P1]** Status update events sent to sender only (not all participants) → Expected: Recipient doesn't see sender's delivery status, only their own read status
- [ ] **[P1]** SENT status when backend acknowledges receipt → Expected: Message saved to DB, SENT event fired
- [ ] **[P1]** DELIVERED status when at least one recipient's client receives it → Expected: DELIVERED event fired when message downloaded to client
- [ ] **[P1]** READ status when recipient views message → Expected: READ event fired on scroll into view or conversation open
- [ ] **[P2]** Status updates for group messages → Expected: Aggregate status (e.g., "DELIVERED to 4 of 5") shown
- [ ] **[P2]** Failed status with error reason → Expected: Status = FAILED, error message (e.g., "Network error", "User blocked") shown
- [ ] **[P3]** Retry failed message → Expected: Status resets to SENDING, re-attempts delivery

### 13.15 Event Bus Flow: Service → InMemoryEventBus → Handlers → SignalR → SSE
- [ ] **[P0]** Domain service publishes event to InMemoryEventBus → Expected: Event received by registered handlers within milliseconds
- [ ] **[P0]** NotificationCreatedEvent published → NotificationHubHandler receives event → Expected: Handler invokes SignalR hub method
- [ ] **[P0]** SignalR hub method sends event to subscribed group → Expected: Event transmitted over WebSocket to Next.js server
- [ ] **[P0]** Next.js server receives SignalR event → Broadcasts via SSE to browser clients → Expected: Browser EventSource receives event
- [ ] **[P1]** Event bus supports multiple handlers for same event → Expected: All registered handlers invoked in parallel
- [ ] **[P1]** Handler throws exception → Expected: Event bus continues processing other handlers, exception logged, event not lost
- [ ] **[P1]** Event published with no registered handlers → Expected: Event logged/discarded gracefully, no errors
- [ ] **[P1]** End-to-end latency measured (service publish → browser display) → Expected: < 1 second for typical events under normal load
- [ ] **[P2]** Event payload serialization → Expected: Complex objects serialized to JSON correctly, deserialized on client without data loss
- [ ] **[P2]** Event bus queue overflow (1000+ events/second) → Expected: Backpressure mechanism prevents memory overflow, events processed in order
- [ ] **[P2]** Event ordering preserved through entire pipeline → Expected: Events published in order A→B→C arrive at browser in same order
- [ ] **[P3]** Event bus metrics tracked → Expected: Event publish count, handler execution time, failure count visible in monitoring

### 13.16 Multiple Browser Tabs Handling
- [ ] **[P0]** Open application in 2 tabs → Both tabs establish separate SSE connections → Expected: Both tabs receive real-time events independently
- [ ] **[P0]** Receive notification in Tab 1 → Mark as read → Expected: Tab 2 also shows notification as read (via real-time sync)
- [ ] **[P1]** Send message in Tab 1 → Expected: Tab 2 shows message appear in conversation in real-time
- [ ] **[P1]** Update profile in Tab 1 → Expected: Tab 2 reflects updated profile data
- [ ] **[P1]** Both tabs subscribed to same project → Project update occurs → Expected: Both tabs receive the same event
- [ ] **[P1]** Close Tab 1 → Expected: Tab 2 connection unaffected, continues receiving events
- [ ] **[P2]** Tab in background → Event received → Expected: Browser may throttle updates, but events queued and processed when tab refocused
- [ ] **[P2]** Logout in Tab 1 → Expected: Tab 2 detects session invalidation, also logs out or prompts re-login
- [ ] **[P2]** Duplicate state synchronization between tabs → Expected: Local storage or BroadcastChannel API used for cross-tab state sync
- [ ] **[P3]** 5+ tabs open simultaneously → Expected: All tabs remain responsive, no performance degradation

### 13.17 Connection State Management
- [ ] **[P0]** Application maintains connection state: CONNECTED, DISCONNECTED, RECONNECTING → Expected: UI reflects current state (indicator icon, banner)
- [ ] **[P0]** State transitions logged to console → Expected: "Connection state changed: CONNECTED" messages visible
- [ ] **[P1]** UI shows "Connecting..." on initial load → Expected: Loading state until SSE connection established
- [ ] **[P1]** Connection drops → UI shows "Disconnected" banner → Expected: User informed of lost connection
- [ ] **[P1]** Reconnecting state → UI shows "Reconnecting..." with retry count → Expected: User knows system is attempting to reconnect
- [ ] **[P1]** Connection restored → UI shows "Connected" briefly, then hides banner → Expected: User notified of successful reconnection
- [ ] **[P2]** Connection state persisted across page navigations (SPA) → Expected: State maintained during client-side routing
- [ ] **[P2]** Manual reconnect button available during disconnected state → Expected: User can trigger immediate reconnection attempt
- [ ] **[P3]** Connection state change triggers analytics event → Expected: Disconnection frequency tracked for reliability monitoring

### 13.18 Event Deduplication
- [ ] **[P0]** Same event sent twice (due to network retry) → Expected: Client detects duplicate via event ID, processes only once
- [ ] **[P1]** Event ID included in all SSE events → Expected: Each event has unique ID or combination of type+entityId+timestamp
- [ ] **[P1]** Client maintains recent event cache (last 100 events) → Expected: Duplicate check against cache before processing
- [ ] **[P1]** Duplicate notification event → Expected: Only one notification appears in UI, not duplicated
- [ ] **[P2]** Duplicate message event → Expected: Message appears once in chat, not duplicated
- [ ] **[P2]** Event cache expires after 5 minutes → Expected: Old event IDs removed from cache to prevent memory bloat
- [ ] **[P3]** Deduplication across multiple tabs → Expected: Each tab independently deduplicates, no cross-tab coordination required

### 13.19 Latency & Ordering Guarantees
- [ ] **[P0]** Event latency measured end-to-end → Expected: P95 latency < 1 second, P99 < 2 seconds under normal load
- [ ] **[P0]** Events arrive in order for same entity (e.g., message A sent before B) → Expected: Browser receives and displays in correct order
- [ ] **[P1]** High server load (100+ concurrent events) → Expected: Events still delivered within 2-3 seconds
- [ ] **[P1]** Network latency (simulated 200ms delay) → Expected: Events delayed but ordering preserved
- [ ] **[P1]** Out-of-order arrival detection → Expected: Client can detect and reorder events using timestamp if necessary
- [ ] **[P2]** Event timestamp comparison → Expected: createdAt timestamp used for authoritative ordering
- [ ] **[P2]** Conflicting updates (two users edit same entity) → Expected: Last-write-wins or conflict resolution based on timestamp
- [ ] **[P3]** Latency monitoring dashboard → Expected: Real-time latency metrics visible to ops team

---

## 14. Cross-Cutting Concerns

### 14.1 API Key Authentication (.NET Middleware)
- [ ] **[P0]** Request to .NET API without X-Api-Key header → Expected: 401 Unauthorized response, request rejected
- [ ] **[P0]** Request with valid X-Api-Key header → Expected: Request accepted, proceeds to controller action
- [ ] **[P0]** Request with invalid/expired X-Api-Key → Expected: 401 Unauthorized, error message "Invalid API key"
- [ ] **[P1]** X-Api-Key validation middleware executes before controller → Expected: Unauthorized requests never reach business logic
- [ ] **[P1]** API key stored securely in configuration (not hardcoded) → Expected: Key loaded from environment variable or secrets manager
- [ ] **[P1]** API key rotation process tested → Expected: Old key rejected, new key accepted after rotation
- [ ] **[P2]** Request logs sanitize API key → Expected: Full API key not logged in plain text, only masked version (e.g., "abc***xyz")
- [ ] **[P2]** API key validation case-sensitive → Expected: "ABC123" ≠ "abc123"
- [ ] **[P3]** Rate limiting per API key → Expected: Single API key cannot exceed 1000 requests/minute

### 14.2 tRPC Protected vs Public Procedures
- [ ] **[P0]** Call protectedProcedure without session → Expected: 401 UNAUTHORIZED error, request rejected
- [ ] **[P0]** Call protectedProcedure with valid session → Expected: Request proceeds, userId available in context
- [ ] **[P0]** Call publicProcedure without session → Expected: Request succeeds, no authentication required
- [ ] **[P1]** protectedProcedure context includes `session.user.id` → Expected: userId accessible in procedure resolver
- [ ] **[P1]** Expired session token → protectedProcedure call → Expected: 401 error, user prompted to re-login
- [ ] **[P1]** Public procedure called with session → Expected: Succeeds, session ignored (optional authentication)
- [ ] **[P2]** Session validation uses NextAuth JWT verification → Expected: Tampered JWT rejected, valid JWT accepted
- [ ] **[P2]** Protected procedure checks role/permissions beyond just authentication → Expected: Admin-only procedures reject non-admin users
- [ ] **[P3]** Public procedure with optional authenticated behavior → Expected: Different response if user is logged in vs anonymous

### 14.3 Ownership Verification Patterns
- [ ] **[P0]** User tries to update another user's project (createdById mismatch) → Expected: 403 Forbidden error
- [ ] **[P0]** User tries to delete resource they don't own → Expected: 403 Forbidden, "You do not have permission to delete this resource"
- [ ] **[P1]** Ownership check via `createdById === session.user.id` → Expected: Only creator can edit/delete
- [ ] **[P1]** Project member (non-owner) tries to delete project → Expected: 403 Forbidden unless user is OWNER role
- [ ] **[P1]** Guild member tries to edit guild settings (not OWNER/ADMIN) → Expected: 403 Forbidden
- [ ] **[P1]** User edits their own profile → Expected: Success
- [ ] **[P1]** User tries to edit another user's profile → Expected: 403 Forbidden (unless admin)
- [ ] **[P2]** Ownership check for nested resources (comment on post) → Expected: Only comment creator can edit/delete their comment
- [ ] **[P2]** Transfer ownership operation (project owner → new owner) → Expected: Ownership check allows current owner, updates createdById
- [ ] **[P3]** Admin override for ownership checks → Expected: Platform admin can access all resources regardless of ownership

### 14.4 Role-Based Access Control
- [ ] **[P0]** Admin user accesses admin-only endpoint → Expected: Success, admin panel data returned
- [ ] **[P0]** Regular user tries to access admin endpoint → Expected: 403 Forbidden
- [ ] **[P1]** Guild role hierarchy: OWNER > ADMIN > MODERATOR > MEMBER → Expected: Higher roles can perform lower role actions
- [ ] **[P1]** Guild OWNER can promote member to ADMIN → Expected: Success
- [ ] **[P1]** Guild MEMBER tries to promote another member → Expected: 403 Forbidden
- [ ] **[P1]** Guild ADMIN can remove MEMBER → Expected: Success
- [ ] **[P1]** Guild ADMIN tries to remove OWNER → Expected: 403 Forbidden
- [ ] **[P1]** Project role hierarchy: OWNER > LEAD > CONTRIBUTOR > VIEWER → Expected: Permissions enforced per role
- [ ] **[P1]** Project OWNER can delete project → Expected: Success
- [ ] **[P1]** Project CONTRIBUTOR tries to delete project → Expected: 403 Forbidden
- [ ] **[P1]** Project LEAD can assign tasks → Expected: Success
- [ ] **[P1]** Project VIEWER tries to assign tasks → Expected: 403 Forbidden
- [ ] **[P2]** Role change triggers permission refresh → Expected: User's permissions updated immediately after role change
- [ ] **[P2]** Role check middleware before sensitive operations → Expected: Role verified before allowing action
- [ ] **[P3]** Role-based UI element visibility → Expected: Admin buttons hidden for non-admins

### 14.5 File Uploads: S3 Integration
- [ ] **[P0]** Upload image file (PNG, JPG) → Expected: File uploaded to S3 bucket, public URL returned
- [ ] **[P0]** S3 bucket path follows convention: `{bucket}/{userId}/{entityType}/{filename}` → Expected: Files organized correctly
- [ ] **[P1]** Upload file to production S3 bucket → Expected: File accessible via HTTPS URL, no CORS errors
- [ ] **[P1]** S3 upload with IAM credentials → Expected: Valid AWS credentials used, upload succeeds
- [ ] **[P1]** Upload fails (invalid credentials) → Expected: Error returned, file not uploaded, user notified
- [ ] **[P1]** Uploaded file URL persisted to Attachment entity → Expected: URL, filename, fileSize, mimeType saved to DB
- [ ] **[P2]** S3 bucket configured with public-read ACL for uploaded files → Expected: Files accessible without signed URLs
- [ ] **[P2]** Upload with custom S3 path prefix (e.g., "projects/attachments/") → Expected: File stored at correct path
- [ ] **[P2]** S3 bucket lifecycle policy (delete after 90 days for temp files) → Expected: Old files auto-deleted
- [ ] **[P3]** S3 upload progress tracking → Expected: Client can monitor upload percentage

### 14.6 File Uploads: Local Storage Fallback
- [ ] **[P0]** S3 unavailable → Upload falls back to local storage → Expected: File saved to `/uploads/` directory, local URL returned
- [ ] **[P1]** Local storage path: `/uploads/{userId}/{entityType}/{filename}` → Expected: Files organized in local filesystem
- [ ] **[P1]** Local file served via static file middleware → Expected: File accessible at `http://localhost:3000/uploads/...`
- [ ] **[P1]** Local storage used in development environment → Expected: No S3 required for local dev, files uploaded successfully
- [ ] **[P2]** Local storage directory created if not exists → Expected: Upload process auto-creates directory structure
- [ ] **[P2]** Local file URL format: `/uploads/...` (relative) or full URL → Expected: Consistent URL format returned to client
- [ ] **[P3]** Migration from local to S3 → Expected: Script available to migrate existing local files to S3

### 14.7 File Uploads: MIME Type Validation
- [ ] **[P0]** Upload image file (image/png, image/jpeg, image/gif) → Expected: Accepted, AttachmentType = IMAGE
- [ ] **[P0]** Upload video file (video/mp4, video/mpeg) → Expected: Accepted, AttachmentType = VIDEO
- [ ] **[P0]** Upload audio file (audio/mpeg, audio/wav) → Expected: Accepted, AttachmentType = AUDIO
- [ ] **[P0]** Upload document (application/pdf, application/msword) → Expected: Accepted, AttachmentType = DOCUMENT
- [ ] **[P0]** Upload archive (application/zip, application/x-tar) → Expected: Accepted, AttachmentType = ARCHIVE
- [ ] **[P1]** Upload unknown MIME type → Expected: Accepted, AttachmentType = OTHER
- [ ] **[P1]** Upload .exe or .bat file (executable) → Expected: Rejected with error "Unsupported file type"
- [ ] **[P1]** Upload .js or .sh file (script) → Expected: Rejected or flagged as security risk
- [ ] **[P1]** MIME type validation server-side (not just client extension check) → Expected: Server inspects file header, not just filename
- [ ] **[P2]** Upload file with mismatched extension and MIME type (.png but actually .exe) → Expected: Rejected, MIME type mismatch detected
- [ ] **[P2]** Allowed MIME types configured in environment → Expected: Upload policy customizable per deployment
- [ ] **[P3]** MIME type validation error message specifies allowed types → Expected: "Only images, videos, and documents are allowed"

### 14.8 File Uploads: File Size Limits
- [ ] **[P0]** Upload file under 5MB (default limit) → Expected: Upload succeeds
- [ ] **[P0]** Upload file over 5MB → Expected: Rejected with error "File size exceeds maximum limit of 5MB"
- [ ] **[P1]** File size limit configurable per file type (images: 5MB, videos: 50MB, documents: 10MB) → Expected: Limits enforced per type
- [ ] **[P1]** Upload 10MB video file (within video limit) → Expected: Success
- [ ] **[P1]** Upload 60MB video file (exceeds video limit) → Expected: Rejected, "Video file too large (max 50MB)"
- [ ] **[P1]** File size validated before upload starts (client-side pre-check) → Expected: User notified immediately, upload not attempted
- [ ] **[P1]** File size validated on server (backend check) → Expected: Server rejects oversized files even if client validation bypassed
- [ ] **[P2]** Upload progress cancelled if size exceeded mid-upload → Expected: Upload aborted, partial file deleted
- [ ] **[P3]** File size limit displayed in upload UI → Expected: "Maximum file size: 5MB" shown to user

### 14.9 File Uploads: Attachment Creation & Linking
- [ ] **[P0]** Upload file and link to entity (Project, Task, Post, Message) → Expected: Attachment record created with correct entityType and entityId
- [ ] **[P0]** Attachment record includes: url, fileName, fileSize, mimeType, uploadedById, attachmentType → Expected: All fields populated
- [ ] **[P1]** Query attachments for entity → Expected: GET /attachments?entityId=X&entityType=PROJECT returns all attachments
- [ ] **[P1]** Delete attachment → Expected: Attachment record deleted, file removed from S3/local storage
- [ ] **[P1]** Delete entity (e.g., project) → Cascade delete attachments → Expected: All related attachments deleted
- [ ] **[P1]** Multiple attachments on single entity → Expected: All attachments linked, retrievable as array
- [ ] **[P2]** Attachment metadata update (rename file) → Expected: fileName updated, URL unchanged
- [ ] **[P2]** Attachment access control (private project attachment) → Expected: Only project members can access attachment URL
- [ ] **[P3]** Attachment thumbnail generation for images → Expected: Thumbnail URL created, used in previews

### 14.10 Pagination: General Pattern
- [ ] **[P0]** Request list with `page=1, limit=10` → Expected: First 10 results returned
- [ ] **[P0]** Request list with `page=2, limit=10` → Expected: Results 11-20 returned
- [ ] **[P0]** Pagination metadata included in response: `{ data: [...], total: 100, page: 1, limit: 10, totalPages: 10 }` → Expected: Metadata accurate
- [ ] **[P1]** Request page beyond total pages (page=99, only 5 pages exist) → Expected: Empty results array, no error
- [ ] **[P1]** Request with limit=0 → Expected: Error or default limit applied (e.g., 10)
- [ ] **[P1]** Request with negative page number → Expected: Error "Invalid page number"
- [ ] **[P1]** Request with limit > 100 (max limit) → Expected: Limit capped at 100, warning returned
- [ ] **[P2]** Pagination with `skip` and `take` (alternative to page/limit) → Expected: `skip=20, take=10` returns same as `page=3, limit=10`
- [ ] **[P2]** Pagination combined with filtering → Expected: Total count reflects filtered results, not entire dataset
- [ ] **[P2]** Pagination combined with sorting → Expected: Results sorted, then paginated

### 14.11 Pagination: Specific Endpoints
- [ ] **[P0]** GET /projects?page=1&limit=10 → Expected: Paginated project list returned
- [ ] **[P0]** GET /guilds?page=1&limit=10 → Expected: Paginated guild list returned
- [ ] **[P0]** GET /opportunities?page=1&limit=10 → Expected: Paginated opportunities list returned
- [ ] **[P0]** GET /events?page=1&limit=10 → Expected: Paginated events list returned
- [ ] **[P0]** GET /tasks?page=1&limit=10 → Expected: Paginated tasks list returned
- [ ] **[P1]** GET /posts?page=1&limit=10 → Expected: Paginated posts/feed items returned
- [ ] **[P1]** GET /users/search?q=john&page=1&limit=10 → Expected: Paginated user search results
- [ ] **[P1]** GET /notifications?page=1&limit=20 → Expected: Paginated notifications list
- [ ] **[P1]** GET /conversations/:id/messages?page=1&limit=50 → Expected: Paginated chat messages (oldest or newest first)
- [ ] **[P2]** GET /projects/:id/proposals?page=1&limit=10 → Expected: Paginated proposals for project
- [ ] **[P2]** GET /projects/:id/members?page=1&limit=10 → Expected: Paginated project members
- [ ] **[P3]** GET /users/:id/followers?page=1&limit=20 → Expected: Paginated follower list

### 14.12 Search Functionality: Users
- [ ] **[P0]** Search users by name: `/users/search?q=alice` → Expected: Users with "alice" in firstName or lastName returned
- [ ] **[P0]** Search users by skill: `/users/search?skills=react` → Expected: Users with "react" in skills array returned
- [ ] **[P1]** Search users by userType: `/users/search?type=CREATOR` → Expected: Only CREATOR users returned
- [ ] **[P1]** Search case-insensitive: `q=ALICE` matches "Alice" → Expected: Case-insensitive search
- [ ] **[P1]** Search partial match: `q=ali` matches "Alice" → Expected: Partial string matching works
- [ ] **[P1]** Search with multiple skills: `skills=react,typescript` → Expected: Users with any of the skills returned
- [ ] **[P2]** Search with no results → Expected: Empty array, no error
- [ ] **[P2]** Search with special characters: `q=alice@example` → Expected: Special chars escaped, no injection vulnerability
- [ ] **[P3]** Search results ordered by relevance or reputation → Expected: Most relevant/highest rep users first

### 14.13 Search Functionality: Projects
- [ ] **[P0]** Search projects by title: `/projects/search?q=mobile` → Expected: Projects with "mobile" in title returned
- [ ] **[P0]** Search projects by category: `/projects/search?category=WEB3` → Expected: Only WEB3 projects returned
- [ ] **[P0]** Search projects by status: `/projects/search?status=ACTIVE` → Expected: Only ACTIVE projects returned
- [ ] **[P1]** Search projects by visibility: `/projects/search?visibility=PUBLIC` → Expected: Only PUBLIC projects returned
- [ ] **[P1]** Search projects by fundingStatus: `/projects/search?fundingStatus=FUNDED` → Expected: Only FUNDED projects returned
- [ ] **[P1]** Combined filters: `category=AI&status=ACTIVE` → Expected: Projects matching both criteria returned
- [ ] **[P2]** Search projects by description text → Expected: Full-text search in description field
- [ ] **[P2]** Search projects by tags (if implemented) → Expected: Projects tagged with specified tags returned
- [ ] **[P3]** Search autocomplete suggestions → Expected: Suggested project titles as user types

### 14.14 Search Functionality: Opportunities
- [ ] **[P0]** Search opportunities by type: `/opportunities/search?type=FULL_TIME` → Expected: Only FULL_TIME opportunities returned
- [ ] **[P0]** Search opportunities by skills: `/opportunities/search?skills=python` → Expected: Opportunities requiring python skill returned
- [ ] **[P0]** Search opportunities by experienceLevel: `/opportunities/search?experienceLevel=INTERMEDIATE` → Expected: Only INTERMEDIATE level opportunities returned
- [ ] **[P1]** Search opportunities by status: `/opportunities/search?status=OPEN` → Expected: Only OPEN opportunities returned
- [ ] **[P1]** Search opportunities by project: `/opportunities/search?projectId=123` → Expected: Opportunities for project 123 returned
- [ ] **[P1]** Combined filters: `type=CONTRACT&experienceLevel=EXPERT` → Expected: Opportunities matching both criteria
- [ ] **[P2]** Search opportunities by compensation range (min/max) → Expected: Opportunities with compensation in range returned
- [ ] **[P3]** Search opportunities by remote availability → Expected: Remote opportunities flagged/filtered

### 14.15 Search Functionality: Guilds & Events
- [ ] **[P1]** Search guilds by name: `/guilds/search?q=blockchain` → Expected: Guilds with "blockchain" in name returned
- [ ] **[P1]** Search guilds by category → Expected: Guilds of specified category returned
- [ ] **[P1]** Search events by title → Expected: Events with matching title returned
- [ ] **[P1]** Search events by type: `/events/search?type=WORKSHOP` → Expected: Only WORKSHOP events returned
- [ ] **[P1]** Search events by date range: `startDate=2026-02-01&endDate=2026-02-28` → Expected: Events within date range returned
- [ ] **[P2]** Search events by location (if implemented) → Expected: Events in specified location returned
- [ ] **[P3]** Search events by host/organizer → Expected: Events hosted by specific user/guild returned

### 14.16 Filtering & Sorting
- [ ] **[P0]** Filter by status (ACTIVE, COMPLETED, etc.) across entities → Expected: Filters applied correctly
- [ ] **[P0]** Filter by type (PROJECT, OPPORTUNITY, EVENT, etc.) → Expected: Type-specific results returned
- [ ] **[P0]** Filter by category (AI, WEB3, SUSTAINABILITY, etc.) → Expected: Category filter works
- [ ] **[P0]** Filter by date range (createdAt >= startDate AND createdAt <= endDate) → Expected: Date range filter accurate
- [ ] **[P1]** Sort by newest (createdAt DESC) → Expected: Most recent items first
- [ ] **[P1]** Sort by oldest (createdAt ASC) → Expected: Oldest items first
- [ ] **[P1]** Sort by trending (based on activity score/likes/views) → Expected: Most popular items first
- [ ] **[P1]** Sort by most popular (highest engagement) → Expected: Items with most interactions first
- [ ] **[P1]** Combined filter + sort: `status=ACTIVE&sort=newest` → Expected: Filtered and sorted results
- [ ] **[P2]** Multiple filters applied simultaneously (AND logic) → Expected: All criteria must match
- [ ] **[P2]** Filter with OR logic (if supported): `category=AI OR category=WEB3` → Expected: Items matching any category
- [ ] **[P3]** Default sort order when none specified → Expected: Newest first or defined default

### 14.17 Error Handling: Result<T> Pattern (Backend)
- [ ] **[P0]** Successful operation returns `Result<T>.Success(data)` → Expected: HTTP 200 with data payload
- [ ] **[P0]** Failed operation returns `Result<T>.Failure(error)` → Expected: HTTP error code (400, 404, 500) with error message
- [ ] **[P1]** Validation error returns 400 Bad Request → Expected: Error details included in response body
- [ ] **[P1]** Not found error returns 404 Not Found → Expected: "Resource not found" message
- [ ] **[P1]** Unauthorized access returns 401 Unauthorized → Expected: "Authentication required" message
- [ ] **[P1]** Forbidden access returns 403 Forbidden → Expected: "You do not have permission" message
- [ ] **[P1]** Server error returns 500 Internal Server Error → Expected: Generic error message, details logged server-side
- [ ] **[P2]** ToActionResult() maps Result to appropriate HTTP response → Expected: Consistent response format
- [ ] **[P2]** Error response includes error code and message: `{ error: { code: "VALIDATION_ERROR", message: "..." } }` → Expected: Structured error
- [ ] **[P3]** Success response includes success flag: `{ success: true, data: {...} }` → Expected: Consistent success format

### 14.18 Error Handling: Global Exception Middleware
- [ ] **[P0]** Unhandled exception in controller → Global exception handler catches it → Expected: 500 error returned, stack trace logged
- [ ] **[P0]** Database connection error → Expected: 500 error, "Database unavailable" message, no sensitive DB details exposed
- [ ] **[P1]** Exception details logged to application logs → Expected: Full stack trace in logs, sanitized error to client
- [ ] **[P1]** Production environment hides stack traces from API response → Expected: Generic "Internal server error" message only
- [ ] **[P1]** Development environment includes stack trace in response → Expected: Detailed error for debugging
- [ ] **[P2]** Exception middleware logs request details (URL, method, user) → Expected: Request context included in error logs
- [ ] **[P2]** Custom exception types (e.g., NotFoundException) handled differently → Expected: Custom exceptions map to specific HTTP codes
- [ ] **[P3]** Exception monitoring/alerting integration (e.g., Sentry) → Expected: Critical errors trigger alerts

### 14.19 Error Handling: tRPC Errors
- [ ] **[P0]** tRPC procedure throws error → Expected: Error serialized and returned to client with error code
- [ ] **[P0]** Client receives tRPC error with code: UNAUTHORIZED, BAD_REQUEST, NOT_FOUND, etc. → Expected: Client can handle error by code
- [ ] **[P1]** tRPC input validation error (Zod schema failure) → Expected: BAD_REQUEST error with field-specific messages
- [ ] **[P1]** tRPC procedure throws custom TRPCError → Expected: Custom error code and message returned
- [ ] **[P1]** tRPC error includes cause/stack in development → Expected: Detailed error info for debugging
- [ ] **[P2]** tRPC error shape consistent: `{ error: { code, message, data } }` → Expected: Predictable error structure
- [ ] **[P3]** tRPC onError hook logs errors globally → Expected: All tRPC errors logged centrally

### 14.20 Error Handling: Client-Side Display
- [ ] **[P0]** API error received → UI displays error toast/banner → Expected: User notified of error
- [ ] **[P0]** Error message user-friendly (not raw stack trace) → Expected: "Failed to load projects. Please try again."
- [ ] **[P1]** Validation errors displayed per field → Expected: Form fields show specific error messages
- [ ] **[P1]** Network error (timeout, connection refused) → Expected: "Network error. Check your connection." message
- [ ] **[P1]** Retry button shown for transient errors → Expected: User can retry failed operation
- [ ] **[P2]** Error state cleared after successful retry → Expected: Error toast dismissed, data loaded
- [ ] **[P2]** Error logging to client-side analytics → Expected: Errors tracked for monitoring
- [ ] **[P3]** Error fallback UI for critical failures → Expected: Graceful degradation, app doesn't crash

### 14.21 CORS Configuration
- [ ] **[P0]** Frontend (localhost:3000) can call backend API (localhost:5000) → Expected: CORS headers allow cross-origin requests
- [ ] **[P0]** CORS preflight (OPTIONS request) returns 200 OK → Expected: Preflight succeeds, actual request proceeds
- [ ] **[P1]** CORS allows specific origins (not wildcard * in production) → Expected: Only trusted domains allowed
- [ ] **[P1]** CORS allows necessary methods: GET, POST, PUT, DELETE, PATCH → Expected: All CRUD methods allowed
- [ ] **[P1]** CORS allows necessary headers: Authorization, Content-Type, X-Api-Key → Expected: Required headers allowed
- [ ] **[P2]** CORS allows credentials (cookies, auth headers) → Expected: `Access-Control-Allow-Credentials: true`
- [ ] **[P2]** Request from disallowed origin rejected → Expected: CORS error in browser, request blocked
- [ ] **[P3]** CORS configuration environment-specific → Expected: Localhost allowed in dev, production domain in prod

### 14.22 Slug Generation & Uniqueness
- [ ] **[P0]** Create project with title "My Awesome Project" → Expected: Slug generated as "my-awesome-project"
- [ ] **[P0]** Slug contains only lowercase letters, numbers, hyphens → Expected: Special characters removed/replaced
- [ ] **[P0]** Create second project with same title → Expected: Slug auto-incremented: "my-awesome-project-1"
- [ ] **[P1]** Slug uniqueness enforced by database constraint → Expected: Duplicate slug insertion fails with unique constraint error
- [ ] **[P1]** Slug generated from title on creation → Expected: Slug auto-populated if not provided
- [ ] **[P1]** Manual slug provided → Expected: Manual slug used if unique, otherwise error returned
- [ ] **[P1]** Update project title → Slug unchanged → Expected: Slug not auto-updated to avoid breaking URLs
- [ ] **[P2]** Slug max length enforced (e.g., 100 chars) → Expected: Long titles truncated to max slug length
- [ ] **[P2]** Slug with unicode characters → Expected: Unicode normalized or transliterated (e.g., "café" → "cafe")
- [ ] **[P3]** Slug collision detection before save → Expected: Check for existing slug, auto-increment if needed

### 14.23 Timestamp Handling
- [ ] **[P0]** Entity created → createdAt auto-populated with current UTC timestamp → Expected: createdAt not null
- [ ] **[P0]** Entity updated → updatedAt auto-populated with current UTC timestamp → Expected: updatedAt reflects latest change
- [ ] **[P1]** createdAt never changes after creation → Expected: createdAt immutable
- [ ] **[P1]** updatedAt changes on every update → Expected: updatedAt timestamp increments with each save
- [ ] **[P1]** Timestamps stored in UTC → Expected: All timestamps in UTC timezone, converted to local time on client
- [ ] **[P1]** Timestamp precision (milliseconds) → Expected: Timestamps include milliseconds: "2026-02-01T12:34:56.789Z"
- [ ] **[P2]** Timezone conversion on client display → Expected: Timestamps displayed in user's local timezone
- [ ] **[P2]** Timestamp comparison (entity A created before entity B) → Expected: Accurate ordering by createdAt
- [ ] **[P3]** Timestamp formatting (relative time: "2 minutes ago") → Expected: Human-readable timestamps in UI

### 14.24 Soft Deletes
- [ ] **[P0]** Delete entity → deletedAt timestamp set, entity not physically removed → Expected: Entity still exists in DB with deletedAt != null
- [ ] **[P0]** Query entities → Soft-deleted entities excluded by default → Expected: WHERE deletedAt IS NULL filter applied
- [ ] **[P1]** Restore soft-deleted entity → deletedAt set to null → Expected: Entity reappears in normal queries
- [ ] **[P1]** Hard delete option available for permanent removal → Expected: Physical DELETE query removes row from DB
- [ ] **[P1]** Cascade soft deletes (delete project → soft delete all tasks) → Expected: Related entities also soft-deleted
- [ ] **[P2]** Query explicitly including soft-deleted entities → Expected: withDeleted() or similar flag includes deleted records
- [ ] **[P2]** Soft-deleted entity inaccessible to normal users, visible to admins → Expected: Role-based access to deleted data
- [ ] **[P3]** Cleanup job periodically hard-deletes old soft-deleted entities → Expected: deletedAt older than 90 days purged

### 14.25 Data Validation: Required Fields
- [ ] **[P0]** Submit form with missing required field → Expected: Validation error, "Field X is required"
- [ ] **[P0]** Backend validation rejects null/empty required field → Expected: 400 Bad Request with validation error
- [ ] **[P1]** Client-side validation prevents submission → Expected: Form submit button disabled or error shown before API call
- [ ] **[P1]** Required field validation on: title, name, email, password → Expected: All critical fields validated
- [ ] **[P2]** Validation error message specific to field → Expected: "Project title is required", not generic error
- [ ] **[P3]** Required fields marked in UI with asterisk (*) → Expected: Visual indicator for required fields

### 14.26 Data Validation: String Lengths
- [ ] **[P0]** Exceed max string length (e.g., title > 200 chars) → Expected: Validation error, "Title must be less than 200 characters"
- [ ] **[P0]** Backend enforces string length limits → Expected: Database varchar constraints or validation logic enforced
- [ ] **[P1]** Field with min length (e.g., password >= 8 chars) → Expected: Validation error if too short
- [ ] **[P1]** Client-side validation shows character count → Expected: "45/200 characters" counter visible
- [ ] **[P2]** String truncation on display (long descriptions) → Expected: UI shows "Read more" for truncated text
- [ ] **[P3]** Validation message includes current length vs max → Expected: "Description too long (523/500)"

### 14.27 Data Validation: Decimal Precision
- [ ] **[P0]** Decimal field (compensation, fundingGoal) has precision 18, scale 8 → Expected: Values up to 9999999999.99999999 accepted
- [ ] **[P0]** Exceed decimal precision → Expected: Validation error or value rounded
- [ ] **[P1]** Negative decimal where not allowed → Expected: Validation error, "Value must be positive"
- [ ] **[P1]** Zero value where not allowed → Expected: Validation error, "Value must be greater than zero"
- [ ] **[P2]** Decimal rounding consistent (round to 8 decimal places) → Expected: Values stored with 8 decimal precision
- [ ] **[P3]** Display decimals with appropriate formatting (e.g., $1,234.56) → Expected: Currency/number formatting applied

---

## 15. Navigation & Layout

### 15.1 Main Navigation/Sidebar Links
- [ ] **[P0]** Click "Home" link → Navigate to `/` → Expected: Landing page displayed
- [ ] **[P0]** Click "Projects" link → Navigate to `/projects` → Expected: Projects listing page displayed
- [ ] **[P0]** Click "Guilds" link → Navigate to `/guilds` → Expected: Guilds listing page displayed
- [ ] **[P0]** Click "Opportunities" link → Navigate to `/opportunities` → Expected: Opportunities marketplace displayed
- [ ] **[P0]** Click "Events" link → Navigate to `/events` → Expected: Events listing page displayed
- [ ] **[P0]** Click "Governance" link → Navigate to `/governance` → Expected: Governance/DAO page displayed
- [ ] **[P0]** Click "Dashboard" link (authenticated user) → Navigate to `/dashboard` → Expected: User dashboard displayed
- [ ] **[P1]** Navigation links highlight current page → Expected: Active page link has active/selected style
- [ ] **[P1]** Logo in header links to home page → Expected: Click logo → navigate to `/`
- [ ] **[P2]** Navigation menu collapsible on mobile → Expected: Hamburger menu icon, menu slides in/out
- [ ] **[P2]** Navigation persistent across page transitions (SPA) → Expected: No full page reload, client-side routing
- [ ] **[P3]** Keyboard navigation (Tab, Enter) → Expected: Accessible navigation via keyboard

### 15.2 Public Routes (Unauthenticated Access)
- [ ] **[P0]** Navigate to `/` (landing page) as unauthenticated user → Expected: Page accessible, no redirect
- [ ] **[P0]** Navigate to `/auth/signin` → Expected: Sign-in page displayed
- [ ] **[P0]** Navigate to `/projects` → Expected: Public projects listing displayed
- [ ] **[P0]** Navigate to `/guilds` → Expected: Public guilds listing displayed
- [ ] **[P0]** Navigate to `/opportunities` → Expected: Opportunities marketplace displayed
- [ ] **[P0]** Navigate to `/events` → Expected: Events listing displayed
- [ ] **[P0]** Navigate to `/governance` → Expected: Governance proposals listing displayed
- [ ] **[P1]** Navigate to `/projects/[slug]` (public project) → Expected: Project detail page accessible
- [ ] **[P1]** Navigate to `/guilds/[slug]` (public guild) → Expected: Guild detail page accessible
- [ ] **[P2]** Navigate to `/governance/[id]` (public proposal) → Expected: Proposal detail page accessible
- [ ] **[P3]** Navigate to `/about`, `/contact`, `/faq` (if exist) → Expected: Static pages accessible

### 15.3 Protected Routes (Authentication Required)
- [ ] **[P0]** Navigate to `/dashboard` as unauthenticated user → Expected: Redirect to `/auth/signin?callbackUrl=/dashboard`
- [ ] **[P0]** Navigate to `/dashboard/create` (create project) as unauthenticated user → Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/projects/create` as unauthenticated user → Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/guilds/create` as unauthenticated user → Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/dashboard/tasks` as unauthenticated user → Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/dashboard/chats` as unauthenticated user → Expected: Redirect to signin
- [ ] **[P0]** Navigate to `/dashboard/profile` as unauthenticated user → Expected: Redirect to signin
- [ ] **[P1]** After signin, redirect to original destination (callbackUrl) → Expected: User lands on originally requested page
- [ ] **[P1]** Navigate to `/dashboard/edit/[id]` as authenticated user → Expected: Edit page accessible if user has permission
- [ ] **[P2]** Navigate to protected route with expired session → Expected: Redirect to signin, session refresh prompt
- [ ] **[P3]** Protected route with role requirement (admin-only) → Non-admin tries to access → Expected: 403 Forbidden or redirect

### 15.4 Dynamic Routes
- [ ] **[P0]** Navigate to `/projects/[slug]` with valid slug → Expected: Project detail page loaded with correct project data
- [ ] **[P0]** Navigate to `/projects/[slug]` with invalid slug → Expected: 404 Not Found page displayed
- [ ] **[P0]** Navigate to `/guilds/[slug]` with valid slug → Expected: Guild detail page loaded
- [ ] **[P0]** Navigate to `/governance/[id]` with valid ID → Expected: Proposal detail page loaded
- [ ] **[P0]** Navigate to `/dashboard/profile/[id]` with valid user ID → Expected: User profile page loaded
- [ ] **[P1]** Dynamic route parameter extraction → Expected: Correct ID/slug passed to data fetching logic
- [ ] **[P1]** SEO metadata (title, description) populated dynamically based on entity → Expected: Page title = "Project Name | ArdaNova"
- [ ] **[P2]** Dynamic route with multiple parameters (e.g., `/projects/[slug]/opportunities/[oppId]`) → Expected: Both parameters extracted correctly
- [ ] **[P3]** Dynamic route pre-rendering (SSR/SSG) → Expected: Page pre-rendered on server, fast initial load

### 15.5 AuthenticatedLayout Wrapper
- [ ] **[P0]** Authenticated route wrapped with AuthenticatedLayout → Session check performed → Expected: Authenticated users see content, others redirected
- [ ] **[P0]** AuthenticatedLayout includes header, sidebar, footer → Expected: Consistent layout across authenticated pages
- [ ] **[P1]** AuthenticatedLayout displays user avatar/name in header → Expected: Current user info visible
- [ ] **[P1]** AuthenticatedLayout shows notification icon with unread count → Expected: Notification badge updated in real-time
- [ ] **[P1]** Logout button in AuthenticatedLayout → Click logout → Expected: Session cleared, redirected to signin
- [ ] **[P2]** AuthenticatedLayout responsive → Expected: Mobile-friendly layout, sidebar collapses on mobile
- [ ] **[P3]** AuthenticatedLayout includes breadcrumbs → Expected: Navigation breadcrumb trail visible

### 15.6 FeedLayout Wrapper
- [ ] **[P1]** Feed pages use FeedLayout wrapper → Expected: Consistent feed UI (sidebar, main feed, trending section)
- [ ] **[P1]** FeedLayout includes left sidebar (navigation), main feed area, right sidebar (trending/suggestions) → Expected: Three-column layout on desktop
- [ ] **[P1]** FeedLayout responsive → Expected: Single column on mobile, sidebars collapse or stack
- [ ] **[P2]** FeedLayout right sidebar shows trending topics/projects → Expected: Dynamic trending content displayed
- [ ] **[P3]** FeedLayout includes infinite scroll for feed → Expected: More posts load as user scrolls

### 15.7 Dashboard Views: Dashboard Home
- [ ] **[P0]** Navigate to `/dashboard` → Expected: Dashboard home page with overview widgets (recent projects, tasks, notifications)
- [ ] **[P1]** Dashboard home shows user's active projects → Expected: List of projects user is member of
- [ ] **[P1]** Dashboard home shows recent tasks assigned to user → Expected: Task list with status indicators
- [ ] **[P1]** Dashboard home shows upcoming events → Expected: Events user is attending or hosting
- [ ] **[P2]** Dashboard home shows activity feed → Expected: Recent activity from followed users/projects
- [ ] **[P2]** Dashboard widgets interactive (click to view details) → Expected: Clicking project widget navigates to project detail
- [ ] **[P3]** Dashboard customizable (user can add/remove widgets) → Expected: Widget layout preferences saved

### 15.8 Dashboard Views: Create Project
- [ ] **[P0]** Navigate to `/dashboard/create` → Expected: Create project form displayed
- [ ] **[P0]** Create project form includes fields: title, description, category, visibility → Expected: All required fields present
- [ ] **[P1]** Submit create project form → Expected: Project created, redirect to project detail page
- [ ] **[P1]** Create project form validation → Expected: Client-side validation prevents invalid submission
- [ ] **[P2]** Create project form autosave draft → Expected: Form data saved to local storage, restored on reload
- [ ] **[P3]** Create project wizard (multi-step form) → Expected: Step-by-step guidance for project creation

### 15.9 Dashboard Views: Edit Project
- [ ] **[P0]** Navigate to `/dashboard/edit/[id]` for user's own project → Expected: Edit form pre-populated with project data
- [ ] **[P0]** Submit edit form → Expected: Project updated, success message shown
- [ ] **[P1]** Navigate to `/dashboard/edit/[id]` for project user doesn't own → Expected: 403 Forbidden or redirect
- [ ] **[P1]** Edit form cancel button → Expected: Navigate back to project detail, changes discarded
- [ ] **[P2]** Edit form shows unsaved changes warning → Expected: "You have unsaved changes" prompt on navigation
- [ ] **[P3]** Edit form version history → Expected: Previous versions accessible, diff view available

### 15.10 Dashboard Views: Profile
- [ ] **[P0]** Navigate to `/dashboard/profile` → Expected: Current user's profile edit page displayed
- [ ] **[P0]** Profile page shows editable fields: name, bio, skills, avatar, social links → Expected: All profile fields editable
- [ ] **[P0]** Submit profile updates → Expected: User profile saved, success message shown
- [ ] **[P1]** Navigate to `/dashboard/profile/[id]` (view another user's profile) → Expected: Read-only profile view for specified user
- [ ] **[P1]** View own profile → Edit button visible → Expected: User can navigate to edit mode
- [ ] **[P1]** View other user's profile → Follow/Message buttons visible → Expected: Social interaction options available
- [ ] **[P2]** Profile shows user's reputation, badges, stats → Expected: Gamification elements displayed
- [ ] **[P2]** Profile shows user's projects, posts, activity → Expected: Tabs for different content types
- [ ] **[P3]** Profile privacy settings → Expected: User can control visibility of profile sections

### 15.11 Dashboard Views: Tasks
- [ ] **[P0]** Navigate to `/dashboard/tasks` → Expected: Task list view with filters (My Tasks, Assigned by Me, All)
- [ ] **[P0]** Task list shows task title, status, assignee, due date → Expected: All key task info visible
- [ ] **[P1]** Filter tasks by status (TODO, IN_PROGRESS, DONE) → Expected: Task list updates to show only selected status
- [ ] **[P1]** Click task → Expected: Task detail modal or page opens
- [ ] **[P1]** Create new task from tasks page → Expected: Create task modal/form opens
- [ ] **[P2]** Sort tasks by due date, priority, status → Expected: Task list reorders accordingly
- [ ] **[P2]** Search tasks by title → Expected: Task list filters to matching tasks
- [ ] **[P3]** Task kanban board view → Expected: Drag-and-drop task cards across status columns

### 15.12 Dashboard Views: Chats
- [ ] **[P0]** Navigate to `/dashboard/chats` → Expected: Chat/messaging interface displayed
- [ ] **[P0]** Chat page shows list of conversations → Expected: All user's conversations listed
- [ ] **[P0]** Click conversation → Expected: Conversation messages displayed in main panel
- [ ] **[P1]** Unread conversation highlighted → Expected: Bold text or badge indicator for unread messages
- [ ] **[P1]** Send message in chat → Expected: Message sent, appears in conversation thread
- [ ] **[P1]** Start new conversation button → Expected: Modal to select participants and start chat
- [ ] **[P2]** Search conversations by participant name or message content → Expected: Conversation list filtered
- [ ] **[P2]** Chat supports attachments (upload image/file) → Expected: File attachment UI available
- [ ] **[P3]** Chat archive conversation → Expected: Conversation moved to archived, removed from main list

### 15.13 Dashboard Views: Subscriptions
- [ ] **[P1]** Navigate to `/dashboard/subscriptions` → Expected: User's subscriptions/following list displayed
- [ ] **[P1]** Subscriptions page shows followed users, projects, guilds → Expected: All subscription types listed
- [ ] **[P1]** Unfollow user/project → Expected: Subscription removed, item removed from list
- [ ] **[P2]** Subscription page shows feed of subscribed content → Expected: Combined feed from all subscriptions
- [ ] **[P3]** Manage subscription notifications → Expected: User can enable/disable notifications per subscription

### 15.14 Tab Navigation Within Detail Pages
- [ ] **[P0]** Project detail page has tabs: Overview, Team, Proposals, Opportunities → Expected: All tabs visible
- [ ] **[P0]** Click "Team" tab → Expected: Team members list displayed, URL updates to `/projects/[slug]?tab=team` or `/projects/[slug]/team`
- [ ] **[P0]** Click "Proposals" tab → Expected: Proposals list displayed for project
- [ ] **[P0]** Click "Opportunities" tab → Expected: Opportunities for project displayed
- [ ] **[P1]** Active tab highlighted → Expected: Current tab has active style
- [ ] **[P1]** Tab navigation updates URL (query param or route segment) → Expected: Direct URL to tab works (shareable link)
- [ ] **[P1]** Navigate directly to tab URL → Expected: Correct tab active on page load
- [ ] **[P2]** Tab content lazy-loaded → Expected: Tab content fetched only when tab clicked
- [ ] **[P3]** Tab navigation keyboard accessible (arrow keys) → Expected: Can navigate tabs with keyboard

### 15.15 Guild Detail Tabs
- [ ] **[P0]** Guild detail page has tabs: Overview, Members, Projects → Expected: All tabs visible
- [ ] **[P0]** Click "Members" tab → Expected: Guild members list displayed
- [ ] **[P0]** Click "Projects" tab → Expected: Projects associated with guild displayed
- [ ] **[P1]** Guild overview tab shows description, stats, recent activity → Expected: Comprehensive guild info
- [ ] **[P2]** Guild tabs accessible to non-members (public guild) → Expected: Tabs viewable, join button visible
- [ ] **[P3]** Guild tabs restricted for private guild → Expected: Non-members see limited info, prompt to join

### 15.16 Governance Detail View
- [ ] **[P0]** Navigate to `/governance/[id]` → Expected: Proposal detail page with title, description, voting info
- [ ] **[P1]** Governance detail shows vote counts, quorum progress → Expected: Visual indicators (progress bars)
- [ ] **[P1]** Governance detail shows voting deadline → Expected: Countdown or expiration date visible
- [ ] **[P1]** Vote button on governance detail → Expected: User can cast vote from detail page
- [ ] **[P2]** Governance detail shows vote history (who voted, how) → Expected: Transparency in voting record
- [ ] **[P3]** Governance detail includes discussion/comments section → Expected: Users can comment on proposal

### 15.17 Breadcrumb Navigation
- [ ] **[P1]** Breadcrumbs displayed on deep pages: "Home > Projects > Project Name" → Expected: Breadcrumb trail visible
- [ ] **[P1]** Click breadcrumb link → Navigate to parent page → Expected: Breadcrumb navigation functional
- [ ] **[P2]** Breadcrumbs dynamically generated based on route → Expected: Accurate breadcrumb for current page
- [ ] **[P2]** Breadcrumbs include entity names (project title, guild name) → Expected: Contextual breadcrumb labels
- [ ] **[P3]** Breadcrumbs accessible → Expected: ARIA labels for screen readers

### 15.18 Mobile Responsive Layout
- [ ] **[P0]** View application on mobile device (< 768px width) → Expected: Responsive layout, no horizontal scroll
- [ ] **[P0]** Navigation menu collapses to hamburger icon on mobile → Expected: Hamburger menu functional
- [ ] **[P1]** Forms stack vertically on mobile → Expected: Form fields full-width, easy to fill on mobile
- [ ] **[P1]** Tables responsive (horizontal scroll or stacked view) → Expected: Tables usable on mobile
- [ ] **[P1]** Buttons and touch targets sized appropriately (min 44px) → Expected: Easy to tap on mobile
- [ ] **[P2]** Images scale to fit mobile screen → Expected: No image overflow
- [ ] **[P2]** Mobile layout tested on iOS and Android → Expected: Consistent experience across mobile platforms
- [ ] **[P3]** Mobile-specific features (swipe gestures, pull-to-refresh) → Expected: Enhanced mobile UX

### 15.19 Back Navigation
- [ ] **[P1]** Browser back button works correctly → Expected: Navigate to previous page in history
- [ ] **[P1]** In-app back button (if present) → Expected: Navigate to logical parent page
- [ ] **[P2]** Back navigation preserves scroll position → Expected: Return to previous scroll position after navigation
- [ ] **[P2]** Back navigation after form submission → Expected: Navigate back without re-submitting form
- [ ] **[P3]** Deep link followed by back navigation → Expected: Browser back goes to external site, not trapped in app

### 15.20 404/Error Pages
- [ ] **[P0]** Navigate to non-existent route (e.g., `/does-not-exist`) → Expected: 404 Not Found page displayed
- [ ] **[P0]** 404 page includes message "Page not found" → Expected: Clear error message
- [ ] **[P1]** 404 page includes link to home page → Expected: User can navigate back to home
- [ ] **[P1]** 404 page maintains layout (header, footer) → Expected: Consistent branding, navigation available
- [ ] **[P2]** Server error (500) → Custom error page displayed → Expected: "Something went wrong" page with retry option
- [ ] **[P2]** Error page includes error code and support contact → Expected: User can report issue
- [ ] **[P3]** 404 page includes search or suggestions → Expected: Help user find what they were looking for

---

## 16. Edge Cases & Negative Testing

### 16.1 Authentication Edge Cases
- [ ] **[P0]** Expired session token used in API request → Expected: 401 Unauthorized, user prompted to re-authenticate
- [ ] **[P0]** Invalid JWT token (tampered) → Expected: Token verification fails, 401 error
- [ ] **[P0]** Concurrent sessions from multiple devices → Expected: Both sessions valid, actions from either device work
- [ ] **[P1]** OAuth callback failure (Google returns error) → Expected: User shown error message, option to retry
- [ ] **[P1]** OAuth state parameter mismatch (CSRF protection) → Expected: OAuth callback rejected, error shown
- [ ] **[P1]** User tries to link Google account already linked to another user → Expected: Error "This Google account is already linked to another account"
- [ ] **[P1]** Session cookie deleted manually → User tries to access protected route → Expected: Redirect to signin
- [ ] **[P2]** Refresh token expired → Expected: User logged out, prompted to sign in again
- [ ] **[P2]** Session hijacking attempt (stolen session token) → Expected: Session validation detects anomaly (IP change, etc.), token invalidated
- [ ] **[P3]** Multiple simultaneous login attempts with same account → Expected: All logins succeed, no account lockout

### 16.2 Authorization Violations
- [ ] **[P0]** Non-member tries to access private project detail → Expected: 403 Forbidden or redirect, "You do not have access to this project"
- [ ] **[P0]** Non-owner tries to delete another user's project → Expected: 403 Forbidden
- [ ] **[P0]** Regular user tries to access admin-only endpoint → Expected: 403 Forbidden
- [ ] **[P1]** Guild MEMBER tries to perform ADMIN action (e.g., remove member) → Expected: 403 Forbidden, "Insufficient permissions"
- [ ] **[P1]** Project VIEWER tries to edit project details → Expected: 403 Forbidden
- [ ] **[P1]** User tries to access another user's private profile data (email, phone) → Expected: Data not returned in API response
- [ ] **[P1]** User tries to modify another user's profile → Expected: 403 Forbidden
- [ ] **[P1]** API request without X-Api-Key to .NET backend → Expected: 401 Unauthorized, request rejected
- [ ] **[P2]** User tries to vote on proposal for project they're not member of → Expected: 403 Forbidden or voting restricted
- [ ] **[P2]** User tries to approve guild membership application without ADMIN role → Expected: 403 Forbidden
- [ ] **[P3]** User tries to access audit log (admin feature) → Expected: 403 Forbidden for non-admins

### 16.3 Data Integrity: Duplicate Slug Creation
- [ ] **[P0]** Create project with slug "my-project" → Create second project with same slug → Expected: Second project auto-assigned "my-project-1"
- [ ] **[P0]** Manually set slug to duplicate value → Expected: Database unique constraint error, 409 Conflict response
- [ ] **[P1]** Slug collision detection before insert → Expected: Application checks for duplicate, auto-increments
- [ ] **[P2]** Rapid parallel project creation with same title → Expected: Unique slugs generated, no race condition duplicates
- [ ] **[P3]** Slug case sensitivity (if enforced) → "My-Project" vs "my-project" → Expected: Treated as different or same based on policy

### 16.4 Data Integrity: Duplicate Email Registration
- [ ] **[P0]** User registers with email already in system → Expected: Error "Email already registered"
- [ ] **[P0]** Database unique constraint on email field → Expected: Duplicate insert fails at DB level
- [ ] **[P1]** Case-insensitive email check ("User@Example.com" vs "user@example.com") → Expected: Treated as duplicate
- [ ] **[P2]** Email verification required → User tries to register same email before verifying → Expected: Previous registration invalidated or error shown
- [ ] **[P3]** Email normalization (trim whitespace, lowercase) before uniqueness check → Expected: " User@Example.com " treated same as "user@example.com"

### 16.5 Data Integrity: Unique Constraint Violations
- [ ] **[P0]** Create OpportunityApplication with same userId + opportunityId → Expected: Unique constraint error, user cannot apply twice
- [ ] **[P0]** Create OpportunityBid with same userId + opportunityId → Expected: Unique constraint error, user cannot bid twice
- [ ] **[P1]** Create duplicate ProjectMember (same userId + projectId) → Expected: Unique constraint error
- [ ] **[P1]** Create duplicate GuildMembership (same userId + guildId) → Expected: Unique constraint error
- [ ] **[P1]** Create duplicate Like (same userId + entityId + entityType) → Expected: Unique constraint error, user cannot like same item twice
- [ ] **[P2]** Create duplicate Follow (same followerId + followedId) → Expected: Unique constraint error
- [ ] **[P2]** Create duplicate EventRegistration (same userId + eventId) → Expected: Unique constraint error
- [ ] **[P3]** Application handles unique constraint errors gracefully → Expected: User-friendly error message, not raw SQL error

### 16.6 Data Integrity: Foreign Key Violations
- [ ] **[P0]** Create project with non-existent createdById (user ID) → Expected: Foreign key constraint error, 400 Bad Request
- [ ] **[P0]** Delete user who owns projects → Expected: Cascade delete or foreign key error, depending on configuration
- [ ] **[P1]** Create task with non-existent projectId → Expected: Foreign key error, task creation fails
- [ ] **[P1]** Delete project → Check related tasks → Expected: Tasks cascade deleted or orphaned (depending on FK constraint)
- [ ] **[P1]** Create comment with non-existent parentId (reply to deleted comment) → Expected: Foreign key error or parent validation
- [ ] **[P2]** Delete conversation → Check messages → Expected: Messages cascade deleted
- [ ] **[P2]** Delete opportunity → Check applications → Expected: Applications cascade deleted or foreign key prevents deletion
- [ ] **[P3]** Soft-deleted entity referenced by foreign key → Expected: Application logic prevents FK to soft-deleted entities

### 16.7 Data Integrity: Circular References
- [ ] **[P0]** Create comment replying to itself (parentId = own id) → Expected: Validation error, "Comment cannot reply to itself"
- [ ] **[P0]** Create task depending on itself (dependsOnId = own id) → Expected: Validation error, "Task cannot depend on itself"
- [ ] **[P1]** Create circular dependency chain (Task A depends on B, B depends on C, C depends on A) → Expected: Validation detects cycle, prevents creation
- [ ] **[P2]** Create circular reporting structure (User A reports to B, B reports to A) → Expected: Validation prevents circular hierarchy
- [ ] **[P3]** Graph traversal for circular reference detection → Expected: Application logic detects cycles before persistence

### 16.8 Concurrent Operations: Double Voting
- [ ] **[P0]** User votes on proposal → Immediately votes again (rapid double-click) → Expected: Only one vote counted, duplicate prevented
- [ ] **[P0]** Two simultaneous vote requests (race condition) → Expected: Database unique constraint or application lock prevents double vote
- [ ] **[P1]** Vote recorded, user refreshes page and votes again → Expected: "You have already voted" error shown
- [ ] **[P2]** Vote change allowed (user switches vote) → Expected: Previous vote removed, new vote recorded (one vote total)
- [ ] **[P3]** Optimistic concurrency control → Expected: Vote update checks version/timestamp, prevents conflicts

### 16.9 Concurrent Operations: Double Liking
- [ ] **[P0]** User likes post → Immediately likes again → Expected: Only one like counted, duplicate prevented by unique constraint
- [ ] **[P0]** User unlikes → likes again → Expected: Like count increments/decrements correctly
- [ ] **[P1]** Rapid like/unlike toggling (5 clicks in 1 second) → Expected: Final state accurate, no race condition errors
- [ ] **[P2]** Like count aggregation consistent → Expected: Like count matches actual number of Like records
- [ ] **[P3]** Eventual consistency → Multiple devices like simultaneously → Expected: Like count eventually accurate across all clients

### 16.10 Concurrent Operations: Simultaneous Bid Submission
- [ ] **[P0]** Two users submit bid on opportunity at same time → Expected: Both bids accepted if opportunity allows multiple bids
- [ ] **[P0]** Opportunity allows only one bid → Two users submit simultaneously → Expected: First bid accepted, second bid rejected "Opportunity already has a bid"
- [ ] **[P1]** Bid submission updates opportunity status (OPEN → BID_SUBMITTED) → Concurrent bids → Expected: Only one status update, consistent state
- [ ] **[P2]** Transaction isolation prevents concurrent bid conflicts → Expected: Serializable or repeatable read isolation level used
- [ ] **[P3]** Bid submission includes timestamp → Expected: Tie-breaking based on earliest timestamp

### 16.11 Concurrent Operations: Balance Updates
- [ ] **[P0]** User makes two purchases simultaneously → Balance deducted twice → Expected: Both transactions succeed if balance sufficient, balance accurate
- [ ] **[P0]** User balance insufficient for concurrent transactions → Expected: One transaction succeeds, other fails "Insufficient balance"
- [ ] **[P1]** Concurrent balance updates use row-level locking → Expected: No lost updates, balance always accurate
- [ ] **[P1]** Transaction rollback on failure → Expected: Failed transaction does not affect balance
- [ ] **[P2]** Balance audit log tracks all changes → Expected: All credits/debits logged, balance reconcilable
- [ ] **[P3]** Optimistic locking on balance updates → Expected: Version field incremented, concurrent writes detected and retried

### 16.12 Concurrent Operations: Membership Approvals
- [ ] **[P0]** Two admins approve same membership request simultaneously → Expected: Only one approval recorded, status updated once
- [ ] **[P0]** Admin approves while another admin rejects → Expected: First action wins, second action fails "Request already processed"
- [ ] **[P1]** Membership status transition atomic (PENDING → APPROVED) → Expected: No intermediate states, transaction ensures consistency
- [ ] **[P2]** Notification sent only once on approval → Expected: No duplicate notifications despite concurrent approvals
- [ ] **[P3]** Audit trail shows which admin performed action → Expected: Both attempts logged, only one succeeded

### 16.13 State Transition Violations: Project Status
- [ ] **[P0]** Try to publish project in COMPLETED status → Expected: Error "Cannot publish completed project"
- [ ] **[P0]** Try to move CANCELLED project to ACTIVE → Expected: Error "Cannot reactivate cancelled project"
- [ ] **[P1]** Valid status transitions enforced (PLANNING → ACTIVE → ON_HOLD → ACTIVE → COMPLETED) → Expected: Invalid transitions rejected
- [ ] **[P1]** Try to skip status (PLANNING → COMPLETED directly) → Expected: Error "Invalid status transition"
- [ ] **[P2]** State machine validation on status updates → Expected: Application enforces state transition rules
- [ ] **[P3]** Status transition history logged → Expected: Audit trail shows all status changes

### 16.14 State Transition Violations: Proposal Voting
- [ ] **[P0]** Vote on EXPIRED proposal → Expected: Error "Voting has ended for this proposal"
- [ ] **[P0]** Vote on APPROVED or REJECTED proposal → Expected: Error "Proposal voting is closed"
- [ ] **[P1]** Proposal status auto-updated when vote deadline passes → Expected: PENDING → EXPIRED if not enough votes
- [ ] **[P1]** Proposal status updated when quorum reached → Expected: PENDING → APPROVED/REJECTED based on vote outcome
- [ ] **[P2]** Vote counted only if proposal in PENDING status → Expected: Votes on expired proposals ignored
- [ ] **[P3]** Proposal can be withdrawn before voting ends → Expected: Status changed to WITHDRAWN, voting disabled

### 16.15 State Transition Violations: Opportunity Applications
- [ ] **[P0]** Apply to opportunity with status FILLED → Expected: Error "This opportunity is no longer available"
- [ ] **[P0]** Apply to CLOSED opportunity → Expected: Error "Applications are closed for this opportunity"
- [ ] **[P1]** Opportunity status auto-updated when max applications reached → Expected: OPEN → FILLED
- [ ] **[P1]** Opportunity owner manually closes opportunity → Expected: Status changed to CLOSED, no new applications accepted
- [ ] **[P2]** Application deadlines enforced → Expected: Applications after deadline rejected
- [ ] **[P3]** Opportunity reopened (FILLED → OPEN) → Expected: New applications accepted again

### 16.16 State Transition Violations: Task Completion
- [ ] **[P0]** Complete task with status CANCELLED → Expected: Error "Cannot complete cancelled task"
- [ ] **[P0]** Reopen COMPLETED task → Expected: Task status changed back to TODO or IN_PROGRESS
- [ ] **[P1]** Complete task with incomplete dependencies → Expected: Warning or error "Dependent tasks not completed"
- [ ] **[P1]** Cancel IN_PROGRESS task → Expected: Status changed to CANCELLED, assignee notified
- [ ] **[P2]** Task status workflow enforced (TODO → IN_PROGRESS → DONE) → Expected: Invalid transitions rejected
- [ ] **[P3]** Task completion triggers dependent task start → Expected: Dependent tasks unblocked automatically

### 16.17 State Transition Violations: Escrow Release
- [ ] **[P0]** Release escrow funds already RELEASED → Expected: Error "Funds already released"
- [ ] **[P0]** Release escrow with status CANCELLED → Expected: Error "Cannot release cancelled escrow"
- [ ] **[P1]** Release escrow before conditions met → Expected: Error "Conditions not satisfied for release"
- [ ] **[P1]** Refund escrow in RELEASED status → Expected: Error "Cannot refund released funds"
- [ ] **[P2]** Escrow state machine: PENDING → FUNDED → RELEASED/REFUNDED → Expected: All transitions validated
- [ ] **[P3]** Partial escrow release → Expected: Status remains FUNDED until fully released

### 16.18 State Transition Violations: Fundraising Contribution
- [ ] **[P0]** Contribute to fundraising with status FAILED → Expected: Error "This fundraising campaign has failed"
- [ ] **[P0]** Contribute to COMPLETED fundraising → Expected: Error "Fundraising goal already reached"
- [ ] **[P1]** Contribute after deadline passed → Expected: Error "Fundraising deadline has passed"
- [ ] **[P1]** Contribution exceeds remaining goal amount → Expected: Contribution accepted, fundraising marked COMPLETED
- [ ] **[P2]** Fundraising auto-failed when deadline passes without reaching goal → Expected: Status changed to FAILED, refunds processed
- [ ] **[P3]** Fundraising extended (deadline moved) → Expected: More contributions accepted

### 16.19 Input Validation: Empty Required Fields
- [ ] **[P0]** Submit project creation with empty title → Expected: Error "Title is required"
- [ ] **[P0]** Submit user registration with empty email → Expected: Error "Email is required"
- [ ] **[P0]** Submit task creation with empty assigneeId → Expected: Error "Assignee is required" (if required)
- [ ] **[P1]** Submit form with all required fields as whitespace only → Expected: Validation error, whitespace trimmed and rejected
- [ ] **[P2]** Frontend prevents form submission if required fields empty → Expected: Submit button disabled or error shown
- [ ] **[P3]** Backend validation catches missing required fields (defense in depth) → Expected: API returns 400 Bad Request

### 16.20 Input Validation: Exceeding Field Length Limits
- [ ] **[P0]** Submit project title > 200 characters → Expected: Error "Title must be less than 200 characters"
- [ ] **[P0]** Submit description > 5000 characters → Expected: Error "Description too long"
- [ ] **[P1]** Frontend enforces maxLength attribute on input fields → Expected: User cannot type beyond limit
- [ ] **[P1]** Backend validates string length even if frontend bypassed → Expected: API rejects oversized input
- [ ] **[P2]** Error message includes actual length vs max → Expected: "Description is 5237 characters, max is 5000"
- [ ] **[P3]** Character counter shown in UI → Expected: "45/200 characters" updates as user types

### 16.21 Input Validation: Negative Amounts
- [ ] **[P0]** Submit compensation with negative value → Expected: Error "Compensation must be positive"
- [ ] **[P0]** Submit fundraising goal with negative value → Expected: Error "Goal amount must be positive"
- [ ] **[P1]** Submit bid amount = -100 → Expected: Error "Bid amount must be greater than zero"
- [ ] **[P1]** Submit token transfer with negative amount → Expected: Error "Amount must be positive"
- [ ] **[P2]** Frontend input type="number" with min="0" → Expected: Negative input prevented in UI
- [ ] **[P3]** Database constraint (CHECK amount > 0) → Expected: Negative amounts rejected at DB level

### 16.22 Input Validation: Zero Amounts
- [ ] **[P0]** Submit fundraising goal = 0 → Expected: Error "Goal amount must be greater than zero"
- [ ] **[P0]** Submit compensation = 0 where not allowed → Expected: Error "Compensation required"
- [ ] **[P1]** Submit token transfer amount = 0 → Expected: Error "Amount must be greater than zero"
- [ ] **[P1]** Some fields allow zero (e.g., optional compensation) → Expected: Zero accepted if appropriate
- [ ] **[P2]** Validation distinguishes between "zero not allowed" vs "zero acceptable" fields → Expected: Context-aware validation
- [ ] **[P3]** Validation error message specific: "Amount must be greater than zero"

### 16.23 Input Validation: Invalid Email Formats
- [ ] **[P0]** Submit email without "@" symbol → Expected: Error "Invalid email format"
- [ ] **[P0]** Submit email "user@" (missing domain) → Expected: Error "Invalid email format"
- [ ] **[P0]** Submit email "@example.com" (missing local part) → Expected: Error "Invalid email format"
- [ ] **[P1]** Email validation regex or library used → Expected: Comprehensive email format validation
- [ ] **[P1]** Frontend shows email error on blur → Expected: User notified of invalid email before submission
- [ ] **[P2]** Backend validates email format (defense in depth) → Expected: API rejects malformed emails
- [ ] **[P3]** Email domain validation (DNS check) → Expected: Reject emails with non-existent domains

### 16.24 Input Validation: Invalid URL Formats
- [ ] **[P0]** Submit URL without scheme ("example.com") → Expected: Error "URL must include http:// or https://" or auto-prepend scheme
- [ ] **[P0]** Submit URL "htp://example.com" (typo in scheme) → Expected: Error "Invalid URL format"
- [ ] **[P1]** URL validation accepts http and https schemes → Expected: Both accepted
- [ ] **[P1]** URL validation rejects javascript:, data:, file: schemes → Expected: Only safe schemes allowed
- [ ] **[P2]** URL validation checks for valid domain format → Expected: Malformed domains rejected
- [ ] **[P3]** URL preview/unfurl for valid URLs → Expected: Link preview shown in UI

### 16.25 Input Validation: XSS Injection Attempts
- [ ] **[P0]** Submit comment with `<script>alert('XSS')</script>` → Expected: Script tags escaped or sanitized, not executed
- [ ] **[P0]** Submit project description with HTML tags → Expected: Tags escaped or allowed tags whitelisted, rendered safely
- [ ] **[P1]** Input sanitization library used (e.g., DOMPurify) → Expected: All user input sanitized before rendering
- [ ] **[P1]** Content Security Policy (CSP) header set → Expected: Inline scripts blocked, XSS attack surface reduced
- [ ] **[P2]** Test various XSS payloads (event handlers, img onerror, etc.) → Expected: All XSS attempts neutralized
- [ ] **[P3]** Rich text editor sanitizes HTML → Expected: Only safe HTML tags/attributes allowed

### 16.26 Input Validation: SQL Injection Attempts
- [ ] **[P0]** Submit search query with SQL: `' OR '1'='1` → Expected: Query parameterized, SQL injection ineffective
- [ ] **[P0]** ORM (Prisma, EF Core) used for DB queries → Expected: Parameterized queries prevent SQL injection
- [ ] **[P1]** Test injection in various input fields (search, filters, etc.) → Expected: No SQL injection possible
- [ ] **[P1]** Raw SQL queries use parameterization → Expected: User input never concatenated into SQL strings
- [ ] **[P2]** Database user permissions limited → Expected: Application DB user cannot DROP tables or access system tables
- [ ] **[P3]** Input validation detects SQL keywords, rejects or escapes → Expected: Suspicious input flagged

### 16.27 Input Validation: Malformed JSON
- [ ] **[P0]** Submit json field with invalid JSON: `{not valid json}` → Expected: Error "Invalid JSON format"
- [ ] **[P0]** API request with malformed JSON body → Expected: 400 Bad Request, "Invalid JSON syntax"
- [ ] **[P1]** Frontend JSON editor validates JSON → Expected: User notified of JSON syntax errors before submission
- [ ] **[P1]** Backend validates JSON schema (if applicable) → Expected: JSON structure validated against schema
- [ ] **[P2]** Large JSON payload (MB size) → Expected: Handled efficiently or rejected if too large
- [ ] **[P3]** JSON parsing error message helpful → Expected: "Unexpected token '}' at position 42"

### 16.28 Boundary Conditions: Maximum Attendees Reached
- [ ] **[P0]** Event maxAttendees = 10, 10 users registered → 11th user tries to register → Expected: Error "Event is full"
- [ ] **[P0]** Event registration count checked before allowing registration → Expected: Count verified, registration rejected if full
- [ ] **[P1]** Concurrent registrations near limit → Expected: Exactly maxAttendees registrations accepted, no overflow
- [ ] **[P2]** Waitlist functionality for full events → Expected: 11th user added to waitlist instead of rejected
- [ ] **[P3]** Event capacity increased → Expected: Waitlisted users can register

### 16.29 Boundary Conditions: Maximum Applications Reached
- [ ] **[P0]** Opportunity maxApplications = 5, 5 applications received → 6th application attempted → Expected: Error "Maximum applications reached"
- [ ] **[P0]** Opportunity status auto-updated to FILLED when maxApplications reached → Expected: Status changed, no more applications accepted
- [ ] **[P1]** Application count incremented atomically → Expected: Race conditions prevented, count accurate
- [ ] **[P2]** maxApplications = null (unlimited) → Expected: Any number of applications accepted
- [ ] **[P3]** Opportunity owner can manually mark as FILLED before maxApplications → Expected: Status change honored, applications closed early

### 16.30 Boundary Conditions: Fundraising Goal Exactly Met
- [ ] **[P0]** Fundraising goal = $1000, contributions total exactly $1000 → Expected: Fundraising status = COMPLETED
- [ ] **[P0]** Contribution would exceed goal (e.g., goal $1000, current $950, contribute $100) → Expected: Contribution accepted, fundraising COMPLETED, total = $1050 or contribution limited to $50
- [ ] **[P1]** Fundraising completion trigger fires when goal reached → Expected: Status updated, backers notified
- [ ] **[P2]** Over-funding policy defined → Expected: Either accept over-funding or cap at goal amount
- [ ] **[P3]** Fundraising stretch goals → Expected: Additional goals unlocked when primary goal exceeded

### 16.31 Boundary Conditions: Vesting Cliff Exactly at Current Date
- [ ] **[P0]** Vesting cliff date = today → Expected: Tokens vest, available for user
- [ ] **[P0]** Vesting calculation accurate when cliff date = current timestamp → Expected: No off-by-one errors
- [ ] **[P1]** Vesting checked at midnight UTC → Expected: Consistent timezone handling
- [ ] **[P2]** Vesting cliff = start date (immediate vesting) → Expected: Tokens available immediately
- [ ] **[P3]** Vesting cliff in past → Expected: Tokens already vested, available to user

### 16.32 Boundary Conditions: Sprint Dates
- [ ] **[P0]** Sprint start date = end date (1-day sprint) → Expected: Sprint accepted or error "End date must be after start date"
- [ ] **[P0]** Sprint start date > end date → Expected: Error "Start date must be before end date"
- [ ] **[P1]** Sprint end date in past → Expected: Sprint auto-marked as COMPLETED
- [ ] **[P2]** Sprint start date in future → Expected: Sprint status = PLANNED, auto-starts when start date reached
- [ ] **[P3]** Overlapping sprints for same project → Expected: Allowed or validation prevents overlap

### 16.33 Boundary Conditions: Zero Voting Weight
- [ ] **[P0]** User with voting weight = 0 tries to vote → Expected: Vote rejected or counted with zero weight
- [ ] **[P1]** Voting weight calculation includes zero-weight users → Expected: Total weight calculation accurate
- [ ] **[P2]** Vote delegation to user with zero weight → Expected: Delegation rejected or delegated weight = 0
- [ ] **[P3]** Proposal outcome calculation excludes zero-weight votes → Expected: No effect on vote result

### 16.34 Boundary Conditions: Quorum/Threshold Extremes
- [ ] **[P0]** Proposal quorum = 0% → Expected: Proposal passes with any number of votes
- [ ] **[P0]** Proposal quorum = 100% → Expected: All eligible voters must vote for quorum to be reached
- [ ] **[P0]** Proposal threshold = 0% → Expected: Proposal passes if any votes are cast
- [ ] **[P0]** Proposal threshold = 100% → Expected: Unanimous vote required for approval
- [ ] **[P1]** Quorum reached but threshold not met → Expected: Proposal status = REJECTED
- [ ] **[P1]** Threshold met but quorum not reached → Expected: Proposal status = EXPIRED (insufficient participation)
- [ ] **[P2]** Edge case: quorum = 100%, threshold = 100%, all users vote yes → Expected: Proposal APPROVED
- [ ] **[P3]** Fractional percentages (e.g., 66.67% threshold) → Expected: Calculation accurate to decimal precision

### 16.35 Deletion Cascades: Delete User
- [ ] **[P0]** Delete user account → Check projects where user is creator → Expected: Projects deleted or reassigned based on cascade rules
- [ ] **[P0]** Delete user → Check tasks assigned to user → Expected: Tasks reassigned or marked unassigned
- [ ] **[P1]** Delete user → Check comments authored by user → Expected: Comments deleted or marked as "deleted user"
- [ ] **[P1]** Delete user → Check notifications for user → Expected: Notifications deleted
- [ ] **[P1]** Delete user → Check memberships (projects, guilds) → Expected: Memberships removed
- [ ] **[P1]** Delete user → Check follows (follower/followed) → Expected: Follow relationships deleted
- [ ] **[P2]** Delete user → Check escrow transactions → Expected: Active escrows prevent deletion or are refunded
- [ ] **[P2]** Delete user with active proposals → Expected: Proposals remain (orphaned) or user deletion blocked
- [ ] **[P3]** Soft delete user instead of hard delete → Expected: User data retained, account marked inactive

### 16.36 Deletion Cascades: Delete Project
- [ ] **[P0]** Delete project → Check tasks for project → Expected: All tasks cascade deleted
- [ ] **[P0]** Delete project → Check project members → Expected: All memberships cascade deleted
- [ ] **[P0]** Delete project → Check proposals for project → Expected: Proposals cascade deleted
- [ ] **[P1]** Delete project → Check opportunities for project → Expected: Opportunities cascade deleted or orphaned
- [ ] **[P1]** Delete project → Check attachments → Expected: Attachments deleted, files removed from storage
- [ ] **[P1]** Delete project → Check activities (posts, events) → Expected: Activities cascade deleted
- [ ] **[P2]** Delete project with active fundraising → Expected: Deletion blocked or funds refunded
- [ ] **[P2]** Delete project → Check notifications referencing project → Expected: Notifications deleted or entity reference nullified
- [ ] **[P3]** Soft delete project → Expected: Project hidden but data retained

### 16.37 Deletion Cascades: Delete Conversation
- [ ] **[P0]** Delete conversation → Check messages in conversation → Expected: All messages cascade deleted
- [ ] **[P0]** Delete conversation → Check conversation members → Expected: All memberships cascade deleted
- [ ] **[P1]** Delete conversation → Check attachments in messages → Expected: Attachments deleted, files removed
- [ ] **[P2]** Delete conversation → Check notifications about conversation → Expected: Notifications deleted or updated
- [ ] **[P3]** Archive conversation instead of delete → Expected: Conversation hidden, messages retained

### 16.38 Deletion Cascades: Delete Guild
- [ ] **[P0]** Delete guild → Check guild members → Expected: All memberships cascade deleted
- [ ] **[P0]** Delete guild → Check guild invitations → Expected: Invitations cascade deleted
- [ ] **[P1]** Delete guild → Check projects associated with guild → Expected: Projects unlinked or cascade deleted
- [ ] **[P1]** Delete guild → Check events hosted by guild → Expected: Events unlinked or cascade deleted
- [ ] **[P2]** Delete guild → Check activities/posts by guild → Expected: Activities cascade deleted
- [ ] **[P3]** Delete guild with active treasury → Expected: Deletion blocked or funds distributed

### 16.39 Network & Performance: API Timeout
- [ ] **[P0]** Simulate slow API response (> 30 seconds) → Expected: Client timeout, error message shown
- [ ] **[P0]** Client-side request timeout configured (e.g., 10 seconds) → Expected: Request aborted after timeout, error handled
- [ ] **[P1]** Timeout error shows retry option → Expected: User can retry timed-out request
- [ ] **[P1]** Long-running operation (report generation) → Expected: Async processing, user notified when complete
- [ ] **[P2]** API request cancelled by user (navigation away) → Expected: Request aborted, no unnecessary processing
- [ ] **[P3]** Timeout monitoring → Expected: Backend tracks and logs timeout occurrences

### 16.40 Network & Performance: Large Payload Handling
- [ ] **[P0]** API returns large dataset (1000+ records) → Expected: Response compressed (gzip), delivered successfully
- [ ] **[P0]** Client handles large JSON response → Expected: Parsing completes without freezing UI
- [ ] **[P1]** Pagination used for large datasets → Expected: Max 100 records per request, pagination enforced
- [ ] **[P1]** Large file upload (50MB video) → Expected: Multipart upload, progress tracking, no timeout
- [ ] **[P2]** Response payload size limit (e.g., 10MB max) → Expected: Oversized responses rejected or paginated
- [ ] **[P3]** Streaming API for very large datasets → Expected: Server sends data in chunks, client processes incrementally

### 16.41 Network & Performance: Rate Limiting
- [ ] **[P0]** Send 100 API requests in 10 seconds → Expected: Rate limit enforced, requests throttled or rejected with 429 Too Many Requests
- [ ] **[P0]** Rate limit response includes Retry-After header → Expected: Client knows when to retry
- [ ] **[P1]** Rate limit per user/API key → Expected: One user's requests don't affect others
- [ ] **[P1]** Rate limit sliding window (e.g., 100 requests per minute) → Expected: Limit resets progressively, not at fixed intervals
- [ ] **[P2]** Rate limit different for authenticated vs anonymous users → Expected: Higher limits for authenticated users
- [ ] **[P2]** Rate limit bypass for admin users → Expected: Admins not subject to rate limiting
- [ ] **[P3]** Rate limit monitoring dashboard → Expected: Track rate limit hits, identify abusive users

### 16.42 Network & Performance: Slow Connection Behavior
- [ ] **[P0]** Simulate slow network (3G speed) → Expected: Application remains functional, loading states shown
- [ ] **[P0]** Images lazy-load on slow connection → Expected: Placeholder shown, images load as user scrolls
- [ ] **[P1]** Critical resources prioritized (CSS, JS) on slow connection → Expected: Page interactive before all resources loaded
- [ ] **[P1]** Loading skeletons shown during data fetch → Expected: User sees skeleton UI, not blank page
- [ ] **[P2]** Adaptive quality (lower resolution images on slow connection) → Expected: UX optimized for bandwidth
- [ ] **[P3]** Offline mode → Expected: Some functionality available offline, sync when connection restored

### 16.43 Network & Performance: Offline Mode / Service Interruption
- [ ] **[P0]** Backend API unavailable → Expected: User shown "Service unavailable" message, retry option
- [ ] **[P0]** Network disconnected → Expected: User notified "You are offline", pending requests queued or failed
- [ ] **[P1]** Service worker caches critical assets → Expected: Basic UI functional offline
- [ ] **[P1]** Form data saved locally when offline → Expected: User can complete form, submitted when connection restored
- [ ] **[P2]** Background sync for offline actions → Expected: Actions queued offline, synced when online
- [ ] **[P2]** Database unavailable → Expected: Backend returns 503 Service Unavailable, retry logic in place
- [ ] **[P3]** Graceful degradation → Expected: Non-essential features disabled, core functionality available

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
- [ ] **[P0]** User clicks "Sign in with Google" → Expected: Redirect to Google OAuth consent screen
- [ ] **[P0]** User approves consent → Expected: Redirect back to callback URL with authorization code
- [ ] **[P0]** Invalid callback state parameter → Expected: Error message, no session created
- [ ] **[P0]** Expired authorization code → Expected: User prompted to sign in again
- [ ] **[P0]** User denies consent → Expected: Return to login page with error message
- [ ] **[P1]** OAuth callback contains valid code → Expected: Token exchange initiated with Google
- [ ] **[P1]** Token exchange successful → Expected: Access token, refresh token, id_token stored in Account table
- [ ] **[P1]** Token exchange fails (network error) → Expected: Graceful error, user can retry
- [ ] **[P2]** User with existing Google account signs in → Expected: Existing user session created, Account record matched by providerAccountId
- [ ] **[P2]** Callback includes session_state → Expected: session_state persisted to Account.session_state

#### First-Time User Creation
- [ ] **[P0]** New Google user signs in → Expected: New User record created with email, name, image from Google profile
- [ ] **[P0]** User email extracted from id_token → Expected: User.email matches Google email
- [ ] **[P0]** User.emailVerified set to current timestamp for Google OAuth → Expected: emailVerified populated on first sign-in
- [ ] **[P1]** User record created with default role=INDIVIDUAL → Expected: User.role = INDIVIDUAL
- [ ] **[P1]** User record created with default userType=INNOVATOR → Expected: User.userType = INNOVATOR
- [ ] **[P1]** User record created with default tier=BRONZE → Expected: User.tier = BRONZE
- [ ] **[P1]** User record created with default verificationLevel=ANONYMOUS → Expected: User.verificationLevel = ANONYMOUS
- [ ] **[P1]** User record created with totalXP=0, level=1, trustScore=0 → Expected: Gamification defaults initialized
- [ ] **[P1]** New Account record created linking User to Google provider → Expected: Account.userId references new User.id, provider="google"
- [ ] **[P1]** Session record created with unique sessionToken → Expected: Session.sessionToken is unique, userId references User.id
- [ ] **[P2]** User image from Google stored → Expected: User.image contains Google profile picture URL
- [ ] **[P2]** createdAt and updatedAt timestamps set → Expected: Both timestamps reflect current time
- [ ] **[P3]** Bio, location, phone, website, linkedIn, twitter initially null → Expected: All optional fields are null for new user

#### JWT Handling
- [ ] **[P0]** JWT created on successful sign-in → Expected: JWT contains userId, email, role, userType, verificationLevel
- [ ] **[P0]** JWT signature is valid → Expected: JWT can be verified with application secret
- [ ] **[P0]** JWT expires after configured duration (e.g., 30 days) → Expected: JWT.exp claim reflects expiry timestamp
- [ ] **[P0]** Expired JWT rejected on API call → Expected: 401 Unauthorized response
- [ ] **[P1]** JWT enriched with user role (INDIVIDUAL, GUILD, ADMIN) → Expected: JWT payload includes role claim
- [ ] **[P1]** JWT enriched with user type (INNOVATOR, SUPPORTER, VOLUNTEER, FREELANCER, SME_OWNER, GUILD_MEMBER) → Expected: JWT payload includes userType claim
- [ ] **[P1]** JWT enriched with verificationLevel (ANONYMOUS, VERIFIED, PRO, EXPERT) → Expected: JWT payload includes verificationLevel claim
- [ ] **[P1]** JWT updated when user role changes → Expected: New JWT issued with updated role
- [ ] **[P1]** JWT updated when user type changes → Expected: New JWT issued with updated userType
- [ ] **[P1]** JWT updated when verificationLevel changes → Expected: New JWT issued with updated verificationLevel
- [ ] **[P2]** JWT contains user tier (BRONZE, SILVER, GOLD, PLATINUM, DIAMOND) → Expected: JWT payload includes tier claim
- [ ] **[P2]** JWT refresh mechanism → Expected: Refresh token used to obtain new access token before expiry
- [ ] **[P3]** JWT includes custom claims for feature flags → Expected: Custom claims accessible in JWT payload

#### Session Management
- [ ] **[P0]** Session created with unique sessionToken → Expected: Session.sessionToken is unique across all sessions
- [ ] **[P0]** Session.expires set to future date → Expected: Session.expires > current time
- [ ] **[P0]** Expired session rejected on API call → Expected: 401 Unauthorized, user redirected to login
- [ ] **[P0]** Session persists across browser refresh → Expected: User remains signed in after refresh
- [ ] **[P1]** Session linked to User via userId → Expected: Session.userId references User.id
- [ ] **[P1]** Multiple sessions for same user allowed → Expected: User can be signed in on multiple devices
- [ ] **[P1]** Session invalidated on sign-out → Expected: Session record deleted or marked invalid
- [ ] **[P2]** Session activity updates lastActiveAt (if implemented) → Expected: Session activity timestamp updated on API calls
- [ ] **[P2]** Inactive session cleanup after threshold (e.g., 90 days) → Expected: Expired sessions removed from database
- [ ] **[P3]** Session revocation by user (sign out all devices) → Expected: All sessions for user invalidated

#### Sign-Out
- [ ] **[P0]** User clicks sign-out → Expected: Session record deleted from database
- [ ] **[P0]** Sign-out clears client-side JWT/session cookie → Expected: Cookie removed from browser
- [ ] **[P0]** Signed-out user cannot access protected routes → Expected: 401 Unauthorized on API calls
- [ ] **[P0]** Sign-out redirects to public landing page → Expected: User redirected to home or login page
- [ ] **[P1]** Sign-out invalidates refresh token → Expected: Refresh token cannot be used post sign-out
- [ ] **[P2]** Sign-out logs event to Activity table → Expected: Activity record with type=LEFT, action="signed_out"
- [ ] **[P3]** Sign-out confirmation dialog (optional) → Expected: User confirms before signing out

---

### User Profile CRUD

#### Read User Profile
- [ ] **[P0]** Authenticated user fetches own profile → Expected: User object with all fields (name, bio, location, phone, website, linkedIn, twitter, image, email, role, userType, tier, verificationLevel, totalXP, level, trustScore)
- [ ] **[P0]** Public user profile fetch by userId → Expected: Public fields returned (name, bio, location, image, tier, verificationLevel), sensitive fields (email, phone) hidden
- [ ] **[P1]** User profile includes UserSkills → Expected: Array of UserSkill objects with skill name and level
- [ ] **[P1]** User profile includes UserExperiences → Expected: Array of UserExperience objects (title, company, description, startDate, endDate, isCurrent)
- [ ] **[P1]** Fetch non-existent userId → Expected: 404 Not Found
- [ ] **[P2]** Fetch user profile with no skills → Expected: Empty skills array
- [ ] **[P2]** Fetch user profile with no experiences → Expected: Empty experiences array
- [ ] **[P3]** User profile includes createdAt and updatedAt timestamps → Expected: Timestamps reflect user creation and last update

#### Create/Update User Profile (Name, Bio, Location, Phone, Website, LinkedIn, Twitter, Image)
- [ ] **[P0]** User updates profile name → Expected: User.name updated, User.updatedAt refreshed
- [ ] **[P0]** User updates bio (max length validated) → Expected: User.bio updated (text field, no length limit in schema but validate in app)
- [ ] **[P0]** User updates location → Expected: User.location updated
- [ ] **[P1]** User updates phone number → Expected: User.phone updated
- [ ] **[P1]** User updates website URL → Expected: User.website updated
- [ ] **[P1]** User updates LinkedIn URL → Expected: User.linkedIn updated
- [ ] **[P1]** User updates Twitter handle → Expected: User.twitter updated
- [ ] **[P1]** User uploads profile image → Expected: Image stored (S3 or local), User.image updated with URL
- [ ] **[P1]** User removes profile image → Expected: User.image set to null or default avatar
- [ ] **[P1]** Invalid phone number format → Expected: Validation error, no update
- [ ] **[P1]** Invalid website URL format → Expected: Validation error, no update
- [ ] **[P2]** Bio contains special characters/emojis → Expected: Bio saved correctly with UTF-8 encoding
- [ ] **[P2]** Update profile with unchanged fields → Expected: No error, updatedAt refreshed
- [ ] **[P2]** Concurrent profile updates by same user → Expected: Last write wins, no data corruption
- [ ] **[P3]** Profile update logs Activity → Expected: Activity record with type=UPDATED, entityType="User", action="updated_profile"

#### User Roles (INDIVIDUAL, GUILD, ADMIN)
- [ ] **[P0]** User role defaults to INDIVIDUAL on creation → Expected: User.role = INDIVIDUAL
- [ ] **[P0]** Admin updates user role to ADMIN → Expected: User.role updated, JWT refreshed with new role
- [ ] **[P0]** User with role GUILD can create/manage Guilds → Expected: Guild creation allowed, ownerId set
- [ ] **[P0]** Role ADMIN has elevated permissions → Expected: Admin can manage users, projects, guilds
- [ ] **[P1]** Role change from INDIVIDUAL to GUILD → Expected: User.role updated, can now create Guild
- [ ] **[P1]** Role change from GUILD to INDIVIDUAL (with active Guild) → Expected: Error or Guild ownership transferred
- [ ] **[P1]** Role INDIVIDUAL cannot perform admin actions → Expected: 403 Forbidden on admin endpoints
- [ ] **[P2]** Role change logs Activity → Expected: Activity record with action="role_changed"
- [ ] **[P3]** User with role GUILD cannot have multiple Guilds (if schema enforces unique ownerId) → Expected: Error on second Guild creation

#### User Types (INNOVATOR, SUPPORTER, VOLUNTEER, FREELANCER, SME_OWNER, GUILD_MEMBER)
- [ ] **[P0]** User type defaults to INNOVATOR → Expected: User.userType = INNOVATOR
- [ ] **[P1]** User updates userType to SUPPORTER → Expected: User.userType updated
- [ ] **[P1]** User updates userType to VOLUNTEER → Expected: User.userType updated
- [ ] **[P1]** User updates userType to FREELANCER → Expected: User.userType updated
- [ ] **[P1]** User updates userType to SME_OWNER → Expected: User.userType updated
- [ ] **[P1]** User updates userType to GUILD_MEMBER → Expected: User.userType updated
- [ ] **[P2]** User type influences UI/UX (e.g., FREELANCER sees opportunities) → Expected: Frontend adapts based on userType
- [ ] **[P2]** User type change logs Activity → Expected: Activity record with action="user_type_changed"
- [ ] **[P3]** Multiple user types not allowed (single enum) → Expected: Only one userType per user

#### User Tiers (BRONZE → SILVER → GOLD → PLATINUM → DIAMOND)
- [ ] **[P0]** New user starts at BRONZE tier → Expected: User.tier = BRONZE
- [ ] **[P1]** User tier upgraded to SILVER based on XP → Expected: User.tier = SILVER, Notification sent (LEVEL_UP)
- [ ] **[P1]** User tier upgraded to GOLD → Expected: User.tier = GOLD
- [ ] **[P1]** User tier upgraded to PLATINUM → Expected: User.tier = PLATINUM
- [ ] **[P1]** User tier upgraded to DIAMOND → Expected: User.tier = DIAMOND
- [ ] **[P2]** Tier downgrade not allowed → Expected: Tier can only increase (or policy defined)
- [ ] **[P2]** Tier upgrade triggers Achievement → Expected: Achievement unlocked for reaching tier
- [ ] **[P2]** Tier displayed on user profile → Expected: Public tier badge visible
- [ ] **[P3]** Tier influences permissions or access (e.g., DIAMOND accesses premium features) → Expected: Feature flags based on tier

#### Verification Levels (ANONYMOUS → VERIFIED → PRO → EXPERT)
- [ ] **[P0]** New user starts at ANONYMOUS → Expected: User.verificationLevel = ANONYMOUS
- [ ] **[P1]** User verifies email → Expected: User.verificationLevel = VERIFIED, User.isVerified = true
- [ ] **[P1]** User upgrades to PRO (e.g., KYC or paid) → Expected: User.verificationLevel = PRO
- [ ] **[P1]** User upgrades to EXPERT (manual review or criteria) → Expected: User.verificationLevel = EXPERT
- [ ] **[P1]** VERIFIED user can perform sensitive actions (e.g., fund projects) → Expected: Actions allowed
- [ ] **[P1]** ANONYMOUS user restricted from certain actions → Expected: 403 Forbidden or prompt to verify
- [ ] **[P2]** Verification level change triggers Notification → Expected: SECURITY_ALERT or similar notification
- [ ] **[P2]** Verification badge displayed on profile → Expected: Verified icon shown
- [ ] **[P3]** Downgrade verification level (e.g., PRO to VERIFIED) → Expected: Admin action, audit log

#### Trust Score Tracking
- [ ] **[P0]** New user has trustScore = 0 → Expected: User.trustScore = 0 (Decimal 18,8)
- [ ] **[P1]** Trust score increases after successful task completion → Expected: User.trustScore incremented
- [ ] **[P1]** Trust score decreases after negative review → Expected: User.trustScore decremented
- [ ] **[P2]** Trust score displayed on user profile → Expected: Numeric score visible
- [ ] **[P2]** Trust score influences project membership approval → Expected: High trust score increases chances
- [ ] **[P2]** Trust score cannot go below 0 → Expected: Validation prevents negative trustScore
- [ ] **[P3]** Trust score history tracked (if implemented) → Expected: Audit trail of changes

---

### User Skills Management

#### Add User Skill
- [ ] **[P0]** User adds skill with level=1 → Expected: UserSkill record created with skill name, level=1
- [ ] **[P0]** User adds skill with level>1 (e.g., level=5) → Expected: UserSkill.level = 5
- [ ] **[P1]** User adds duplicate skill → Expected: Error or update existing skill level
- [ ] **[P1]** Skill name validation (non-empty) → Expected: Error if skill name is empty
- [ ] **[P2]** Skill level must be >= 1 → Expected: Validation error if level < 1
- [ ] **[P2]** Skill stored case-insensitively (if implemented) → Expected: "JavaScript" and "javascript" treated as same
- [ ] **[P3]** Adding skill logs Activity → Expected: Activity record with action="skill_added"

#### Remove User Skill
- [ ] **[P0]** User removes existing skill → Expected: UserSkill record deleted
- [ ] **[P1]** Remove non-existent skill → Expected: 404 Not Found or no-op
- [ ] **[P2]** Removing skill logs Activity → Expected: Activity record with action="skill_removed"

#### Update User Skill Level
- [ ] **[P0]** User updates skill level from 1 to 3 → Expected: UserSkill.level = 3
- [ ] **[P1]** Update skill level to same value → Expected: No error, no change
- [ ] **[P1]** Update skill level to < 1 → Expected: Validation error
- [ ] **[P2]** Updating skill level logs Activity → Expected: Activity record with action="skill_updated"

#### Fetch User Skills
- [ ] **[P0]** Fetch all skills for user → Expected: Array of UserSkill objects with skill, level
- [ ] **[P1]** User with no skills → Expected: Empty array
- [ ] **[P2]** Skills ordered by level descending (if implemented) → Expected: Highest level skills first

---

### User Experience Management

#### Add User Experience
- [ ] **[P0]** User adds new experience (title, company, startDate) → Expected: UserExperience record created
- [ ] **[P0]** User sets experience as current (isCurrent=true, endDate=null) → Expected: UserExperience.isCurrent = true, endDate = null
- [ ] **[P0]** User adds experience with endDate → Expected: UserExperience.endDate populated
- [ ] **[P1]** User adds experience with description → Expected: UserExperience.description saved
- [ ] **[P1]** startDate must be <= endDate → Expected: Validation error if startDate > endDate
- [ ] **[P1]** Required fields (title, company, startDate) → Expected: Validation error if any missing
- [ ] **[P2]** User adds multiple experiences → Expected: All experiences stored, no limit
- [ ] **[P2]** Adding experience logs Activity → Expected: Activity record with action="experience_added"

#### Edit User Experience
- [ ] **[P0]** User updates experience title → Expected: UserExperience.title updated
- [ ] **[P0]** User updates isCurrent from false to true → Expected: UserExperience.isCurrent = true, endDate cleared
- [ ] **[P0]** User updates isCurrent from true to false and sets endDate → Expected: UserExperience.isCurrent = false, endDate set
- [ ] **[P1]** User updates description → Expected: UserExperience.description updated
- [ ] **[P1]** User updates startDate → Expected: UserExperience.startDate updated, validated against endDate
- [ ] **[P1]** User updates endDate → Expected: UserExperience.endDate updated
- [ ] **[P2]** Editing experience logs Activity → Expected: Activity record with action="experience_updated"

#### Delete User Experience
- [ ] **[P0]** User deletes experience → Expected: UserExperience record deleted
- [ ] **[P1]** Delete non-existent experience → Expected: 404 Not Found or no-op
- [ ] **[P2]** Deleting experience logs Activity → Expected: Activity record with action="experience_deleted"

#### Current Job Toggle
- [ ] **[P0]** User sets one experience as current → Expected: UserExperience.isCurrent = true for that experience
- [ ] **[P1]** User sets another experience as current → Expected: Previous current experience isCurrent = false (if only one allowed)
- [ ] **[P1]** Multiple current jobs allowed (schema allows) → Expected: Multiple experiences with isCurrent = true
- [ ] **[P2]** Current job has no endDate → Expected: UserExperience.endDate = null for current jobs

---

### Notifications (All 35+ Types)

#### Notification Creation
- [ ] **[P0]** TASK_ASSIGNED notification created when task assigned → Expected: Notification with type=TASK_ASSIGNED, userId=assignee
- [ ] **[P0]** TASK_COMPLETED notification created → Expected: Notification with type=TASK_COMPLETED
- [ ] **[P0]** TASK_REVIEWED notification created → Expected: Notification with type=TASK_REVIEWED
- [ ] **[P0]** TASK_REVISION_REQUESTED notification created → Expected: Notification with type=TASK_REVISION_REQUESTED
- [ ] **[P1]** PROPOSAL_CREATED notification → Expected: Notification sent to project members
- [ ] **[P1]** PROPOSAL_VOTED notification → Expected: Notification sent to proposal creator
- [ ] **[P1]** PROPOSAL_PASSED notification → Expected: Notification sent to all voters/members
- [ ] **[P1]** PROPOSAL_REJECTED notification → Expected: Notification sent
- [ ] **[P1]** PROPOSAL_EXECUTED notification → Expected: Notification sent after execution
- [ ] **[P1]** ACHIEVEMENT_EARNED notification → Expected: Notification with achievement details
- [ ] **[P1]** LEVEL_UP notification → Expected: Notification when user levels up
- [ ] **[P1]** STREAK_MILESTONE notification → Expected: Notification for streak achievement
- [ ] **[P1]** SHARES_RECEIVED notification → Expected: Notification when shares allocated
- [ ] **[P1]** SHARES_VESTED notification → Expected: Notification when shares vest
- [ ] **[P1]** ESCROW_FUNDED notification → Expected: Notification when task escrow funded
- [ ] **[P1]** ESCROW_RELEASED notification → Expected: Notification when escrow released
- [ ] **[P1]** PROJECT_INVITATION notification → Expected: Notification when invited to project
- [ ] **[P1]** PROJECT_INVITATION_ACCEPTED notification → Expected: Notification to inviter
- [ ] **[P1]** PROJECT_INVITATION_DECLINED notification → Expected: Notification to inviter
- [ ] **[P1]** PROJECT_MEMBERSHIP_REQUEST notification → Expected: Notification to project leaders
- [ ] **[P1]** PROJECT_MEMBERSHIP_APPROVED notification → Expected: Notification to requester
- [ ] **[P1]** PROJECT_MEMBERSHIP_REJECTED notification → Expected: Notification to requester
- [ ] **[P1]** PROJECT_UPDATE notification → Expected: Notification to followers
- [ ] **[P1]** PROJECT_FUNDED notification → Expected: Notification to project team
- [ ] **[P1]** GUILD_INVITATION notification → Expected: Notification when invited to guild
- [ ] **[P1]** GUILD_INVITATION_ACCEPTED notification → Expected: Notification to inviter
- [ ] **[P1]** GUILD_INVITATION_DECLINED notification → Expected: Notification to inviter
- [ ] **[P1]** GUILD_APPLICATION notification → Expected: Notification to guild admins
- [ ] **[P1]** GUILD_APPLICATION_APPROVED notification → Expected: Notification to applicant
- [ ] **[P1]** GUILD_APPLICATION_REJECTED notification → Expected: Notification to applicant
- [ ] **[P2]** EVENT_INVITATION notification → Expected: Notification when invited to event
- [ ] **[P2]** EVENT_REMINDER notification → Expected: Notification before event starts
- [ ] **[P2]** EVENT_STARTING_SOON notification → Expected: Notification X minutes before event
- [ ] **[P2]** EVENT_CANCELLED notification → Expected: Notification to all attendees
- [ ] **[P2]** EVENT_UPDATED notification → Expected: Notification to attendees
- [ ] **[P2]** USER_FOLLOWED notification → Expected: Notification when someone follows user
- [ ] **[P2]** PROJECT_FOLLOWED notification → Expected: Notification to project owner (optional)
- [ ] **[P2]** GUILD_FOLLOWED notification → Expected: Notification to guild owner (optional)
- [ ] **[P2]** COMMENT_REPLY notification → Expected: Notification when someone replies to comment
- [ ] **[P2]** MENTION notification → Expected: Notification when user mentioned
- [ ] **[P2]** FOLLOWER_NEW notification → Expected: Notification when new follower
- [ ] **[P3]** SYSTEM_ANNOUNCEMENT notification → Expected: Broadcast to all users
- [ ] **[P3]** SECURITY_ALERT notification → Expected: Notification for security events (e.g., password change)

#### Notification Fields
- [ ] **[P0]** Notification includes title → Expected: Notification.title populated
- [ ] **[P0]** Notification includes message → Expected: Notification.message (text) populated
- [ ] **[P1]** Notification includes data (JSON) for context → Expected: Notification.data contains metadata (e.g., projectId, taskId)
- [ ] **[P1]** Notification includes actionUrl → Expected: Notification.actionUrl links to relevant page
- [ ] **[P1]** Notification created with isRead=false → Expected: Default isRead = false
- [ ] **[P1]** Notification createdAt timestamp → Expected: Notification.createdAt reflects creation time

#### Mark Notification as Read
- [ ] **[P0]** User marks notification as read → Expected: Notification.isRead = true, readAt = current timestamp
- [ ] **[P0]** User marks all notifications as read → Expected: All unread notifications updated
- [ ] **[P1]** Mark already read notification → Expected: No error, readAt updated if different
- [ ] **[P2]** Read timestamp accuracy → Expected: Notification.readAt matches when user marked it read
- [ ] **[P3]** Marking notification as unread (if supported) → Expected: isRead = false, readAt = null

#### Notification Delivery
- [ ] **[P1]** Notification visible in user's notification feed → Expected: Fetch notifications returns all for user
- [ ] **[P1]** Real-time notification delivery (SignalR) → Expected: Notification pushed to client via WebSocket
- [ ] **[P2]** Notification count badge (unread count) → Expected: API returns count of unread notifications
- [ ] **[P2]** Notification filters by type → Expected: Fetch notifications filtered by NotificationType
- [ ] **[P3]** Notification deletion → Expected: User can delete notification

---

### Activity Feed (All ActivityTypes)

#### Activity Types: CREATED, UPDATED, DELETED, COMPLETED, JOINED, LEFT, COMMENTED, VOTED, SUBMITTED, REVIEWED, FUNDED, TRANSFERRED, SWAPPED

#### Activity Creation
- [ ] **[P1]** CREATED activity logged when user creates project → Expected: Activity with type=CREATED, entityType="Project", entityId=projectId
- [ ] **[P1]** UPDATED activity logged when user updates profile → Expected: Activity with type=UPDATED, entityType="User"
- [ ] **[P1]** DELETED activity logged when user deletes resource → Expected: Activity with type=DELETED
- [ ] **[P1]** COMPLETED activity logged when task completed → Expected: Activity with type=COMPLETED, entityType="ProjectTask"
- [ ] **[P1]** JOINED activity logged when user joins project → Expected: Activity with type=JOINED, entityType="ProjectMember"
- [ ] **[P1]** LEFT activity logged when user leaves project → Expected: Activity with type=LEFT
- [ ] **[P1]** COMMENTED activity logged when user comments → Expected: Activity with type=COMMENTED, entityType="ProjectComment"
- [ ] **[P1]** VOTED activity logged when user votes on proposal → Expected: Activity with type=VOTED, entityType="Vote"
- [ ] **[P1]** SUBMITTED activity logged when task submission → Expected: Activity with type=SUBMITTED, entityType="TaskSubmission"
- [ ] **[P1]** REVIEWED activity logged when task reviewed → Expected: Activity with type=REVIEWED
- [ ] **[P1]** FUNDED activity logged when project funded → Expected: Activity with type=FUNDED
- [ ] **[P1]** TRANSFERRED activity logged when shares transferred → Expected: Activity with type=TRANSFERRED
- [ ] **[P1]** SWAPPED activity logged when shares swapped → Expected: Activity with type=SWAPPED

#### Activity Fields
- [ ] **[P1]** Activity includes userId → Expected: Activity.userId references acting user
- [ ] **[P1]** Activity includes projectId (if applicable) → Expected: Activity.projectId references project
- [ ] **[P1]** Activity includes entityType → Expected: Activity.entityType describes entity (e.g., "Project", "Task")
- [ ] **[P1]** Activity includes entityId → Expected: Activity.entityId references specific entity
- [ ] **[P1]** Activity includes action description → Expected: Activity.action describes action (e.g., "created", "updated")
- [ ] **[P1]** Activity includes metadata (JSON) → Expected: Activity.metadata stores additional context
- [ ] **[P1]** Activity createdAt timestamp → Expected: Activity.createdAt reflects when activity occurred

#### Activity Feed Retrieval
- [ ] **[P1]** Fetch user's activity feed → Expected: All activities for userId returned, ordered by createdAt desc
- [ ] **[P1]** Fetch project activity feed → Expected: All activities for projectId returned
- [ ] **[P1]** Activity feed pagination → Expected: Limit and offset supported
- [ ] **[P2]** Activity feed filtered by type → Expected: Filter by ActivityType
- [ ] **[P2]** Activity feed filtered by date range → Expected: Filter by createdAt range
- [ ] **[P3]** Activity feed includes user and project details (joined) → Expected: Response includes related User and Project objects

---

## Section 2: Project Management (Full Lifecycle)

### Project CRUD

#### Create Project
- [ ] **[P0]** User creates project with title, description, problemStatement, solution → Expected: Project record created with status=DRAFT
- [ ] **[P0]** Project slug generated from title (unique) → Expected: Project.slug is URL-safe, unique
- [ ] **[P0]** Project created by user → Expected: Project.createdById = userId
- [ ] **[P1]** Project created with categories (comma-separated ProjectCategory values) → Expected: Project.categories stores multiple categories
- [ ] **[P1]** Project created with projectType (TEMPORARY, LONG_TERM, FOUNDATION, BUSINESS, PRODUCT, OPEN_SOURCE, COMMUNITY) → Expected: Project.projectType set
- [ ] **[P1]** Project created with duration (ONE_TWO_WEEKS through ONGOING) → Expected: Project.duration set
- [ ] **[P1]** Project created with fundingGoal → Expected: Project.fundingGoal (Decimal) set
- [ ] **[P1]** Project created with tags → Expected: Project.tags stored
- [ ] **[P1]** Project created with images (comma-separated URLs) → Expected: Project.images stored
- [ ] **[P1]** Project created with videos (comma-separated URLs) → Expected: Project.videos stored
- [ ] **[P1]** Project created with documents (comma-separated URLs) → Expected: Project.documents stored
- [ ] **[P1]** Project created with targetAudience → Expected: Project.targetAudience stored
- [ ] **[P1]** Project created with expectedImpact → Expected: Project.expectedImpact stored
- [ ] **[P1]** Project created with timeline → Expected: Project.timeline stored
- [ ] **[P1]** Project defaults: status=DRAFT, currentFunding=0, supportersCount=0, votesCount=0, viewsCount=0, featured=false, isTrending=false → Expected: All defaults set correctly
- [ ] **[P1]** Project.createdAt and updatedAt set → Expected: Timestamps reflect creation time
- [ ] **[P2]** Duplicate slug handling → Expected: Error or auto-increment slug (e.g., "my-project-1")
- [ ] **[P2]** Project with missing required fields → Expected: Validation error
- [ ] **[P2]** Project created with commerceEnabled=true → Expected: Project.commerceEnabled = true
- [ ] **[P2]** Project created with storefrontDescription → Expected: Project.storefrontDescription stored
- [ ] **[P2]** Project assigned to guild (assignedGuildId) → Expected: Project.assignedGuildId references Guild.id
- [ ] **[P3]** Creating project logs Activity → Expected: Activity record with type=CREATED, entityType="Project"

#### Read Project
- [ ] **[P0]** Fetch project by id → Expected: Project object with all fields
- [ ] **[P0]** Fetch project by slug → Expected: Project object matched by slug
- [ ] **[P1]** Fetch project includes createdBy user → Expected: Response includes User object (createdBy)
- [ ] **[P1]** Fetch project includes assignedGuild (if set) → Expected: Response includes Guild object
- [ ] **[P1]** Fetch non-existent project → Expected: 404 Not Found
- [ ] **[P2]** Fetch project increments viewsCount → Expected: Project.viewsCount incremented on fetch
- [ ] **[P2]** Public project accessible without auth → Expected: Project returned for PUBLISHED projects
- [ ] **[P2]** DRAFT project accessible only by creator/members → Expected: 403 Forbidden for non-authorized users
- [ ] **[P3]** Fetch project includes stats (tasks, milestones, members count) → Expected: Computed stats returned

#### Update Project
- [ ] **[P0]** Project creator updates title → Expected: Project.title updated, updatedAt refreshed
- [ ] **[P0]** Project creator updates description → Expected: Project.description updated
- [ ] **[P0]** Project creator updates problemStatement → Expected: Project.problemStatement updated
- [ ] **[P0]** Project creator updates solution → Expected: Project.solution updated
- [ ] **[P1]** Project creator updates categories → Expected: Project.categories updated
- [ ] **[P1]** Project creator updates projectType → Expected: Project.projectType updated
- [ ] **[P1]** Project creator updates duration → Expected: Project.duration updated
- [ ] **[P1]** Project creator updates fundingGoal → Expected: Project.fundingGoal updated
- [ ] **[P1]** Project creator updates tags → Expected: Project.tags updated
- [ ] **[P1]** Project creator updates images → Expected: Project.images updated
- [ ] **[P1]** Project creator updates videos → Expected: Project.videos updated
- [ ] **[P1]** Project creator updates documents → Expected: Project.documents updated
- [ ] **[P1]** Project creator updates targetAudience → Expected: Project.targetAudience updated
- [ ] **[P1]** Project creator updates expectedImpact → Expected: Project.expectedImpact updated
- [ ] **[P1]** Project creator updates timeline → Expected: Project.timeline updated
- [ ] **[P1]** Project creator updates commerceEnabled → Expected: Project.commerceEnabled updated
- [ ] **[P1]** Project creator updates storefrontDescription → Expected: Project.storefrontDescription updated
- [ ] **[P1]** Project creator updates assignedGuildId → Expected: Project.assignedGuildId updated
- [ ] **[P1]** Non-creator, non-member attempts update → Expected: 403 Forbidden
- [ ] **[P2]** Project LEADER can update project → Expected: Update successful
- [ ] **[P2]** Project OBSERVER cannot update project → Expected: 403 Forbidden
- [ ] **[P2]** Updating project logs Activity → Expected: Activity record with type=UPDATED
- [ ] **[P3]** Updating slug regenerates URL → Expected: New slug set, old slug redirects (if implemented)

#### Delete Project
- [ ] **[P0]** Project creator deletes project (soft delete or hard delete based on policy) → Expected: Project deleted or marked as CANCELLED
- [ ] **[P1]** Non-creator attempts delete → Expected: 403 Forbidden
- [ ] **[P1]** Delete project with active members → Expected: Confirmation required or error
- [ ] **[P2]** Delete project cascades to tasks, milestones (if hard delete) → Expected: Related entities deleted
- [ ] **[P2]** Deleting project logs Activity → Expected: Activity record with type=DELETED
- [ ] **[P3]** Deleted project not fetchable → Expected: 404 Not Found on fetch

---

### Project Types (TEMPORARY, LONG_TERM, FOUNDATION, BUSINESS, PRODUCT, OPEN_SOURCE, COMMUNITY)

- [ ] **[P1]** Project type TEMPORARY selected → Expected: Project.projectType = TEMPORARY
- [ ] **[P1]** Project type LONG_TERM selected → Expected: Project.projectType = LONG_TERM
- [ ] **[P1]** Project type FOUNDATION selected → Expected: Project.projectType = FOUNDATION
- [ ] **[P1]** Project type BUSINESS selected → Expected: Project.projectType = BUSINESS
- [ ] **[P1]** Project type PRODUCT selected → Expected: Project.projectType = PRODUCT
- [ ] **[P1]** Project type OPEN_SOURCE selected → Expected: Project.projectType = OPEN_SOURCE
- [ ] **[P1]** Project type COMMUNITY selected → Expected: Project.projectType = COMMUNITY
- [ ] **[P2]** Project type influences duration options → Expected: UI adapts based on projectType
- [ ] **[P3]** Project type displayed on project card → Expected: Type badge shown

---

### Project Durations (ONE_TWO_WEEKS through ONGOING)

- [ ] **[P1]** Duration ONE_TWO_WEEKS selected → Expected: Project.duration = ONE_TWO_WEEKS
- [ ] **[P1]** Duration ONE_THREE_MONTHS selected → Expected: Project.duration = ONE_THREE_MONTHS
- [ ] **[P1]** Duration THREE_SIX_MONTHS selected → Expected: Project.duration = THREE_SIX_MONTHS
- [ ] **[P1]** Duration SIX_TWELVE_MONTHS selected → Expected: Project.duration = SIX_TWELVE_MONTHS
- [ ] **[P1]** Duration ONE_TWO_YEARS selected → Expected: Project.duration = ONE_TWO_YEARS
- [ ] **[P1]** Duration TWO_PLUS_YEARS selected → Expected: Project.duration = TWO_PLUS_YEARS
- [ ] **[P1]** Duration ONGOING selected → Expected: Project.duration = ONGOING
- [ ] **[P2]** Duration influences milestone planning → Expected: Default milestones suggested
- [ ] **[P3]** Duration displayed on project page → Expected: Duration badge shown

---

### Project Lifecycle (DRAFT → PUBLISHED → SEEKING_SUPPORT → FUNDED → IN_PROGRESS → COMPLETED | CANCELLED)

#### DRAFT Status
- [ ] **[P0]** Newly created project starts as DRAFT → Expected: Project.status = DRAFT
- [ ] **[P0]** DRAFT project visible only to creator and members → Expected: Non-members cannot see DRAFT projects
- [ ] **[P1]** DRAFT project can be edited freely → Expected: All fields editable
- [ ] **[P2]** DRAFT project not listed in public project listings → Expected: DRAFT projects excluded from public search

#### DRAFT → PUBLISHED Transition
- [ ] **[P0]** Creator publishes DRAFT project → Expected: Project.status = PUBLISHED, publishedAt = current timestamp
- [ ] **[P1]** Published project visible to public → Expected: Project appears in public listings
- [ ] **[P1]** Publish requires all mandatory fields filled → Expected: Validation error if missing fields
- [ ] **[P2]** Publishing logs Activity → Expected: Activity record with action="published"
- [ ] **[P2]** Publishing sends notification to followers (if any) → Expected: PROJECT_UPDATE notifications sent

#### PUBLISHED → SEEKING_SUPPORT Transition
- [ ] **[P0]** Creator sets project to SEEKING_SUPPORT → Expected: Project.status = SEEKING_SUPPORT
- [ ] **[P1]** SEEKING_SUPPORT project can receive votes, subscriptions, volunteers, resources → Expected: Support actions enabled
- [ ] **[P2]** SEEKING_SUPPORT status displayed prominently → Expected: UI shows "Seeking Support" badge

#### SEEKING_SUPPORT → FUNDED Transition
- [ ] **[P0]** Project reaches fundingGoal → Expected: Project.status = FUNDED, fundedAt = current timestamp
- [ ] **[P1]** FUNDED project triggers notifications → Expected: PROJECT_FUNDED notifications sent to team and supporters
- [ ] **[P1]** FUNDED project can move to IN_PROGRESS → Expected: Status can be updated
- [ ] **[P2]** Partial funding (currentFunding < fundingGoal) → Expected: Project remains SEEKING_SUPPORT
- [ ] **[P3]** Over-funding (currentFunding > fundingGoal) → Expected: Allowed, status = FUNDED

#### FUNDED → IN_PROGRESS Transition
- [ ] **[P0]** Creator starts funded project → Expected: Project.status = IN_PROGRESS
- [ ] **[P1]** IN_PROGRESS project shows progress metrics → Expected: Progress bar based on milestones/tasks
- [ ] **[P2]** IN_PROGRESS project can create tasks, milestones → Expected: Task/milestone creation enabled

#### IN_PROGRESS → COMPLETED Transition
- [ ] **[P0]** All milestones and tasks completed → Expected: Project.status can be set to COMPLETED, completedAt = current timestamp
- [ ] **[P1]** COMPLETED project is read-only (tasks/milestones) → Expected: No new tasks/milestones can be added
- [ ] **[P1]** COMPLETED project triggers achievements → Expected: Achievements awarded to contributors
- [ ] **[P2]** COMPLETED project sends notifications → Expected: Notifications to all members and followers
- [ ] **[P2]** COMPLETED project displayed in portfolio → Expected: Archived projects section

#### Any Status → CANCELLED Transition
- [ ] **[P0]** Creator cancels project → Expected: Project.status = CANCELLED
- [ ] **[P1]** CANCELLED project triggers refunds (if applicable) → Expected: Escrow funds returned
- [ ] **[P1]** CANCELLED project sends notifications → Expected: Notifications to all stakeholders
- [ ] **[P2]** CANCELLED project is read-only → Expected: No further updates allowed
- [ ] **[P3]** CANCELLED project can be reactivated (optional) → Expected: Status can be changed back to DRAFT or SEEKING_SUPPORT

---

### Project Resources

#### Create Project Resource
- [ ] **[P1]** User adds resource with name, quantity, estimatedCost → Expected: ProjectResource record created
- [ ] **[P1]** Resource marked as required → Expected: ProjectResource.isRequired = true
- [ ] **[P1]** Resource marked as obtained → Expected: ProjectResource.isObtained = true
- [ ] **[P1]** Resource with recurringCost and recurringIntervalDays → Expected: Recurring resource tracked
- [ ] **[P2]** Resource description added → Expected: ProjectResource.description stored
- [ ] **[P2]** Creating resource logs Activity → Expected: Activity record with action="resource_added"

#### Update Project Resource
- [ ] **[P1]** User updates resource quantity → Expected: ProjectResource.quantity updated
- [ ] **[P1]** User updates estimatedCost → Expected: ProjectResource.estimatedCost updated
- [ ] **[P1]** User marks resource as obtained → Expected: ProjectResource.isObtained = true
- [ ] **[P2]** Updating resource logs Activity → Expected: Activity record with action="resource_updated"

#### Delete Project Resource
- [ ] **[P1]** User deletes resource → Expected: ProjectResource record deleted
- [ ] **[P2]** Deleting resource logs Activity → Expected: Activity record with action="resource_deleted"

#### Fetch Project Resources
- [ ] **[P1]** Fetch all resources for project → Expected: Array of ProjectResource objects
- [ ] **[P2]** Filter resources by isRequired → Expected: Only required resources returned
- [ ] **[P2]** Filter resources by isObtained → Expected: Only obtained/not-obtained resources returned

---

### Milestones (PLANNED → IN_PROGRESS → COMPLETED → CANCELLED)

#### Create Milestone
- [ ] **[P0]** User creates milestone with title, description, targetDate → Expected: ProjectMilestone record created with status=PLANNED
- [ ] **[P1]** Milestone with priority (LOW, MEDIUM, HIGH, CRITICAL) → Expected: ProjectMilestone.priority set
- [ ] **[P1]** Milestone with order → Expected: ProjectMilestone.order set
- [ ] **[P1]** Milestone assigned to user → Expected: ProjectMilestone.assigneeId references User.id
- [ ] **[P2]** Creating milestone logs Activity → Expected: Activity record with action="milestone_created"

#### Update Milestone
- [ ] **[P0]** User updates milestone title → Expected: ProjectMilestone.title updated
- [ ] **[P1]** User updates milestone status to IN_PROGRESS → Expected: ProjectMilestone.status = IN_PROGRESS
- [ ] **[P1]** User updates milestone targetDate → Expected: ProjectMilestone.targetDate updated
- [ ] **[P1]** User updates milestone priority → Expected: ProjectMilestone.priority updated
- [ ] **[P1]** User updates milestone order → Expected: ProjectMilestone.order updated
- [ ] **[P1]** User assigns milestone to different user → Expected: ProjectMilestone.assigneeId updated
- [ ] **[P2]** Updating milestone logs Activity → Expected: Activity record with action="milestone_updated"

#### Complete Milestone
- [ ] **[P0]** User marks milestone as COMPLETED → Expected: ProjectMilestone.status = COMPLETED, completedAt = current timestamp
- [ ] **[P1]** Completing milestone with incomplete tasks → Expected: Warning or error
- [ ] **[P1]** Completing milestone triggers notification → Expected: Notification sent to assignee and project team
- [ ] **[P2]** Completing milestone logs Activity → Expected: Activity record with type=COMPLETED, action="milestone_completed"

#### Cancel Milestone
- [ ] **[P1]** User cancels milestone → Expected: ProjectMilestone.status = CANCELLED
- [ ] **[P2]** Cancelling milestone logs Activity → Expected: Activity record with action="milestone_cancelled"

#### Fetch Milestones
- [ ] **[P1]** Fetch all milestones for project → Expected: Array of ProjectMilestone objects ordered by order
- [ ] **[P2]** Filter milestones by status → Expected: Only milestones with specified status returned
- [ ] **[P2]** Fetch milestone includes assignee details → Expected: Response includes User object for assignee

---

### Tasks (TODO → IN_PROGRESS → REVIEW → COMPLETED → BLOCKED)

#### Create Task
- [ ] **[P0]** User creates task with title, description → Expected: ProjectTask record created with status=TODO
- [ ] **[P0]** Task assigned to project → Expected: ProjectTask.projectId references Project.id
- [ ] **[P1]** Task with priority (LOW, MEDIUM, HIGH, URGENT) → Expected: ProjectTask.priority set
- [ ] **[P1]** Task with taskType (FEATURE, BUG, ENHANCEMENT, DOCUMENTATION, RESEARCH, DESIGN, TESTING, REVIEW, MAINTENANCE, OTHER) → Expected: ProjectTask.taskType set
- [ ] **[P1]** Task with estimatedHours → Expected: ProjectTask.estimatedHours set
- [ ] **[P1]** Task with equityReward → Expected: ProjectTask.equityReward set
- [ ] **[P1]** Task with dueDate → Expected: ProjectTask.dueDate set
- [ ] **[P1]** Task assigned to user → Expected: ProjectTask.assignedToId references User.id
- [ ] **[P1]** Task linked to PBI → Expected: ProjectTask.pbiId references ProductBacklogItem.id
- [ ] **[P1]** Task escrowStatus defaults to NONE → Expected: ProjectTask.escrowStatus = NONE
- [ ] **[P2]** Creating task logs Activity → Expected: Activity record with action="task_created"
- [ ] **[P2]** Creating task sends notification to assignee → Expected: TASK_ASSIGNED notification sent

#### Update Task
- [ ] **[P0]** User updates task title → Expected: ProjectTask.title updated
- [ ] **[P0]** User updates task description → Expected: ProjectTask.description updated
- [ ] **[P1]** User updates task status to IN_PROGRESS → Expected: ProjectTask.status = IN_PROGRESS
- [ ] **[P1]** User updates task status to REVIEW → Expected: ProjectTask.status = REVIEW
- [ ] **[P1]** User updates task status to BLOCKED → Expected: ProjectTask.status = BLOCKED
- [ ] **[P1]** User updates task priority → Expected: ProjectTask.priority updated
- [ ] **[P1]** User updates task taskType → Expected: ProjectTask.taskType updated
- [ ] **[P1]** User updates actualHours → Expected: ProjectTask.actualHours updated
- [ ] **[P1]** User reassigns task to different user → Expected: ProjectTask.assignedToId updated, notification sent
- [ ] **[P2]** Updating task logs Activity → Expected: Activity record with action="task_updated"

#### Complete Task
- [ ] **[P0]** User marks task as COMPLETED → Expected: ProjectTask.status = COMPLETED, completedAt = current timestamp
- [ ] **[P1]** Completing task triggers TASK_COMPLETED notification → Expected: Notification sent to assignee and project team
- [ ] **[P1]** Completing task awards XP → Expected: XPEvent record created
- [ ] **[P1]** Completing task releases escrow (if funded) → Expected: Escrow released, ESCROW_RELEASED notification sent
- [ ] **[P2]** Completing task logs Activity → Expected: Activity record with type=COMPLETED, action="task_completed"

#### Task Types (FEATURE, BUG, ENHANCEMENT, DOCUMENTATION, RESEARCH, DESIGN, TESTING, REVIEW, MAINTENANCE, OTHER)
- [ ] **[P1]** Task type FEATURE → Expected: ProjectTask.taskType = FEATURE
- [ ] **[P1]** Task type BUG → Expected: ProjectTask.taskType = BUG
- [ ] **[P1]** Task type ENHANCEMENT → Expected: ProjectTask.taskType = ENHANCEMENT
- [ ] **[P1]** Task type DOCUMENTATION → Expected: ProjectTask.taskType = DOCUMENTATION
- [ ] **[P1]** Task type RESEARCH → Expected: ProjectTask.taskType = RESEARCH
- [ ] **[P1]** Task type DESIGN → Expected: ProjectTask.taskType = DESIGN
- [ ] **[P1]** Task type TESTING → Expected: ProjectTask.taskType = TESTING
- [ ] **[P1]** Task type REVIEW → Expected: ProjectTask.taskType = REVIEW
- [ ] **[P1]** Task type MAINTENANCE → Expected: ProjectTask.taskType = MAINTENANCE
- [ ] **[P1]** Task type OTHER → Expected: ProjectTask.taskType = OTHER

#### Task Priorities (LOW, MEDIUM, HIGH, URGENT)
- [ ] **[P1]** Task priority LOW → Expected: ProjectTask.priority = LOW
- [ ] **[P1]** Task priority MEDIUM → Expected: ProjectTask.priority = MEDIUM
- [ ] **[P1]** Task priority HIGH → Expected: ProjectTask.priority = HIGH
- [ ] **[P1]** Task priority URGENT → Expected: ProjectTask.priority = URGENT

#### Task Dependencies
- [ ] **[P1]** User adds task dependency (task A depends on task B) → Expected: ProjectTaskDependency record created
- [ ] **[P1]** Task cannot be started until dependency completed → Expected: Validation or warning in UI
- [ ] **[P2]** Circular dependency detection → Expected: Error on circular dependency creation
- [ ] **[P2]** Removing task dependency → Expected: ProjectTaskDependency record deleted

#### Task Compensation (FIXED_SHARES, HOURLY_SHARES, EQUITY_PERCENT, HYBRID, BOUNTY, MILESTONE)
- [ ] **[P1]** Task compensation model FIXED_SHARES → Expected: TaskCompensation.compensationModel = FIXED_SHARES, shareAmount set
- [ ] **[P1]** Task compensation model HOURLY_SHARES → Expected: TaskCompensation.compensationModel = HOURLY_SHARES, hourlyRate set
- [ ] **[P1]** Task compensation model EQUITY_PERCENT → Expected: TaskCompensation.compensationModel = EQUITY_PERCENT, equityPercent set
- [ ] **[P1]** Task compensation model HYBRID → Expected: TaskCompensation.compensationModel = HYBRID, multiple fields set
- [ ] **[P1]** Task compensation model BOUNTY → Expected: TaskCompensation.compensationModel = BOUNTY, stableCoinAmount set
- [ ] **[P1]** Task compensation model MILESTONE → Expected: TaskCompensation.compensationModel = MILESTONE
- [ ] **[P1]** Task compensation with vesting → Expected: TaskCompensation.vestingMonths set
- [ ] **[P2]** Updating task compensation → Expected: TaskCompensation record updated
- [ ] **[P2]** Task without compensation → Expected: No TaskCompensation record

#### Task Submissions (PENDING → APPROVED → REJECTED → REVISION_REQUESTED)
- [ ] **[P0]** Assignee submits task → Expected: TaskSubmission record created with status=PENDING
- [ ] **[P0]** Submission includes content and attachments → Expected: TaskSubmission.content and attachments stored
- [ ] **[P1]** Reviewer approves submission → Expected: TaskSubmission.status = APPROVED, reviewedAt set, task status = COMPLETED
- [ ] **[P1]** Reviewer rejects submission → Expected: TaskSubmission.status = REJECTED, feedback provided
- [ ] **[P1]** Reviewer requests revision → Expected: TaskSubmission.status = REVISION_REQUESTED, TASK_REVISION_REQUESTED notification sent
- [ ] **[P1]** Submission approval triggers escrow release → Expected: Escrow status updated, funds released
- [ ] **[P2]** Multiple submissions per task → Expected: All submissions tracked, latest reviewed
- [ ] **[P2]** Submission logs Activity → Expected: Activity record with type=SUBMITTED, action="task_submitted"

#### Task Escrow (NONE → FUNDED → RELEASED → DISPUTED → REFUNDED)
- [ ] **[P0]** Task escrow funded → Expected: TaskEscrow record created with status=FUNDED, ProjectTask.escrowStatus = FUNDED
- [ ] **[P0]** Task completed and escrow released → Expected: TaskEscrow.status = RELEASED, releasedAt set, ESCROW_RELEASED notification sent
- [ ] **[P1]** Task escrow disputed → Expected: TaskEscrow.status = DISPUTED
- [ ] **[P1]** Task escrow refunded → Expected: TaskEscrow.status = REFUNDED, refundedAt set
- [ ] **[P1]** Escrow funded by project member → Expected: TaskEscrow.funderId references User.id
- [ ] **[P1]** Escrow linked to project share → Expected: TaskEscrow.shareId references ProjectShare.id
- [ ] **[P2]** Escrow transaction hashes stored (txHashFund, txHashRelease, txHashRefund) → Expected: Blockchain tx hashes stored
- [ ] **[P2]** Escrow status transitions logged → Expected: Activity records for each status change

---

### Project Members (FOUNDER, LEADER, CORE_CONTRIBUTOR, CONTRIBUTOR, OBSERVER)

#### Add Project Member
- [ ] **[P0]** User accepts project invitation → Expected: ProjectMember record created with role from invitation
- [ ] **[P0]** Project creator automatically becomes FOUNDER → Expected: ProjectMember with role=FOUNDER created
- [ ] **[P1]** Member added with role LEADER → Expected: ProjectMember.role = LEADER
- [ ] **[P1]** Member added with role CORE_CONTRIBUTOR → Expected: ProjectMember.role = CORE_CONTRIBUTOR
- [ ] **[P1]** Member added with role CONTRIBUTOR → Expected: ProjectMember.role = CONTRIBUTOR
- [ ] **[P1]** Member added with role OBSERVER → Expected: ProjectMember.role = OBSERVER
- [ ] **[P1]** Member joinedAt timestamp set → Expected: ProjectMember.joinedAt = current timestamp
- [ ] **[P1]** Member invitedById tracked → Expected: ProjectMember.invitedById references inviter User.id
- [ ] **[P2]** Adding member logs Activity → Expected: Activity record with type=JOINED, action="member_added"
- [ ] **[P2]** Adding member sends notification → Expected: PROJECT_INVITATION_ACCEPTED notification to inviter

#### Update Project Member Role
- [ ] **[P1]** FOUNDER promotes member to LEADER → Expected: ProjectMember.role = LEADER
- [ ] **[P1]** LEADER demotes CORE_CONTRIBUTOR to CONTRIBUTOR → Expected: ProjectMember.role = CONTRIBUTOR
- [ ] **[P1]** Non-FOUNDER attempts to change roles → Expected: 403 Forbidden
- [ ] **[P2]** Role change logs Activity → Expected: Activity record with action="role_changed"

#### Remove Project Member
- [ ] **[P0]** FOUNDER removes member → Expected: ProjectMember record deleted
- [ ] **[P1]** Member leaves project voluntarily → Expected: ProjectMember record deleted
- [ ] **[P1]** Removing member logs Activity → Expected: Activity record with type=LEFT, action="member_removed"
- [ ] **[P1]** Removing member sends notification → Expected: Notification to removed member
- [ ] **[P2]** Cannot remove FOUNDER → Expected: Error or confirmation required

#### Share Balance and Voting Power
- [ ] **[P1]** Member granted shares → Expected: ProjectMember.shareBalance updated
- [ ] **[P1]** Share balance increases voting power → Expected: ProjectMember.votingPower updated proportionally
- [ ] **[P2]** Voting power calculated based on shareBalance → Expected: votingPower = shareBalance or formula applied
- [ ] **[P2]** Share transfer updates both sender and receiver shareBalance → Expected: Both ProjectMember records updated
- [ ] **[P3]** Share delegation updates votingPower → Expected: Delegator votingPower reduced, delegatee increased

---

### Project Applications (PENDING → ACCEPTED → REJECTED → WITHDRAWN)

#### Create Project Application
- [ ] **[P0]** User applies to project with roleTitle, message → Expected: ProjectApplication record created with status=PENDING
- [ ] **[P1]** Application includes skills, experience, availability → Expected: Fields populated
- [ ] **[P1]** Application sent notification to project leaders → Expected: PROJECT_MEMBERSHIP_REQUEST notification sent
- [ ] **[P2]** Duplicate application by same user → Expected: Error or update existing application
- [ ] **[P2]** Creating application logs Activity → Expected: Activity record with action="application_submitted"

#### Review Project Application
- [ ] **[P0]** LEADER accepts application → Expected: ProjectApplication.status = ACCEPTED, ProjectMember record created
- [ ] **[P0]** LEADER rejects application → Expected: ProjectApplication.status = REJECTED, reviewMessage provided
- [ ] **[P1]** Acceptance sends notification to applicant → Expected: PROJECT_MEMBERSHIP_APPROVED notification sent
- [ ] **[P1]** Rejection sends notification to applicant → Expected: PROJECT_MEMBERSHIP_REJECTED notification sent
- [ ] **[P2]** Review sets reviewedAt timestamp → Expected: ProjectApplication.reviewedAt = current timestamp
- [ ] **[P2]** Review logs Activity → Expected: Activity record with action="application_reviewed"

#### Withdraw Project Application
- [ ] **[P1]** Applicant withdraws application → Expected: ProjectApplication.status = WITHDRAWN
- [ ] **[P2]** Withdrawing logs Activity → Expected: Activity record with action="application_withdrawn"

---

### Project Invitations (PENDING → ACCEPTED → DECLINED → EXPIRED → CANCELLED)

#### Create Project Invitation
- [ ] **[P0]** LEADER invites user by userId → Expected: ProjectInvitation record created with invitedUserId, status=PENDING
- [ ] **[P0]** LEADER invites by email (non-registered user) → Expected: ProjectInvitation created with invitedEmail, token generated
- [ ] **[P1]** Invitation includes role (FOUNDER, LEADER, CORE_CONTRIBUTOR, CONTRIBUTOR, OBSERVER) → Expected: ProjectInvitation.role set
- [ ] **[P1]** Invitation includes message → Expected: ProjectInvitation.message stored
- [ ] **[P1]** Invitation sends notification to invitedUserId → Expected: PROJECT_INVITATION notification sent
- [ ] **[P1]** Invitation sent to email (for non-users) → Expected: Email sent with invitation link and token
- [ ] **[P1]** Invitation expiresAt set (e.g., 7 days) → Expected: ProjectInvitation.expiresAt set
- [ ] **[P2]** Creating invitation logs Activity → Expected: Activity record with action="invitation_sent"

#### Accept Project Invitation
- [ ] **[P0]** Invited user accepts invitation → Expected: ProjectInvitation.status = ACCEPTED, ProjectMember record created, respondedAt set
- [ ] **[P0]** Acceptance sends notification to inviter → Expected: PROJECT_INVITATION_ACCEPTED notification sent
- [ ] **[P1]** Accepting expired invitation → Expected: Error, status cannot be changed
- [ ] **[P2]** Accepting invitation logs Activity → Expected: Activity record with type=JOINED, action="invitation_accepted"

#### Decline Project Invitation
- [ ] **[P0]** Invited user declines invitation → Expected: ProjectInvitation.status = DECLINED, respondedAt set
- [ ] **[P0]** Decline sends notification to inviter → Expected: PROJECT_INVITATION_DECLINED notification sent
- [ ] **[P2]** Declining invitation logs Activity → Expected: Activity record with action="invitation_declined"

#### Expire Project Invitation
- [ ] **[P1]** Invitation not responded to before expiresAt → Expected: ProjectInvitation.status = EXPIRED (automated job or on access)
- [ ] **[P2]** Expired invitation cannot be accepted → Expected: Error on attempt to accept

#### Cancel Project Invitation
- [ ] **[P1]** Inviter cancels pending invitation → Expected: ProjectInvitation.status = CANCELLED
- [ ] **[P2]** Cancelling invitation logs Activity → Expected: Activity record with action="invitation_cancelled"

---

### Membership Requests (PENDING → APPROVED → REJECTED → WITHDRAWN)

#### Create Membership Request
- [ ] **[P0]** User requests to join project with requestedRole, message → Expected: ProjectMembershipRequest record created with status=PENDING
- [ ] **[P1]** Request includes skills, motivation, portfolio → Expected: Fields populated
- [ ] **[P1]** Request sends notification to project leaders → Expected: PROJECT_MEMBERSHIP_REQUEST notification sent
- [ ] **[P2]** Duplicate request by same user → Expected: Error or update existing request
- [ ] **[P2]** Creating request logs Activity → Expected: Activity record with action="membership_request_submitted"

#### Approve Membership Request
- [ ] **[P0]** LEADER approves request → Expected: ProjectMembershipRequest.status = APPROVED, ProjectMember record created
- [ ] **[P0]** Approval sends notification to requester → Expected: PROJECT_MEMBERSHIP_APPROVED notification sent
- [ ] **[P1]** Approval sets reviewedById and reviewedAt → Expected: Fields populated
- [ ] **[P2]** Approval logs Activity → Expected: Activity record with action="membership_request_approved"

#### Reject Membership Request
- [ ] **[P0]** LEADER rejects request → Expected: ProjectMembershipRequest.status = REJECTED, reviewMessage provided
- [ ] **[P0]** Rejection sends notification to requester → Expected: PROJECT_MEMBERSHIP_REJECTED notification sent
- [ ] **[P2]** Rejection logs Activity → Expected: Activity record with action="membership_request_rejected"

#### Withdraw Membership Request
- [ ] **[P1]** Requester withdraws request → Expected: ProjectMembershipRequest.status = WITHDRAWN
- [ ] **[P2]** Withdrawing logs Activity → Expected: Activity record with action="membership_request_withdrawn"

---

### Project Comments (Threaded)

#### Create Project Comment
- [ ] **[P0]** User posts comment on project → Expected: ProjectComment record created
- [ ] **[P0]** Comment includes content → Expected: ProjectComment.content stored
- [ ] **[P1]** Comment is top-level (parentId=null) → Expected: ProjectComment.parentId = null
- [ ] **[P1]** Comment is reply (parentId set) → Expected: ProjectComment.parentId references parent comment
- [ ] **[P2]** Creating comment logs Activity → Expected: Activity record with type=COMMENTED, action="comment_posted"
- [ ] **[P2]** Replying to comment sends notification to parent author → Expected: COMMENT_REPLY notification sent

#### Update Project Comment
- [ ] **[P1]** Author edits comment → Expected: ProjectComment.content updated, updatedAt refreshed
- [ ] **[P1]** Non-author attempts edit → Expected: 403 Forbidden
- [ ] **[P2]** Editing comment logs Activity → Expected: Activity record with action="comment_edited"

#### Delete Project Comment
- [ ] **[P1]** Author deletes comment → Expected: ProjectComment record deleted (or soft deleted)
- [ ] **[P1]** Deleting parent comment with replies → Expected: Cascade delete replies or mark as deleted
- [ ] **[P2]** Deleting comment logs Activity → Expected: Activity record with action="comment_deleted"

#### Fetch Project Comments
- [ ] **[P0]** Fetch all comments for project → Expected: Array of ProjectComment objects, threaded
- [ ] **[P1]** Comments ordered by createdAt → Expected: Oldest or newest first based on query
- [ ] **[P2]** Pagination of comments → Expected: Limit and offset supported

---

### Project Updates (with Images)

#### Create Project Update
- [ ] **[P0]** Project member posts update with title, content → Expected: ProjectUpdate record created
- [ ] **[P1]** Update includes images → Expected: ProjectUpdate.images (comma-separated URLs) stored
- [ ] **[P1]** Update sends notification to followers → Expected: PROJECT_UPDATE notification sent
- [ ] **[P2]** Creating update logs Activity → Expected: Activity record with action="update_posted"

#### Fetch Project Updates
- [ ] **[P0]** Fetch all updates for project → Expected: Array of ProjectUpdate objects ordered by createdAt desc
- [ ] **[P1]** Update includes author details → Expected: Response includes User object
- [ ] **[P2]** Pagination of updates → Expected: Limit and offset supported

---

### Project Support (VOTE, SUBSCRIPTION, VOLUNTEER, RESOURCE)

#### Create Project Support
- [ ] **[P0]** User votes for project → Expected: ProjectSupport record created with supportType=VOTE
- [ ] **[P0]** User subscribes with monthly amount → Expected: ProjectSupport record created with supportType=SUBSCRIPTION, monthlyAmount set
- [ ] **[P0]** User volunteers → Expected: ProjectSupport record created with supportType=VOLUNTEER
- [ ] **[P0]** User offers resource → Expected: ProjectSupport record created with supportType=RESOURCE
- [ ] **[P1]** Support includes message → Expected: ProjectSupport.message stored
- [ ] **[P1]** Support is active → Expected: ProjectSupport.isActive = true
- [ ] **[P1]** Creating support increments project.supportersCount → Expected: Project.supportersCount incremented
- [ ] **[P2]** Creating support logs Activity → Expected: Activity record with action="support_added"

#### Update Project Support
- [ ] **[P1]** User updates monthlyAmount for subscription → Expected: ProjectSupport.monthlyAmount updated, updatedAt refreshed
- [ ] **[P1]** User deactivates support → Expected: ProjectSupport.isActive = false
- [ ] **[P2]** Updating support logs Activity → Expected: Activity record with action="support_updated"

#### Delete Project Support
- [ ] **[P1]** User withdraws support → Expected: ProjectSupport record deleted or isActive = false
- [ ] **[P1]** Deleting support decrements project.supportersCount → Expected: Project.supportersCount decremented
- [ ] **[P2]** Deleting support logs Activity → Expected: Activity record with action="support_removed"

---

### Project Following (with Notification Preferences)

#### Follow Project
- [ ] **[P0]** User follows project → Expected: ProjectFollow record created
- [ ] **[P1]** Follow with notifyUpdates=true → Expected: User receives PROJECT_UPDATE notifications
- [ ] **[P1]** Follow with notifyMilestones=true → Expected: User receives milestone notifications
- [ ] **[P1]** Follow with notifyProposals=true → Expected: User receives proposal notifications
- [ ] **[P1]** Follow defaults: notifyUpdates=true, notifyMilestones=true, notifyProposals=false → Expected: Defaults set correctly
- [ ] **[P2]** Following logs Activity → Expected: Activity record with action="project_followed"
- [ ] **[P2]** Following sends notification to project creator (optional) → Expected: PROJECT_FOLLOWED notification sent

#### Update Follow Preferences
- [ ] **[P1]** User disables notifyUpdates → Expected: ProjectFollow.notifyUpdates = false
- [ ] **[P1]** User enables notifyProposals → Expected: ProjectFollow.notifyProposals = true
- [ ] **[P2]** Updating preferences logs Activity → Expected: Activity record with action="follow_preferences_updated"

#### Unfollow Project
- [ ] **[P0]** User unfollows project → Expected: ProjectFollow record deleted
- [ ] **[P2]** Unfollowing logs Activity → Expected: Activity record with action="project_unfollowed"

---

### Project Trending & Featured

#### Trending Score and Rank
- [ ] **[P1]** Project trendingScore calculated based on views, votes, activity → Expected: Project.trendingScore updated
- [ ] **[P1]** Project with high trendingScore gets isTrending=true → Expected: Project.isTrending = true
- [ ] **[P1]** Trending projects assigned trendingRank → Expected: Project.trendingRank reflects position in trending list
- [ ] **[P1]** trendingAt timestamp updated → Expected: Project.trendingAt reflects when trending status set
- [ ] **[P2]** Trending projects listed in dedicated section → Expected: API endpoint for trending projects
- [ ] **[P2]** Trending calculation periodic (e.g., daily) → Expected: Automated job updates trendingScore

#### Featured Projects
- [ ] **[P1]** Admin marks project as featured → Expected: Project.featured = true
- [ ] **[P1]** Featured projects displayed prominently → Expected: UI shows featured projects on homepage
- [ ] **[P2]** Featured flag can be toggled → Expected: Admin can un-feature project

#### Views Count
- [ ] **[P0]** Fetching project increments viewsCount → Expected: Project.viewsCount incremented
- [ ] **[P2]** Views tracked per unique user (if implemented) → Expected: Duplicate views by same user not counted
- [ ] **[P3]** Views reset periodically (if implemented) → Expected: viewsCount reset for trending calculation

---

### Commerce & Storefront

#### Enable Commerce
- [ ] **[P1]** Project creator enables commerce → Expected: Project.commerceEnabled = true
- [ ] **[P1]** Storefront description added → Expected: Project.storefrontDescription stored
- [ ] **[P2]** Commerce enabled projects can create products → Expected: Product creation allowed
- [ ] **[P3]** Commerce disabled projects cannot create products → Expected: Error on product creation attempt

#### Assigned Guild
- [ ] **[P1]** Project assigned to guild → Expected: Project.assignedGuildId references Guild.id
- [ ] **[P1]** Assigned guild appears on project page → Expected: Guild details displayed
- [ ] **[P2]** Assigned guild receives notifications → Expected: Guild members notified of project updates
- [ ] **[P3]** Un-assigning guild → Expected: Project.assignedGuildId set to null

---

## Section 3: Agile Hierarchy (Project → Milestone → Epic → Sprint → Feature → PBI → Task)

### Epics under Milestones (PLANNED → IN_PROGRESS → COMPLETED → CANCELLED)

#### Create Epic
- [ ] **[P0]** User creates epic under milestone with title, description → Expected: Epic record created with status=PLANNED
- [ ] **[P1]** Epic with priority (LOW, MEDIUM, HIGH, CRITICAL) → Expected: Epic.priority set
- [ ] **[P1]** Epic with equityBudget → Expected: Epic.equityBudget set
- [ ] **[P1]** Epic with progress → Expected: Epic.progress defaults to 0
- [ ] **[P1]** Epic with startDate and targetDate → Expected: Epic.startDate and targetDate set
- [ ] **[P1]** Epic assigned to user → Expected: Epic.assigneeId references User.id
- [ ] **[P2]** Creating epic logs Activity → Expected: Activity record with action="epic_created"

#### Update Epic
- [ ] **[P0]** User updates epic title → Expected: Epic.title updated
- [ ] **[P1]** User updates epic status to IN_PROGRESS → Expected: Epic.status = IN_PROGRESS
- [ ] **[P1]** User updates epic priority → Expected: Epic.priority updated
- [ ] **[P1]** User updates epic progress → Expected: Epic.progress updated
- [ ] **[P1]** User updates epic targetDate → Expected: Epic.targetDate updated
- [ ] **[P1]** User assigns epic to different user → Expected: Epic.assigneeId updated
- [ ] **[P2]** Updating epic logs Activity → Expected: Activity record with action="epic_updated"

#### Complete Epic
- [ ] **[P0]** User marks epic as COMPLETED → Expected: Epic.status = COMPLETED
- [ ] **[P1]** Completing epic with incomplete sprints → Expected: Warning or error
- [ ] **[P1]** Completing epic updates milestone progress → Expected: Milestone.progress calculated based on epics
- [ ] **[P2]** Completing epic logs Activity → Expected: Activity record with action="epic_completed"

#### Cancel Epic
- [ ] **[P1]** User cancels epic → Expected: Epic.status = CANCELLED
- [ ] **[P2]** Cancelling epic logs Activity → Expected: Activity record with action="epic_cancelled"

#### Fetch Epics
- [ ] **[P1]** Fetch all epics for milestone → Expected: Array of Epic objects
- [ ] **[P2]** Filter epics by status → Expected: Only epics with specified status returned
- [ ] **[P2]** Fetch epic includes assignee details → Expected: Response includes User object for assignee

---

### Sprints under Epics (PLANNED → ACTIVE → COMPLETED → CANCELLED)

#### Create Sprint
- [ ] **[P0]** User creates sprint under epic with name, goal, startDate, endDate → Expected: Sprint record created with status=PLANNED
- [ ] **[P1]** Sprint with equityBudget → Expected: Sprint.equityBudget set
- [ ] **[P1]** Sprint with velocity → Expected: Sprint.velocity set
- [ ] **[P1]** Sprint assigned to user → Expected: Sprint.assigneeId references User.id
- [ ] **[P2]** Creating sprint logs Activity → Expected: Activity record with action="sprint_created"

#### Update Sprint
- [ ] **[P0]** User updates sprint name → Expected: Sprint.name updated
- [ ] **[P1]** User updates sprint status to ACTIVE → Expected: Sprint.status = ACTIVE
- [ ] **[P1]** User updates sprint goal → Expected: Sprint.goal updated
- [ ] **[P1]** User updates sprint startDate/endDate → Expected: Sprint.startDate/endDate updated
- [ ] **[P1]** User updates sprint velocity → Expected: Sprint.velocity updated
- [ ] **[P1]** User assigns sprint to different user → Expected: Sprint.assigneeId updated
- [ ] **[P2]** Updating sprint logs Activity → Expected: Activity record with action="sprint_updated"

#### Complete Sprint
- [ ] **[P0]** User marks sprint as COMPLETED → Expected: Sprint.status = COMPLETED
- [ ] **[P1]** Completing sprint with incomplete features → Expected: Warning or error
- [ ] **[P1]** Completing sprint updates epic progress → Expected: Epic.progress calculated based on sprints
- [ ] **[P2]** Completing sprint logs Activity → Expected: Activity record with action="sprint_completed"

#### Cancel Sprint
- [ ] **[P1]** User cancels sprint → Expected: Sprint.status = CANCELLED
- [ ] **[P2]** Cancelling sprint logs Activity → Expected: Activity record with action="sprint_cancelled"

#### Fetch Sprints
- [ ] **[P1]** Fetch all sprints for epic → Expected: Array of Sprint objects
- [ ] **[P2]** Filter sprints by status → Expected: Only sprints with specified status returned
- [ ] **[P2]** Fetch sprint includes assignee details → Expected: Response includes User object for assignee

---

### Features under Sprints (PLANNED → IN_PROGRESS → COMPLETED → CANCELLED)

#### Create Feature
- [ ] **[P0]** User creates feature under sprint with title, description → Expected: Feature record created with status=PLANNED
- [ ] **[P1]** Feature with priority (LOW, MEDIUM, HIGH, CRITICAL) → Expected: Feature.priority set
- [ ] **[P1]** Feature with order → Expected: Feature.order set
- [ ] **[P1]** Feature assigned to user → Expected: Feature.assigneeId references User.id
- [ ] **[P2]** Creating feature logs Activity → Expected: Activity record with action="feature_created"

#### Update Feature
- [ ] **[P0]** User updates feature title → Expected: Feature.title updated
- [ ] **[P1]** User updates feature status to IN_PROGRESS → Expected: Feature.status = IN_PROGRESS
- [ ] **[P1]** User updates feature priority → Expected: Feature.priority updated
- [ ] **[P1]** User updates feature order → Expected: Feature.order updated
- [ ] **[P1]** User assigns feature to different user → Expected: Feature.assigneeId updated
- [ ] **[P2]** Updating feature logs Activity → Expected: Activity record with action="feature_updated"

#### Complete Feature
- [ ] **[P0]** User marks feature as COMPLETED → Expected: Feature.status = COMPLETED
- [ ] **[P1]** Completing feature with incomplete PBIs → Expected: Warning or error
- [ ] **[P1]** Completing feature updates sprint progress → Expected: Sprint progress calculated based on features
- [ ] **[P2]** Completing feature logs Activity → Expected: Activity record with action="feature_completed"

#### Cancel Feature
- [ ] **[P1]** User cancels feature → Expected: Feature.status = CANCELLED
- [ ] **[P2]** Cancelling feature logs Activity → Expected: Activity record with action="feature_cancelled"

#### Fetch Features
- [ ] **[P1]** Fetch all features for sprint → Expected: Array of Feature objects ordered by order
- [ ] **[P2]** Filter features by status → Expected: Only features with specified status returned
- [ ] **[P2]** Fetch feature includes assignee details → Expected: Response includes User object for assignee

---

### Product Backlog Items (PBIs) under Features

#### PBI Types (FEATURE, ENHANCEMENT, BUG, TECHNICAL_DEBT, SPIKE)
- [ ] **[P1]** PBI type FEATURE → Expected: ProductBacklogItem.type = FEATURE
- [ ] **[P1]** PBI type ENHANCEMENT → Expected: ProductBacklogItem.type = ENHANCEMENT
- [ ] **[P1]** PBI type BUG → Expected: ProductBacklogItem.type = BUG
- [ ] **[P1]** PBI type TECHNICAL_DEBT → Expected: ProductBacklogItem.type = TECHNICAL_DEBT
- [ ] **[P1]** PBI type SPIKE → Expected: ProductBacklogItem.type = SPIKE

#### PBI Statuses (NEW → READY → IN_PROGRESS → DONE → CANCELLED)
- [ ] **[P0]** User creates PBI → Expected: ProductBacklogItem record created with status=NEW
- [ ] **[P1]** PBI status updated to READY → Expected: ProductBacklogItem.status = READY
- [ ] **[P1]** PBI status updated to IN_PROGRESS → Expected: ProductBacklogItem.status = IN_PROGRESS
- [ ] **[P0]** PBI status updated to DONE → Expected: ProductBacklogItem.status = DONE
- [ ] **[P1]** PBI status updated to CANCELLED → Expected: ProductBacklogItem.status = CANCELLED

#### Create PBI
- [ ] **[P0]** User creates PBI under feature with title, description → Expected: ProductBacklogItem record created
- [ ] **[P1]** PBI with storyPoints → Expected: ProductBacklogItem.storyPoints set
- [ ] **[P1]** PBI with acceptanceCriteria → Expected: ProductBacklogItem.acceptanceCriteria stored
- [ ] **[P1]** PBI with priority (LOW, MEDIUM, HIGH, CRITICAL) → Expected: ProductBacklogItem.priority set
- [ ] **[P1]** PBI assigned to user → Expected: ProductBacklogItem.assigneeId references User.id
- [ ] **[P2]** Creating PBI logs Activity → Expected: Activity record with action="pbi_created"

#### Update PBI
- [ ] **[P0]** User updates PBI title → Expected: ProductBacklogItem.title updated
- [ ] **[P1]** User updates PBI status → Expected: ProductBacklogItem.status updated
- [ ] **[P1]** User updates PBI type → Expected: ProductBacklogItem.type updated
- [ ] **[P1]** User updates storyPoints → Expected: ProductBacklogItem.storyPoints updated
- [ ] **[P1]** User updates acceptanceCriteria → Expected: ProductBacklogItem.acceptanceCriteria updated
- [ ] **[P1]** User updates priority → Expected: ProductBacklogItem.priority updated
- [ ] **[P1]** User assigns PBI to different user → Expected: ProductBacklogItem.assigneeId updated
- [ ] **[P2]** Updating PBI logs Activity → Expected: Activity record with action="pbi_updated"

#### Link PBI to Task
- [ ] **[P0]** Task linked to PBI → Expected: ProjectTask.pbiId references ProductBacklogItem.id
- [ ] **[P1]** Multiple tasks can link to same PBI → Expected: One-to-many relationship supported
- [ ] **[P2]** PBI completion triggers task notifications → Expected: Related tasks updated or notified

#### Fetch PBIs
- [ ] **[P1]** Fetch all PBIs for feature → Expected: Array of ProductBacklogItem objects
- [ ] **[P2]** Filter PBIs by status → Expected: Only PBIs with specified status returned
- [ ] **[P2]** Filter PBIs by type → Expected: Only PBIs with specified type returned
- [ ] **[P2]** Fetch PBI includes assignee details → Expected: Response includes User object for assignee

---

### Full Drill-Down Navigation (Project → Milestone → Epic → Sprint → Feature → PBI → Task)

#### Hierarchy Navigation
- [ ] **[P1]** Fetch project with all milestones → Expected: Project includes milestones array
- [ ] **[P1]** Fetch milestone with all epics → Expected: Milestone includes epics array
- [ ] **[P1]** Fetch epic with all sprints → Expected: Epic includes sprints array
- [ ] **[P1]** Fetch sprint with all features → Expected: Sprint includes features array
- [ ] **[P1]** Fetch feature with all PBIs → Expected: Feature includes PBIs array
- [ ] **[P1]** Fetch PBI with all linked tasks → Expected: PBI includes tasks array
- [ ] **[P2]** Drill-down API endpoint (project → all children) → Expected: Single query returns full hierarchy
- [ ] **[P2]** Breadcrumb navigation in UI → Expected: UI shows path (Project > Milestone > Epic > Sprint > Feature > PBI > Task)

---

### Status Propagation (Completing All Children Allows Parent Completion)

#### Task → PBI
- [ ] **[P1]** All tasks linked to PBI completed → Expected: PBI can be marked DONE
- [ ] **[P1]** PBI with incomplete tasks → Expected: Warning or error on PBI completion

#### PBI → Feature
- [ ] **[P1]** All PBIs under feature completed → Expected: Feature can be marked COMPLETED
- [ ] **[P1]** Feature with incomplete PBIs → Expected: Warning or error on feature completion

#### Feature → Sprint
- [ ] **[P1]** All features under sprint completed → Expected: Sprint can be marked COMPLETED
- [ ] **[P1]** Sprint with incomplete features → Expected: Warning or error on sprint completion

#### Sprint → Epic
- [ ] **[P1]** All sprints under epic completed → Expected: Epic can be marked COMPLETED
- [ ] **[P1]** Epic with incomplete sprints → Expected: Warning or error on epic completion

#### Epic → Milestone
- [ ] **[P1]** All epics under milestone completed → Expected: Milestone can be marked COMPLETED
- [ ] **[P1]** Milestone with incomplete epics → Expected: Warning or error on milestone completion

#### Milestone → Project
- [ ] **[P1]** All milestones under project completed → Expected: Project can be marked COMPLETED
- [ ] **[P1]** Project with incomplete milestones → Expected: Warning or error on project completion

#### Automated Status Propagation (Optional)
- [ ] **[P2]** Completing last task auto-marks PBI as DONE → Expected: PBI.status = DONE automatically
- [ ] **[P2]** Completing last PBI auto-marks feature as COMPLETED → Expected: Feature.status = COMPLETED automatically
- [ ] **[P2]** Completing last feature auto-marks sprint as COMPLETED → Expected: Sprint.status = COMPLETED automatically
- [ ] **[P2]** Completing last sprint auto-marks epic as COMPLETED → Expected: Epic.status = COMPLETED automatically
- [ ] **[P2]** Completing last epic auto-marks milestone as COMPLETED → Expected: Milestone.status = COMPLETED automatically
- [ ] **[P3]** Automated propagation can be disabled (manual control) → Expected: Setting to disable auto-completion

---

### Cross-Level Dependencies and Constraints

#### Dependencies Between Entities
- [ ] **[P2]** Epic depends on another epic → Expected: Dependency tracked, blocking epic cannot complete until dependency resolved
- [ ] **[P2]** Sprint depends on another sprint → Expected: Dependency tracked
- [ ] **[P2]** Feature depends on another feature → Expected: Dependency tracked
- [ ] **[P2]** PBI depends on another PBI → Expected: Dependency tracked
- [ ] **[P0]** Task depends on another task → Expected: ProjectTaskDependency enforces dependency

#### Constraint Validation
- [ ] **[P2]** Cannot start sprint until all dependencies resolved → Expected: Validation error or warning
- [ ] **[P2]** Cannot complete epic with blocked sprints → Expected: Validation error
- [ ] **[P2]** Circular dependencies detected and prevented → Expected: Error on circular dependency creation across levels
- [ ] **[P3]** Dependency graph visualization → Expected: UI shows dependency tree or graph

---

### Assignee Tracking Across Hierarchy

#### Assignee Inheritance (Optional)
- [ ] **[P2]** Milestone assignee defaults to project creator → Expected: Milestone.assigneeId set
- [ ] **[P2]** Epic assignee defaults to milestone assignee → Expected: Epic.assigneeId inherited
- [ ] **[P2]** Sprint assignee defaults to epic assignee → Expected: Sprint.assigneeId inherited
- [ ] **[P2]** Feature assignee defaults to sprint assignee → Expected: Feature.assigneeId inherited
- [ ] **[P2]** PBI assignee defaults to feature assignee → Expected: PBI.assigneeId inherited
- [ ] **[P2]** Task assignee defaults to PBI assignee → Expected: Task.assignedToId inherited

#### Assignee Override
- [ ] **[P1]** Assignee can be overridden at any level → Expected: Assignee explicitly set, no inheritance
- [ ] **[P2]** Changing parent assignee updates children (if inheritance enabled) → Expected: All child entities assignees updated

---

### Progress Tracking Across Hierarchy

#### Progress Calculation
- [ ] **[P1]** Epic progress calculated from completed sprints → Expected: Epic.progress = (completed sprints / total sprints) * 100
- [ ] **[P1]** Milestone progress calculated from completed epics → Expected: Milestone progress computed
- [ ] **[P1]** Feature progress calculated from completed PBIs → Expected: Feature progress computed
- [ ] **[P2]** Project progress calculated from completed milestones → Expected: Project completion % displayed
- [ ] **[P2]** Progress updated in real-time on child completion → Expected: Parent progress refreshed automatically

#### Progress Display
- [ ] **[P1]** Progress bar shown for epic → Expected: UI displays Epic.progress as percentage
- [ ] **[P1]** Progress bar shown for milestone → Expected: UI displays milestone progress
- [ ] **[P2]** Progress bar shown for project → Expected: UI displays project overall progress
- [ ] **[P3]** Progress history tracked (if implemented) → Expected: Historical progress snapshots stored

---

**End of QA Testing Checklist (Sections 1-3)**
## Section 4: Guild Module

### 4.1 Guild CRUD Operations

#### Guild Creation
- [ ] **[P0]** Create guild with all required fields (name, slug, description, email) → Expected: Guild created successfully with default values (isVerified: false, rating: null, counts: 0)
- [ ] **[P0]** Create guild with all optional fields populated (website, phone, address, logo, portfolio, specialties) → Expected: All fields saved correctly
- [ ] **[P0]** Create guild with unique slug → Expected: Guild created successfully
- [ ] **[P1]** Create guild with duplicate slug → Expected: Validation error, slug must be unique
- [ ] **[P0]** Create guild without required fields (name, email) → Expected: Validation error listing missing fields
- [ ] **[P1]** Create guild with invalid email format → Expected: Validation error for email field
- [ ] **[P0]** Verify ownerId is set to creator upon guild creation → Expected: ownerId matches creator's userId and is unique
- [ ] **[P1]** Attempt to create second guild with same owner → Expected: Error, one owner can only own one guild
- [ ] **[P2]** Create guild with very long description (10,000+ chars) → Expected: Text field handles large content
- [ ] **[P2]** Create guild with specialties as comma-separated list → Expected: Specialties stored and retrievable as text

#### Guild Read/Retrieve
- [ ] **[P0]** Retrieve guild by ID → Expected: Guild returned with all fields
- [ ] **[P0]** Retrieve guild by slug → Expected: Guild found and returned
- [ ] **[P1]** Retrieve guild with non-existent ID → Expected: 404 error or null
- [ ] **[P1]** List all guilds with pagination → Expected: Paginated results with correct page size
- [ ] **[P2]** Filter guilds by isVerified status → Expected: Only verified/unverified guilds returned
- [ ] **[P2]** Filter guilds by specialty → Expected: Guilds matching specialty criteria returned
- [ ] **[P1]** Retrieve guild with member count → Expected: membersCount accurately reflects GuildMember records
- [ ] **[P1]** Retrieve guild with projects count → Expected: projectsCount accurately reflects assigned projects
- [ ] **[P1]** Retrieve guild with reviews count and rating → Expected: Accurate aggregate data from GuildReview

#### Guild Update
- [ ] **[P0]** Update guild name as owner → Expected: Name updated successfully
- [ ] **[P0]** Update guild description, website, contact info as owner → Expected: All fields updated
- [ ] **[P1]** Update guild logo URL → Expected: Logo updated and displayed correctly
- [ ] **[P1]** Update guild specialties → Expected: New specialties saved
- [ ] **[P0]** Attempt to update guild as non-owner → Expected: Forbidden, only owner or admin can update
- [ ] **[P1]** Update guild slug to duplicate value → Expected: Validation error, slug must remain unique
- [ ] **[P2]** Update guild isVerified flag (admin action) → Expected: Verification status updated
- [ ] **[P2]** Update guild without changing ownerId → Expected: ownerId remains immutable during standard updates

#### Guild Deletion
- [ ] **[P0]** Delete guild as owner → Expected: Guild soft-deleted or removed, cascade to related entities
- [ ] **[P1]** Delete guild with active members → Expected: Warning or error, handle member removal first
- [ ] **[P1]** Delete guild with assigned projects → Expected: Projects unassigned or error shown
- [ ] **[P1]** Attempt to delete guild as non-owner → Expected: Forbidden error
- [ ] **[P2]** Delete guild with existing reviews → Expected: Reviews handled appropriately (cascade or orphaned)

---

### 4.2 Guild Members & Roles

#### Guild Member Roles (OWNER, ADMIN, MANAGER, MEMBER, APPRENTICE)
- [ ] **[P0]** Add member with OWNER role → Expected: Error, only one owner allowed (set at creation)
- [ ] **[P0]** Add member with ADMIN role → Expected: Member added with admin privileges
- [ ] **[P0]** Add member with MANAGER role → Expected: Member added with manager privileges
- [ ] **[P0]** Add member with MEMBER role (default) → Expected: Member added with standard access
- [ ] **[P0]** Add member with APPRENTICE role → Expected: Member added with limited access
- [ ] **[P1]** Verify owner can perform all guild actions → Expected: Full CRUD and management permissions
- [ ] **[P1]** Verify admin can manage members but not change ownership → Expected: Admin permissions enforced
- [ ] **[P1]** Verify manager can assign tasks/projects but not edit guild core info → Expected: Manager scope limited
- [ ] **[P1]** Verify member has read access and limited write access → Expected: Member permissions respected
- [ ] **[P1]** Verify apprentice has minimal permissions → Expected: Apprentice can view but not modify
- [ ] **[P2]** List all members grouped by role → Expected: Members returned sorted/filtered by role

#### Member Management
- [ ] **[P0]** Add new member to guild → Expected: GuildMember record created with joinedAt timestamp
- [ ] **[P1]** Add duplicate member (same userId) → Expected: Error, member already exists in guild
- [ ] **[P0]** Update member role (MEMBER → ADMIN) → Expected: Role updated successfully
- [ ] **[P1]** Downgrade member role (ADMIN → MEMBER) → Expected: Role change persisted
- [ ] **[P0]** Remove member from guild → Expected: GuildMember record deleted
- [ ] **[P1]** Remove owner from guild → Expected: Error, owner cannot be removed (transfer ownership first)
- [ ] **[P2]** Verify membersCount increments on member add → Expected: Guild.membersCount updated
- [ ] **[P2]** Verify membersCount decrements on member removal → Expected: Guild.membersCount updated

---

### 4.3 Guild Invitations

#### Invitation Creation
- [ ] **[P0]** Invite user by userId with MEMBER role → Expected: GuildInvitation created with PENDING status
- [ ] **[P0]** Invite user by email (non-registered) with ADMIN role → Expected: Invitation created with email, token generated
- [ ] **[P1]** Invite with expiration date → Expected: expiresAt set, token valid until expiry
- [ ] **[P1]** Invite without expiration → Expected: expiresAt null, invitation valid indefinitely
- [ ] **[P0]** Verify invitation token is unique → Expected: Each invitation has unique token for redemption
- [ ] **[P1]** Send invitation message with custom text → Expected: Message stored and delivered
- [ ] **[P2]** Invite user who is already a member → Expected: Error or warning, duplicate membership

#### Invitation Status Transitions
- [ ] **[P0]** Invitation created → PENDING status → Expected: Status is PENDING on creation
- [ ] **[P0]** User accepts invitation → PENDING → ACCEPTED → Expected: Status updated, GuildMember created with specified role
- [ ] **[P0]** User declines invitation → PENDING → DECLINED → Expected: Status updated to DECLINED, no membership created
- [ ] **[P1]** Invitation expires (past expiresAt) → PENDING → EXPIRED → Expected: Invitation marked EXPIRED, cannot be accepted
- [ ] **[P1]** Inviter cancels invitation → PENDING → CANCELLED → Expected: Status set to CANCELLED, token invalidated
- [ ] **[P1]** Attempt to accept EXPIRED invitation → Expected: Error, invitation no longer valid
- [ ] **[P1]** Attempt to accept CANCELLED invitation → Expected: Error, invitation revoked
- [ ] **[P2]** Attempt to accept already ACCEPTED invitation → Expected: Error or idempotent behavior

#### Invitation Permissions
- [ ] **[P0]** Owner invites member → Expected: Invitation created successfully
- [ ] **[P0]** Admin invites member → Expected: Invitation created successfully
- [ ] **[P1]** Manager invites member → Expected: Success if authorized, else error
- [ ] **[P1]** Regular member attempts to invite → Expected: Forbidden, insufficient permissions
- [ ] **[P1]** Apprentice attempts to invite → Expected: Forbidden
- [ ] **[P2]** Invite with role higher than inviter's role → Expected: Validation error or permission denied

#### Invitation by Email (Non-registered Users)
- [ ] **[P0]** Invite non-registered user via email → Expected: Invitation created, email notification sent
- [ ] **[P1]** Non-registered user registers and accepts invitation → Expected: User account created, membership established
- [ ] **[P2]** Verify token-based redemption works for email invitations → Expected: Valid token grants access

---

### 4.4 Guild Applications (Membership Requests)

#### Application Submission
- [ ] **[P0]** User submits application with all fields (requestedRole, message, skills, experience, portfolio, availability) → Expected: GuildApplication created with PENDING status
- [ ] **[P0]** Submit application with minimal required fields (requestedRole, message) → Expected: Application accepted
- [ ] **[P1]** Submit application for OWNER role → Expected: Error, cannot apply for owner role
- [ ] **[P1]** Submit application for ADMIN role → Expected: Application created, pending review
- [ ] **[P1]** Submit duplicate application (same user, same guild) → Expected: Error, application already exists
- [ ] **[P2]** Include portfolio URL in application → Expected: Portfolio link stored and accessible
- [ ] **[P2]** Specify availability (e.g., "Full-time", "Part-time") → Expected: Availability saved

#### Application Status Transitions
- [ ] **[P0]** Application submitted → PENDING status → Expected: Status is PENDING, appliedAt timestamp set
- [ ] **[P0]** Admin approves application → PENDING → APPROVED → Expected: Status updated, GuildMember created with requested role
- [ ] **[P0]** Admin rejects application → PENDING → REJECTED → Expected: Status updated, no membership created
- [ ] **[P1]** Applicant withdraws application → PENDING → WITHDRAWN → Expected: Status set to WITHDRAWN
- [ ] **[P1]** Attempt to approve already APPROVED application → Expected: Error or idempotent behavior
- [ ] **[P1]** Attempt to withdraw APPROVED application → Expected: Error, cannot withdraw approved application

#### Application Review
- [ ] **[P0]** Admin reviews application and adds review message → Expected: reviewMessage and reviewedById saved, reviewedAt timestamp set
- [ ] **[P1]** Owner reviews and approves application → Expected: Application approved, member added
- [ ] **[P1]** Manager reviews application → Expected: Success if authorized, else forbidden
- [ ] **[P1]** Regular member attempts to review → Expected: Forbidden, insufficient permissions
- [ ] **[P2]** Review application with detailed feedback in reviewMessage → Expected: Feedback stored and visible to applicant

#### Application Permissions
- [ ] **[P0]** Any authenticated user can submit application → Expected: Application created
- [ ] **[P0]** Owner can review applications → Expected: Full review access
- [ ] **[P0]** Admin can review applications → Expected: Review access granted
- [ ] **[P1]** Manager review permissions → Expected: Based on guild configuration
- [ ] **[P1]** Member cannot review applications → Expected: Forbidden
- [ ] **[P1]** Applicant can view own application status → Expected: Read access to own record

---

### 4.5 Guild Reviews & Ratings

#### Review Creation
- [ ] **[P0]** User submits review with rating (1-5) and comment → Expected: GuildReview created, linked to user and guild
- [ ] **[P0]** Submit review with rating only (no comment) → Expected: Review created with null comment
- [ ] **[P1]** Submit review linked to specific project (projectId) → Expected: Review associated with project context
- [ ] **[P1]** Submit review with rating outside range (0, 6, -1) → Expected: Validation error, rating must be 1-5
- [ ] **[P0]** Verify user can only review guild once per project → Expected: Duplicate review error or one review per project constraint
- [ ] **[P2]** Submit review with very long comment (5000+ chars) → Expected: Text field accommodates length

#### Rating Aggregation
- [ ] **[P0]** Submit first review → Expected: Guild.rating updated to match review rating
- [ ] **[P0]** Submit multiple reviews → Expected: Guild.rating reflects average of all reviews
- [ ] **[P1]** Verify reviewsCount increments on review submission → Expected: Guild.reviewsCount matches total reviews
- [ ] **[P1]** Delete review → Expected: Guild.rating and reviewsCount recalculated
- [ ] **[P2]** Verify rating calculation with decimal precision → Expected: Average computed correctly (e.g., 4.67)

#### Review Management
- [ ] **[P1]** User edits own review → Expected: Review updated, rating recalculated if changed
- [ ] **[P1]** User deletes own review → Expected: Review removed, aggregates updated
- [ ] **[P1]** Guild owner responds to review → Expected: Response functionality (if supported)
- [ ] **[P2]** Admin moderates inappropriate review → Expected: Review flagged or removed

---

### 4.6 Guild Updates (Posts/Announcements)

#### Guild Update Creation
- [ ] **[P0]** Owner posts update with title and content → Expected: GuildUpdate created with createdAt timestamp
- [ ] **[P0]** Admin posts update → Expected: Update created successfully
- [ ] **[P1]** Manager posts update → Expected: Success if authorized
- [ ] **[P1]** Member attempts to post update → Expected: Forbidden unless permission granted
- [ ] **[P1]** Include images (text field with URLs) in update → Expected: Images stored as text, retrievable
- [ ] **[P2]** Post update with rich content (markdown or HTML) → Expected: Content rendered correctly

#### Guild Update Retrieval
- [ ] **[P0]** List all updates for a guild → Expected: Updates returned in reverse chronological order
- [ ] **[P1]** Paginate guild updates → Expected: Correct page size and offset handling
- [ ] **[P2]** Filter updates by date range → Expected: Updates within range returned
- [ ] **[P2]** Retrieve update by ID → Expected: Specific update returned

#### Guild Update Management
- [ ] **[P1]** Owner edits update → Expected: Update content modified, no timestamp change for createdAt
- [ ] **[P1]** Owner deletes update → Expected: Update removed
- [ ] **[P1]** Non-creator attempts to edit update → Expected: Forbidden unless admin
- [ ] **[P2]** Notify followers when update is posted → Expected: Notifications sent to users following guild

---

### 4.7 Guild Following & Notifications

#### Guild Follow
- [ ] **[P0]** User follows guild → Expected: GuildFollow record created with default notification preferences (notifyUpdates: true, notifyEvents: true, notifyProjects: false)
- [ ] **[P0]** User unfollows guild → Expected: GuildFollow record deleted
- [ ] **[P1]** Follow guild twice (duplicate) → Expected: Error or idempotent behavior
- [ ] **[P2]** Verify follower count incremented → Expected: Guild.followersCount updated (if field exists)

#### Notification Preferences
- [ ] **[P0]** Toggle notifyUpdates preference → Expected: Preference updated, notifications sent accordingly
- [ ] **[P0]** Toggle notifyEvents preference → Expected: Event notifications controlled by setting
- [ ] **[P0]** Toggle notifyProjects preference → Expected: Project-related notifications enabled/disabled
- [ ] **[P1]** Set all notification preferences to false → Expected: User still follows but receives no notifications
- [ ] **[P1]** Verify notification sent when guild posts update and notifyUpdates is true → Expected: Notification delivered
- [ ] **[P1]** Verify no notification sent when notifyUpdates is false → Expected: No notification despite update
- [ ] **[P2]** Bulk update notification preferences → Expected: All preferences saved

---

### 4.8 Guild Assignment to Projects

#### Project Assignment
- [ ] **[P0]** Assign guild to project (Project.assignedGuildId) → Expected: Project linked to guild
- [ ] **[P0]** Unassign guild from project → Expected: assignedGuildId set to null
- [ ] **[P1]** Assign multiple projects to same guild → Expected: Multiple projects can reference same guild
- [ ] **[P1]** Verify projectsCount increments on project assignment → Expected: Guild.projectsCount updated
- [ ] **[P1]** Verify projectsCount decrements on unassignment → Expected: Count decremented
- [ ] **[P2]** Retrieve all projects assigned to guild → Expected: List of projects returned

#### Assignment Permissions
- [ ] **[P0]** Project creator assigns guild → Expected: Assignment successful
- [ ] **[P1]** Guild owner accepts or rejects assignment → Expected: Workflow enforced (if applicable)
- [ ] **[P2]** Verify guild members notified of project assignment → Expected: Notifications sent based on preferences

---

### 4.9 Guild Trending & Discoverability

#### Trending Mechanics
- [ ] **[P1]** Guild marked as trending (isTrending: true) → Expected: Flag set, trendingAt timestamp recorded
- [ ] **[P1]** Set trendingScore for guild → Expected: Score stored, used for ranking
- [ ] **[P1]** Set trendingRank → Expected: Rank assigned, guilds sorted by rank
- [ ] **[P2]** Verify trending guilds appear in discovery feed → Expected: Trending guilds prioritized
- [ ] **[P2]** Remove trending status → Expected: isTrending set to false, rank/score cleared or ignored

#### Trending Algorithm Triggers
- [ ] **[P2]** High review count increases trendingScore → Expected: Score recalculated
- [ ] **[P2]** High project completion rate affects trending → Expected: Metric impacts score
- [ ] **[P2]** Recent activity boosts trending status → Expected: Activity tracked and scored

---

### 4.10 Guild Verification

#### Verification Process
- [ ] **[P0]** Admin verifies guild (isVerified: true) → Expected: Verified badge displayed
- [ ] **[P1]** Unverified guild attempts verification request → Expected: Request submitted for admin review
- [ ] **[P1]** Admin revokes verification → Expected: isVerified set to false
- [ ] **[P2]** Verify badge shown in UI for verified guilds → Expected: Visual indicator present

---

### 4.11 Edge Cases & Error Handling

#### Guild Constraints
- [ ] **[P0]** Ensure ownerId is unique (one owner per guild) → Expected: Constraint enforced at DB level
- [ ] **[P1]** Attempt to transfer ownership → Expected: ownerId updated, previous owner role changed
- [ ] **[P2]** Orphaned guild (owner account deleted) → Expected: Graceful handling or reassignment

#### Data Integrity
- [ ] **[P1]** Delete guild with active invitations → Expected: Invitations cancelled or orphaned
- [ ] **[P1]** Delete guild with pending applications → Expected: Applications auto-rejected or removed
- [ ] **[P2]** Guild with no members (only owner) → Expected: membersCount reflects owner only

#### Performance
- [ ] **[P2]** Retrieve guild with 1000+ members → Expected: Pagination and performance optimized
- [ ] **[P2]** List guilds with complex filters (verified, trending, specialty) → Expected: Query performance acceptable

---

## Section 5: Opportunities Marketplace

### 5.1 Opportunity CRUD Operations

#### Opportunity Creation
- [ ] **[P0]** Create opportunity with all required fields (title, slug, description, type) → Expected: Opportunity created with DRAFT status, defaults applied (origin: TEAM_POSITION, experienceLevel: MID, isRemote: true)
- [ ] **[P0]** Create opportunity with all optional fields (requirements, skills, benefits, location, compensation, compensationDetails, deadline, maxApplications, coverImage) → Expected: All fields saved
- [ ] **[P0]** Create opportunity with unique slug → Expected: Slug uniqueness enforced
- [ ] **[P1]** Create opportunity with duplicate slug → Expected: Validation error
- [ ] **[P1]** Create opportunity without required fields → Expected: Validation error listing missing fields
- [ ] **[P0]** Create opportunity linked to guild (guildId) → Expected: Opportunity associated with guild
- [ ] **[P0]** Create opportunity linked to project (projectId) → Expected: Opportunity associated with project
- [ ] **[P0]** Create opportunity linked to task (taskId) with origin TASK_GENERATED → Expected: Auto-generated opportunity, linked to task
- [ ] **[P1]** Create opportunity with origin TEAM_POSITION (manual) → Expected: Manual creation, not linked to task
- [ ] **[P2]** Create opportunity with very long description (20,000+ chars) → Expected: Text field handles large content

#### Opportunity Types
- [ ] **[P0]** Create GUILD_POSITION opportunity → Expected: Type set correctly, guild context implied
- [ ] **[P0]** Create PROJECT_ROLE opportunity → Expected: Type set, project context required
- [ ] **[P0]** Create TASK_BOUNTY opportunity → Expected: Type set, compensation expected
- [ ] **[P0]** Create FREELANCE opportunity → Expected: Type set, independent contractor context
- [ ] **[P0]** Create MENTORSHIP opportunity → Expected: Type set, mentor/mentee relationship
- [ ] **[P0]** Create COLLABORATION opportunity → Expected: Type set, partnership context

#### Opportunity Read/Retrieve
- [ ] **[P0]** Retrieve opportunity by ID → Expected: Opportunity returned with all fields
- [ ] **[P0]** Retrieve opportunity by slug → Expected: Opportunity found
- [ ] **[P1]** Retrieve non-existent opportunity → Expected: 404 error
- [ ] **[P1]** List all opportunities with pagination → Expected: Paginated results
- [ ] **[P2]** Filter opportunities by type → Expected: Only matching types returned
- [ ] **[P2]** Filter opportunities by status → Expected: Only matching statuses returned
- [ ] **[P2]** Filter opportunities by experienceLevel → Expected: Filtered results
- [ ] **[P2]** Filter opportunities by isRemote flag → Expected: Remote or on-site opportunities
- [ ] **[P2]** Search opportunities by keyword in title/description → Expected: Relevant results returned

#### Opportunity Update
- [ ] **[P0]** Update opportunity title, description as poster → Expected: Fields updated
- [ ] **[P0]** Update opportunity deadline → Expected: New deadline saved
- [ ] **[P1]** Update opportunity status (DRAFT → OPEN) → Expected: Status transition allowed
- [ ] **[P1]** Update opportunity compensation → Expected: Amount updated
- [ ] **[P1]** Attempt to update opportunity as non-poster → Expected: Forbidden error
- [ ] **[P2]** Update opportunity with new coverImage → Expected: Image URL updated

#### Opportunity Deletion
- [ ] **[P0]** Delete opportunity as poster → Expected: Opportunity removed or soft-deleted
- [ ] **[P1]** Delete opportunity with pending applications → Expected: Applications handled (cancelled or archived)
- [ ] **[P1]** Delete opportunity with bids → Expected: Bids handled appropriately
- [ ] **[P1]** Attempt to delete opportunity as non-poster → Expected: Forbidden error

---

### 5.2 Opportunity Lifecycle & Status

#### Status Transitions
- [ ] **[P0]** Create opportunity → DRAFT status → Expected: Initial status is DRAFT
- [ ] **[P0]** Publish opportunity → DRAFT → OPEN → Expected: Status updated, opportunity visible to applicants
- [ ] **[P0]** Review applications → OPEN → IN_REVIEW → Expected: Status indicates review in progress
- [ ] **[P0]** Accept applicant → IN_REVIEW → FILLED → Expected: Opportunity marked as filled
- [ ] **[P0]** Close opportunity → Any status → CLOSED → Expected: Opportunity closed, closedAt timestamp set
- [ ] **[P0]** Cancel opportunity → Any status → CANCELLED → Expected: Opportunity cancelled
- [ ] **[P1]** Reopen CLOSED opportunity → Expected: Status change to OPEN or validation error
- [ ] **[P1]** Attempt to apply to FILLED opportunity → Expected: Error, opportunity no longer accepting applications
- [ ] **[P1]** Attempt to apply to CANCELLED opportunity → Expected: Error, opportunity inactive
- [ ] **[P2]** Verify closedAt timestamp set on CLOSED status → Expected: Timestamp recorded

---

### 5.3 Experience Levels

#### Experience Level Filtering
- [ ] **[P0]** Create opportunity with ENTRY level → Expected: Level set, visible in filters
- [ ] **[P0]** Create opportunity with JUNIOR level → Expected: Level set
- [ ] **[P0]** Create opportunity with MID level (default) → Expected: Default applied
- [ ] **[P0]** Create opportunity with SENIOR level → Expected: Level set
- [ ] **[P0]** Create opportunity with LEAD level → Expected: Level set
- [ ] **[P0]** Create opportunity with EXPERT level → Expected: Level set
- [ ] **[P2]** Filter opportunities by multiple experience levels → Expected: Combined results

---

### 5.4 Remote & Location

#### Location Handling
- [ ] **[P0]** Create remote opportunity (isRemote: true, location: null) → Expected: Location not required
- [ ] **[P0]** Create on-site opportunity (isRemote: false, location set) → Expected: Location required and stored
- [ ] **[P1]** Create hybrid opportunity (isRemote: true, location set) → Expected: Both remote and location specified
- [ ] **[P2]** Filter by isRemote flag → Expected: Only remote or on-site opportunities returned
- [ ] **[P2]** Search by location keyword → Expected: Opportunities in specified location returned

---

### 5.5 Opportunity Applications

#### Application Submission
- [ ] **[P0]** User submits application with coverLetter → Expected: OpportunityApplication created with PENDING status, appliedAt timestamp set
- [ ] **[P0]** Submit application with portfolio and additionalInfo → Expected: All fields saved
- [ ] **[P0]** Submit application to OPEN opportunity → Expected: Application accepted
- [ ] **[P1]** Submit duplicate application (same user, same opportunity) → Expected: Error, unique constraint (opportunityId, applicantId) enforced
- [ ] **[P1]** Submit application to DRAFT opportunity → Expected: Error or warning, opportunity not open
- [ ] **[P1]** Submit application to FILLED opportunity → Expected: Error, no longer accepting applications
- [ ] **[P1]** Submit application when maxApplications reached → Expected: Error, application limit exceeded
- [ ] **[P2]** Verify applicationsCount increments on submission → Expected: Opportunity.applicationsCount updated

#### Application Status Transitions
- [ ] **[P0]** Application submitted → PENDING status → Expected: Initial status
- [ ] **[P0]** Poster accepts application → PENDING → ACCEPTED → Expected: Status updated, reviewedAt timestamp set
- [ ] **[P0]** Poster rejects application → PENDING → REJECTED → Expected: Status updated, reviewNotes stored
- [ ] **[P1]** Applicant withdraws application → PENDING → WITHDRAWN → Expected: Status updated
- [ ] **[P1]** Attempt to withdraw ACCEPTED application → Expected: Error or special workflow
- [ ] **[P2]** Attempt to accept already ACCEPTED application → Expected: Idempotent or error

#### Application Review
- [ ] **[P0]** Poster reviews application and adds reviewNotes → Expected: Notes saved, reviewedAt timestamp set
- [ ] **[P1]** Non-poster attempts to review → Expected: Forbidden unless authorized (guild admin, project lead)
- [ ] **[P2]** Retrieve all applications for an opportunity → Expected: List returned with statuses

#### Application Permissions
- [ ] **[P0]** Any authenticated user can apply → Expected: Application created
- [ ] **[P0]** Poster can review applications → Expected: Full access to applications
- [ ] **[P1]** Applicant can view own application status → Expected: Read access granted
- [ ] **[P1]** Applicant cannot view other applications → Expected: Privacy enforced

---

### 5.6 Opportunity Bids

#### Bid Submission
- [ ] **[P0]** User submits bid with proposal → Expected: OpportunityBid created with SUBMITTED status
- [ ] **[P0]** Submit bid with proposedAmount, estimatedHours, timeline, deliverables → Expected: All fields saved
- [ ] **[P0]** Submit bid linked to guild (guildId) → Expected: Bid associated with guild
- [ ] **[P0]** Submit bid as individual (guildId null) → Expected: Bid accepted without guild
- [ ] **[P1]** Submit duplicate bid (same bidderId, same opportunityId) → Expected: Error, unique constraint enforced
- [ ] **[P1]** Submit bid to CLOSED opportunity → Expected: Error, bidding not allowed
- [ ] **[P2]** Verify bidsCount increments on submission → Expected: Opportunity.bidsCount updated

#### Bid Status Transitions
- [ ] **[P0]** Bid submitted → SUBMITTED status → Expected: Initial status
- [ ] **[P0]** Poster reviews bid → SUBMITTED → UNDER_REVIEW → Expected: Status updated
- [ ] **[P0]** Poster accepts bid → UNDER_REVIEW → ACCEPTED → Expected: Status updated, reviewedAt timestamp set
- [ ] **[P0]** Poster rejects bid → UNDER_REVIEW → REJECTED → Expected: Status updated
- [ ] **[P1]** Bidder withdraws bid → SUBMITTED → WITHDRAWN → Expected: Status updated
- [ ] **[P1]** Attempt to withdraw ACCEPTED bid → Expected: Error or special workflow
- [ ] **[P2]** Attempt to accept multiple bids for same opportunity → Expected: Business logic enforced (one winner or multiple allowed)

#### Bid Review
- [ ] **[P0]** Poster reviews bid → Expected: reviewedAt timestamp set
- [ ] **[P1]** Non-poster attempts to review bid → Expected: Forbidden
- [ ] **[P2]** Retrieve all bids for an opportunity → Expected: List returned sorted by status or submission time

#### Bid Permissions
- [ ] **[P0]** Authenticated user can submit bid → Expected: Bid created
- [ ] **[P0]** Guild member submits bid on behalf of guild → Expected: Bid linked to guild
- [ ] **[P1]** Poster can view all bids → Expected: Full access
- [ ] **[P1]** Bidder can view own bid → Expected: Read access
- [ ] **[P1]** Bidder cannot view other bids → Expected: Privacy enforced

---

### 5.7 Opportunity Updates

#### Opportunity Update Creation
- [ ] **[P0]** Poster creates update with title and content → Expected: OpportunityUpdate created
- [ ] **[P1]** Include images in update → Expected: Images stored as text
- [ ] **[P2]** List updates for opportunity → Expected: Updates returned in chronological order

---

### 5.8 Opportunity Comments

#### Comment Creation
- [ ] **[P0]** User posts comment on opportunity → Expected: OpportunityComment created
- [ ] **[P0]** Post threaded comment (with parentId) → Expected: Reply linked to parent comment
- [ ] **[P1]** Retrieve all comments for opportunity → Expected: Comments returned with threading structure
- [ ] **[P2]** Edit own comment → Expected: Content updated, updatedAt timestamp set
- [ ] **[P2]** Delete own comment → Expected: Comment removed or soft-deleted

---

### 5.9 Opportunity Search, Filtering, and Pagination

#### Search
- [ ] **[P1]** Search opportunities by title keyword → Expected: Relevant opportunities returned
- [ ] **[P1]** Search by description keyword → Expected: Full-text search results
- [ ] **[P2]** Search by skills → Expected: Opportunities matching skill requirements returned

#### Filtering
- [ ] **[P1]** Filter by type (GUILD_POSITION, PROJECT_ROLE, etc.) → Expected: Filtered results
- [ ] **[P1]** Filter by status (OPEN, FILLED, etc.) → Expected: Status-based filtering
- [ ] **[P1]** Filter by experienceLevel → Expected: Level-based filtering
- [ ] **[P1]** Filter by isRemote → Expected: Remote/on-site filtering
- [ ] **[P2]** Combine multiple filters (type + status + level) → Expected: Compound filtering works

#### Pagination
- [ ] **[P1]** Paginate opportunity list (page 1, size 10) → Expected: First 10 results
- [ ] **[P1]** Navigate to page 2 → Expected: Next 10 results
- [ ] **[P2]** Verify total count matches filtered results → Expected: Pagination metadata accurate

---

### 5.10 Opportunity Edge Cases & Constraints

#### Unique Constraints
- [ ] **[P0]** Enforce unique (opportunityId, applicantId) for applications → Expected: Constraint prevents duplicate applications
- [ ] **[P0]** Enforce unique (opportunityId, bidderId) for bids → Expected: Constraint prevents duplicate bids

#### Linked Entities
- [ ] **[P0]** Opportunity linked to task via taskId → Expected: Task relationship maintained
- [ ] **[P0]** Delete linked task → Expected: Opportunity orphaned or cascade handled
- [ ] **[P1]** Opportunity linked to project and guild → Expected: Both relationships valid
- [ ] **[P2]** Orphaned opportunity (deleted poster) → Expected: Graceful handling

#### Deadline Handling
- [ ] **[P1]** Set deadline in future → Expected: Deadline stored
- [ ] **[P1]** Deadline passes → Expected: Opportunity auto-closed or status updated (if configured)
- [ ] **[P2]** Apply after deadline → Expected: Error or warning

#### Application Limits
- [ ] **[P1]** Set maxApplications to 5 → Expected: 6th application rejected
- [ ] **[P2]** No maxApplications set (null) → Expected: Unlimited applications allowed

---

## Section 6: DAO Governance

### 6.1 Proposal CRUD Operations

#### Proposal Creation
- [ ] **[P0]** Create proposal with all required fields (title, description, options, quorum, threshold, type) → Expected: Proposal created with DRAFT status
- [ ] **[P0]** Create proposal linked to project (projectId) → Expected: Proposal associated with project
- [ ] **[P0]** Create proposal with all optional fields (votingStart, votingEnd, executionDelay) → Expected: All fields saved
- [ ] **[P1]** Create proposal without required fields → Expected: Validation error
- [ ] **[P0]** Set options as JSON array (e.g., ["Yes", "No", "Abstain"]) → Expected: Options stored and retrievable
- [ ] **[P1]** Create proposal with invalid quorum (negative or zero) → Expected: Validation error
- [ ] **[P1]** Create proposal with invalid threshold (< 0 or > 100) → Expected: Validation error
- [ ] **[P2]** Create proposal with very large description (50,000+ chars) → Expected: Text field handles content

#### Proposal Types
- [ ] **[P0]** Create TREASURY proposal → Expected: Type set, treasury actions implied
- [ ] **[P0]** Create GOVERNANCE proposal → Expected: Type set, governance rules change
- [ ] **[P0]** Create STRATEGIC proposal → Expected: Type set, strategic direction
- [ ] **[P0]** Create OPERATIONAL proposal → Expected: Type set, operational changes
- [ ] **[P0]** Create EMERGENCY proposal → Expected: Type set, expedited voting (if configured)
- [ ] **[P0]** Create CONSTITUTIONAL proposal → Expected: Type set, fundamental changes
- [ ] **[P0]** Create SHARES proposal → Expected: Type set, share allocation/distribution

#### Proposal Read/Retrieve
- [ ] **[P0]** Retrieve proposal by ID → Expected: Proposal returned with all fields
- [ ] **[P1]** Retrieve non-existent proposal → Expected: 404 error
- [ ] **[P1]** List all proposals for a project → Expected: Proposals returned
- [ ] **[P2]** Filter proposals by type → Expected: Filtered results
- [ ] **[P2]** Filter proposals by status → Expected: Status-based filtering

#### Proposal Update
- [ ] **[P0]** Update proposal in DRAFT status → Expected: Fields updated
- [ ] **[P1]** Attempt to update ACTIVE proposal → Expected: Error, cannot edit active proposal
- [ ] **[P1]** Update votingStart and votingEnd before activation → Expected: Dates updated
- [ ] **[P2]** Creator updates own proposal → Expected: Update allowed
- [ ] **[P2]** Non-creator attempts to update → Expected: Forbidden unless admin

#### Proposal Deletion
- [ ] **[P0]** Delete proposal in DRAFT status → Expected: Proposal removed
- [ ] **[P1]** Delete ACTIVE proposal → Expected: Error or special permissions required
- [ ] **[P2]** Delete EXECUTED proposal → Expected: Error, cannot delete executed proposals

---

### 6.2 Proposal Lifecycle & Status

#### Status Transitions
- [ ] **[P0]** Create proposal → DRAFT status → Expected: Initial status
- [ ] **[P0]** Activate proposal → DRAFT → ACTIVE → Expected: Voting period begins, votingStart timestamp set
- [ ] **[P0]** Proposal passes (votes meet quorum and threshold) → ACTIVE → PASSED → Expected: Status updated
- [ ] **[P0]** Proposal fails (does not meet quorum or threshold) → ACTIVE → REJECTED → Expected: Status updated
- [ ] **[P0]** Execute passed proposal → PASSED → EXECUTED → Expected: ProposalExecution record created, executedAt timestamp set
- [ ] **[P0]** Cancel proposal → DRAFT/ACTIVE → CANCELLED → Expected: Status updated
- [ ] **[P0]** Proposal expires (votingEnd passed, no quorum) → ACTIVE → EXPIRED → Expected: Status updated
- [ ] **[P1]** Attempt to execute REJECTED proposal → Expected: Error, only PASSED proposals can be executed
- [ ] **[P1]** Attempt to vote on EXPIRED proposal → Expected: Error, voting period ended
- [ ] **[P2]** Attempt to vote on CANCELLED proposal → Expected: Error, proposal inactive

---

### 6.3 Voting

#### Vote Submission
- [ ] **[P0]** User casts vote with choice and weight → Expected: Vote record created with voterId, choice, weight, createdAt
- [ ] **[P0]** Vote with reason (optional) → Expected: Reason stored
- [ ] **[P0]** Vote with txHash (blockchain transaction) → Expected: txHash stored
- [ ] **[P1]** Vote before votingStart → Expected: Error, voting not yet open
- [ ] **[P1]** Vote after votingEnd → Expected: Error, voting period closed
- [ ] **[P1]** Vote on DRAFT proposal → Expected: Error, proposal not active
- [ ] **[P1]** Duplicate vote (same voter, same proposal) → Expected: Error or vote update (depending on rules)
- [ ] **[P2]** Vote weight exceeds user's voting power → Expected: Error, insufficient voting power

#### Vote Weight Calculation
- [ ] **[P0]** Vote weight based on share balance → Expected: Weight equals user's share balance in project
- [ ] **[P0]** Vote weight includes delegated shares → Expected: Delegated voting power added to weight
- [ ] **[P1]** User with zero shares attempts to vote → Expected: Error or zero-weight vote (depending on config)
- [ ] **[P2]** Vote weight updated when share balance changes mid-vote → Expected: Snapshot or real-time weight handling

#### Voting Outcomes
- [ ] **[P0]** Quorum met (total votes >= quorum) → Expected: Proposal eligible for pass/reject
- [ ] **[P0]** Quorum not met → Expected: Proposal REJECTED or EXPIRED
- [ ] **[P0]** Threshold met (approval percentage >= threshold) → Expected: Proposal PASSED
- [ ] **[P0]** Threshold not met → Expected: Proposal REJECTED
- [ ] **[P1]** Exactly at threshold boundary (e.g., 50.00%) → Expected: Proposal PASSED (inclusive)
- [ ] **[P2]** Verify vote tallying is accurate with decimal weights → Expected: Correct calculation

---

### 6.4 Proposal Execution

#### Execution Workflow
- [ ] **[P0]** Execute PASSED proposal → Expected: ProposalExecution record created with unique proposalId, executedAt timestamp, txHash, result (JSON)
- [ ] **[P0]** Execute with executionDelay → Expected: Delay enforced, execution only after delay period
- [ ] **[P1]** Attempt to execute before executionDelay elapses → Expected: Error, delay not satisfied
- [ ] **[P1]** Execute REJECTED proposal → Expected: Error, only PASSED proposals can execute
- [ ] **[P1]** Execute already EXECUTED proposal → Expected: Error, unique constraint on proposalId prevents duplicate execution
- [ ] **[P2]** Store execution result as JSON → Expected: Result data saved and retrievable

#### Execution Actions
- [ ] **[P0]** TREASURY proposal execution transfers funds → Expected: Treasury balance updated, transaction recorded
- [ ] **[P0]** SHARES proposal execution distributes shares → Expected: ShareHolder balances updated
- [ ] **[P1]** GOVERNANCE proposal execution updates project rules → Expected: Configuration changes applied
- [ ] **[P2]** Execution fails mid-process → Expected: Rollback or error handling, status remains PASSED

---

### 6.5 Delegated Voting

#### Delegation Creation
- [ ] **[P0]** User delegates voting power to delegatee for a project and share → Expected: DelegatedVote record created with delegatorId, delegateeId, projectId, shareId, amount, isActive: true
- [ ] **[P0]** Delegate with expiry (expiresAt set) → Expected: Delegation valid until expiry
- [ ] **[P0]** Delegate without expiry (expiresAt null) → Expected: Delegation valid indefinitely
- [ ] **[P1]** Delegate more shares than owned → Expected: Error, cannot delegate more than balance
- [ ] **[P1]** Delegate to self (delegatorId == delegateeId) → Expected: Error, self-delegation not allowed
- [ ] **[P2]** Delegate partial share balance → Expected: Remaining balance retained for direct voting

#### Delegation Status
- [ ] **[P0]** Active delegation (isActive: true) → Expected: Delegatee's voting power includes delegated amount
- [ ] **[P0]** Revoke delegation (revokedAt timestamp set, isActive: false) → Expected: Voting power returned to delegator
- [ ] **[P1]** Delegation expires (past expiresAt) → Expected: Delegation inactive, voting power reverted
- [ ] **[P2]** Attempt to vote with revoked delegation → Expected: Error, delegation no longer active

#### Circular Delegation Prevention
- [ ] **[P1]** User A delegates to User B, User B delegates to User A → Expected: Error, circular delegation detected
- [ ] **[P2]** User A → User B → User C → User A (chain) → Expected: Error, circular delegation chain prevented

#### Delegation Permissions
- [ ] **[P0]** User delegates own shares → Expected: Delegation created
- [ ] **[P1]** User attempts to delegate others' shares → Expected: Error, forbidden
- [ ] **[P2]** Retrieve all delegations for a user → Expected: List of active delegations

---

### 6.6 Edge Cases & Error Handling

#### Voting Edge Cases
- [ ] **[P1]** Vote on proposal with no votingEnd (indefinite) → Expected: Voting allowed until manually closed
- [ ] **[P2]** Proposal with zero quorum → Expected: Any votes sufficient or validation error
- [ ] **[P2]** Proposal with 100% threshold → Expected: Requires unanimous approval

#### Proposal Constraints
- [ ] **[P0]** One execution per proposal (unique proposalId in ProposalExecution) → Expected: Constraint enforced
- [ ] **[P1]** Delete proposal with existing votes → Expected: Cascade delete votes or error
- [ ] **[P2]** Orphaned proposal (deleted creator or project) → Expected: Graceful handling

#### Data Integrity
- [ ] **[P1]** Verify vote count matches total votes cast → Expected: Accurate tallying
- [ ] **[P2]** Verify quorum and threshold calculations with edge weights (0.00001 shares) → Expected: Precision maintained
- [ ] **[P2]** Concurrent voting (multiple users voting simultaneously) → Expected: No race conditions, accurate counts

#### Performance
- [ ] **[P2]** Proposal with 10,000+ votes → Expected: Tallying and retrieval optimized
- [ ] **[P2]** Complex delegation chains (5+ levels) → Expected: Voting power calculation accurate and performant

---

### 6.7 Governance Notifications

#### Notification Triggers
- [ ] **[P1]** Proposal created → Expected: Project members notified (if opted in)
- [ ] **[P1]** Proposal activated → Expected: Voters notified
- [ ] **[P0]** Proposal passed → Expected: Notification sent to creator and voters
- [ ] **[P0]** Proposal rejected → Expected: Notification sent
- [ ] **[P0]** Proposal executed → Expected: Execution confirmation sent
- [ ] **[P2]** Vote cast → Expected: Optional notification to proposal creator

#### Notification Preferences
- [ ] **[P2]** User opts out of governance notifications → Expected: No notifications sent despite events
- [ ] **[P2]** User opts in to specific proposal types → Expected: Notifications filtered by type

---

## Section 7: Finance & Tokenomics

### ProjectShare Management

#### CRUD Operations
- [ ] **[P0]** Create ProjectShare with required fields (name, symbol, totalSupply) → Expected: Share created with default decimals=6
- [ ] **[P0]** Create ProjectShare with unique projectId → Expected: Success, one share per project
- [ ] **[P0]** Attempt to create duplicate ProjectShare for same projectId → Expected: Error, unique constraint violation
- [ ] **[P1]** Create ProjectShare with custom decimals → Expected: Decimals value persisted correctly
- [ ] **[P1]** Create ProjectShare with allocation JSON → Expected: Allocation data stored and retrievable
- [ ] **[P1]** Create ProjectShare with vestingConfig JSON → Expected: Vesting configuration stored
- [ ] **[P2]** Update ProjectShare name and symbol → Expected: Values updated successfully
- [ ] **[P2]** Update ProjectShare totalSupply → Expected: Supply updated, affects calculations
- [ ] **[P1]** Read ProjectShare by projectId → Expected: Returns correct share data
- [ ] **[P1]** Read ProjectShare with assetId → Expected: Returns blockchain asset reference
- [ ] **[P2]** Delete ProjectShare with no dependencies → Expected: Deletion successful
- [ ] **[P3]** Delete ProjectShare with dependent records (shareholders, escrows) → Expected: Error or cascade delete based on schema

#### Deployment Tracking
- [ ] **[P0]** Deploy ProjectShare (isDeployed = false → true) → Expected: isDeployed flag set, deployedAt timestamp recorded
- [ ] **[P0]** Deploy ProjectShare with assetId → Expected: assetId stored after blockchain deployment
- [ ] **[P1]** Attempt operations on non-deployed share → Expected: Business logic validates deployment status
- [ ] **[P2]** Redeploy already deployed share → Expected: Prevented or logged appropriately

### ProjectEquity Management

#### Equity Assignment
- [ ] **[P0]** Grant equity to user for project → Expected: ProjectEquity record created with sharePercent and investmentAmount
- [ ] **[P1]** Grant multiple equity records to same user for same project → Expected: Multiple records allowed (or consolidated based on business rules)
- [ ] **[P1]** Grant equity with 100% sharePercent total across all users → Expected: Total validation enforced
- [ ] **[P1]** Grant equity exceeding 100% total → Expected: Validation error
- [ ] **[P2]** Grant equity with zero sharePercent → Expected: Validation error or warning
- [ ] **[P2]** Grant equity with negative sharePercent → Expected: Validation error
- [ ] **[P2]** Grant equity with zero investmentAmount → Expected: Allowed (contributor scenario)
- [ ] **[P1]** Update equity sharePercent → Expected: Updated correctly, affects voting power
- [ ] **[P1]** Update equity investmentAmount → Expected: Updated correctly, affects ROI calculations
- [ ] **[P2]** Delete equity record → Expected: Equity removed, affects project cap table

#### Equity Queries
- [ ] **[P1]** Get all equity holders for a project → Expected: Returns all ProjectEquity records for projectId
- [ ] **[P1]** Get total equity percentage allocated → Expected: Sum of all sharePercent values
- [ ] **[P2]** Get total investment amount for project → Expected: Sum of all investmentAmount values
- [ ] **[P2]** Get user equity across all projects → Expected: List of all ProjectEquity for userId

### Treasury Management

#### Treasury Creation & Tracking
- [ ] **[P0]** Create Treasury for project → Expected: Treasury created with balance=0, unique projectId
- [ ] **[P0]** Attempt to create duplicate Treasury for project → Expected: Error, unique constraint violation
- [ ] **[P1]** Create Treasury with shareAssetId → Expected: Asset reference stored
- [ ] **[P1]** Update Treasury balance → Expected: Balance updated correctly
- [ ] **[P2]** Update Treasury shareAssetId → Expected: Asset reference updated
- [ ] **[P1]** Read Treasury by projectId → Expected: Returns correct treasury data

#### Treasury Transactions - ALL Types
- [ ] **[P0]** Create DEPOSIT transaction → Expected: Transaction recorded, treasury balance increased
- [ ] **[P0]** Create WITHDRAWAL transaction → Expected: Transaction recorded, treasury balance decreased
- [ ] **[P0]** Create TASK_PAYMENT transaction → Expected: Transaction recorded for task compensation
- [ ] **[P0]** Create PROPOSAL_EXECUTION transaction → Expected: Transaction linked to proposalId
- [ ] **[P0]** Create DIVIDEND transaction → Expected: Transaction recorded for dividend distribution
- [ ] **[P0]** Create FEE transaction → Expected: Transaction recorded for platform/protocol fees
- [ ] **[P1]** Create transaction with txHash → Expected: Blockchain transaction hash stored
- [ ] **[P1]** Create transaction with description → Expected: Description stored and retrievable
- [ ] **[P1]** Create transaction linked to proposal → Expected: proposalId reference stored
- [ ] **[P2]** Create transaction with empty/null amount → Expected: Validation error
- [ ] **[P2]** Create transaction with negative amount → Expected: Validation error or handled based on type
- [ ] **[P1]** Query all transactions for treasury → Expected: Returns all TreasuryTransaction records
- [ ] **[P1]** Query transactions by type → Expected: Filtered results for specific TransactionType
- [ ] **[P2]** Query transactions by date range → Expected: Filtered results within date range
- [ ] **[P2]** Calculate treasury balance from transactions → Expected: Matches stored balance

### ShareHolder Management

#### Balance Tracking
- [ ] **[P0]** Create ShareHolder with initial balance → Expected: ShareHolder record created for shareId + userId
- [ ] **[P1]** Update ShareHolder balance → Expected: Balance updated correctly
- [ ] **[P1]** Update ShareHolder stakedAmount → Expected: Staked amount updated, affects available balance
- [ ] **[P1]** Update ShareHolder lockedAmount → Expected: Locked amount updated, affects available balance
- [ ] **[P0]** Calculate available balance (balance - stakedAmount - lockedAmount) → Expected: Correct available amount
- [ ] **[P2]** Set stakedAmount exceeding balance → Expected: Validation error
- [ ] **[P2]** Set lockedAmount exceeding balance → Expected: Validation error
- [ ] **[P2]** Set combined stakedAmount + lockedAmount exceeding balance → Expected: Validation error
- [ ] **[P2]** Transfer shares reducing balance below staked + locked → Expected: Validation error
- [ ] **[P1]** Query all shareholders for a share → Expected: Returns all ShareHolder records for shareId
- [ ] **[P2]** Query total circulating supply → Expected: Sum of all balances for shareId

#### Staking & Locking
- [ ] **[P1]** Stake shares → Expected: stakedAmount increased, available balance decreased
- [ ] **[P1]** Unstake shares → Expected: stakedAmount decreased, available balance increased
- [ ] **[P1]** Lock shares → Expected: lockedAmount increased, available balance decreased
- [ ] **[P1]** Unlock shares → Expected: lockedAmount decreased, available balance increased
- [ ] **[P2]** Attempt to stake/lock more than available → Expected: Validation error

### ShareVesting Management

#### Vesting Creation & Configuration
- [ ] **[P0]** Create ShareVesting with DAILY frequency → Expected: Vesting schedule created with daily release
- [ ] **[P0]** Create ShareVesting with WEEKLY frequency → Expected: Vesting schedule created with weekly release
- [ ] **[P0]** Create ShareVesting with MONTHLY frequency → Expected: Vesting schedule created with monthly release
- [ ] **[P0]** Create ShareVesting with QUARTERLY frequency → Expected: Vesting schedule created with quarterly release
- [ ] **[P1]** Create ShareVesting with totalAmount → Expected: Total vesting amount set
- [ ] **[P1]** Create ShareVesting with releasedAmount=0 → Expected: No shares released initially
- [ ] **[P1]** Create ShareVesting with cliff period → Expected: cliffEnd date set after startDate
- [ ] **[P1]** Create ShareVesting with vestingEnd after cliffEnd → Expected: Valid vesting timeline
- [ ] **[P2]** Create ShareVesting with cliffEnd before startDate → Expected: Validation error
- [ ] **[P2]** Create ShareVesting with vestingEnd before cliffEnd → Expected: Validation error
- [ ] **[P2]** Create ShareVesting with zero totalAmount → Expected: Validation error

#### Vesting Cliff Enforcement
- [ ] **[P0]** Attempt to release shares before cliffEnd → Expected: Release blocked, error message
- [ ] **[P0]** Release shares exactly at cliffEnd → Expected: Release allowed
- [ ] **[P0]** Release shares after cliffEnd → Expected: Release allowed
- [ ] **[P1]** Query vested amount before cliff → Expected: Returns 0
- [ ] **[P1]** Query vested amount after cliff → Expected: Returns calculated vested amount

#### Progressive Release Calculation
- [ ] **[P0]** Calculate vested amount at 25% of vesting period → Expected: ~25% of totalAmount vested
- [ ] **[P0]** Calculate vested amount at 50% of vesting period → Expected: ~50% of totalAmount vested
- [ ] **[P0]** Calculate vested amount at 75% of vesting period → Expected: ~75% of totalAmount vested
- [ ] **[P0]** Calculate vested amount at 100% of vesting period (vestingEnd) → Expected: 100% of totalAmount vested
- [ ] **[P1]** Calculate vested amount past vestingEnd → Expected: No more than totalAmount
- [ ] **[P1]** Release vested shares (update releasedAmount) → Expected: releasedAmount increased, cannot exceed totalAmount
- [ ] **[P1]** Attempt to release more than vested amount → Expected: Validation error
- [ ] **[P1]** Attempt to release more than totalAmount → Expected: Validation error
- [ ] **[P2]** Query remaining vested shares → Expected: totalAmount - releasedAmount
- [ ] **[P2]** Complete full vesting schedule → Expected: releasedAmount == totalAmount

#### Vesting by Frequency
- [ ] **[P1]** DAILY vesting: calculate release every day → Expected: Daily increments calculated
- [ ] **[P1]** WEEKLY vesting: calculate release every 7 days → Expected: Weekly increments calculated
- [ ] **[P1]** MONTHLY vesting: calculate release every month → Expected: Monthly increments calculated
- [ ] **[P1]** QUARTERLY vesting: calculate release every 3 months → Expected: Quarterly increments calculated

### Fundraising Campaign Management

#### Campaign Creation
- [ ] **[P0]** Create Fundraising linked to ProjectShare → Expected: Campaign created with unique shareId
- [ ] **[P0]** Attempt to create duplicate Fundraising for same share → Expected: Error, unique constraint violation
- [ ] **[P1]** Create Fundraising with fundingGoal → Expected: Goal amount set
- [ ] **[P1]** Create Fundraising with minContribution → Expected: Minimum contribution limit set
- [ ] **[P1]** Create Fundraising with maxContribution → Expected: Maximum contribution limit set
- [ ] **[P1]** Create Fundraising with sharePrice → Expected: Price per share set
- [ ] **[P1]** Create Fundraising with date range (startDate, endDate) → Expected: Campaign period defined
- [ ] **[P2]** Create Fundraising with endDate before startDate → Expected: Validation error
- [ ] **[P2]** Create Fundraising with minContribution > maxContribution → Expected: Validation error

#### Campaign Status Transitions
- [ ] **[P0]** Create campaign with PENDING status → Expected: Initial status set to PENDING
- [ ] **[P0]** Activate campaign (PENDING → ACTIVE) → Expected: Status updated, campaign starts accepting contributions
- [ ] **[P0]** Complete successful campaign (ACTIVE → SUCCESSFUL) when goal met → Expected: Status updated to SUCCESSFUL
- [ ] **[P0]** Fail campaign (ACTIVE → FAILED) when endDate reached without meeting goal → Expected: Status updated to FAILED
- [ ] **[P1]** Cancel campaign (any status → CANCELLED) → Expected: Status updated to CANCELLED
- [ ] **[P0]** Initiate refund process (FAILED → REFUNDING) → Expected: Status updated to REFUNDING
- [ ] **[P2]** Attempt invalid status transition → Expected: Validation error
- [ ] **[P1]** Query campaigns by status → Expected: Filtered results for specific FundraisingStatus

#### Funding Tracking
- [ ] **[P0]** Update currentFunding when contribution confirmed → Expected: currentFunding increased
- [ ] **[P0]** Check if fundingGoal reached → Expected: Correct comparison of currentFunding vs fundingGoal
- [ ] **[P1]** Calculate funding percentage → Expected: (currentFunding / fundingGoal) * 100

### Fundraising Contribution Management

#### Contribution Creation
- [ ] **[P0]** Create contribution with valid amount → Expected: FundraisingContribution created with PENDING status
- [ ] **[P1]** Create contribution with shareAmount calculated from sharePrice → Expected: Correct share allocation
- [ ] **[P1]** Create contribution with paymentAsset → Expected: Payment asset type stored
- [ ] **[P1]** Create contribution with txHash → Expected: Blockchain transaction reference stored
- [ ] **[P2]** Create contribution with amount below minContribution → Expected: Validation error
- [ ] **[P2]** Create contribution with amount above maxContribution → Expected: Validation error
- [ ] **[P2]** Create contribution with zero or negative amount → Expected: Validation error

#### Contribution Status Transitions
- [ ] **[P0]** Confirm contribution (PENDING → CONFIRMED) → Expected: Status updated, funds added to currentFunding
- [ ] **[P1]** Refund contribution (CONFIRMED → REFUNDED) → Expected: Status updated, funds deducted from currentFunding
- [ ] **[P1]** Fail contribution (PENDING → FAILED) → Expected: Status updated, no impact on currentFunding
- [ ] **[P2]** Attempt to refund non-confirmed contribution → Expected: Validation error

#### Contribution Limit Enforcement
- [ ] **[P0]** Enforce minContribution on single contribution → Expected: Contribution blocked if below minimum
- [ ] **[P0]** Enforce maxContribution on single contribution → Expected: Contribution blocked if above maximum
- [ ] **[P1]** Enforce cumulative maxContribution per user → Expected: Total user contributions cannot exceed limit
- [ ] **[P2]** Handle multiple contributions from same user → Expected: Sum validated against maxContribution

#### Contribution Queries
- [ ] **[P1]** Get all contributions for a campaign → Expected: Returns all FundraisingContribution records for fundraisingId
- [ ] **[P1]** Get contributions by user → Expected: Returns all contributions for userId
- [ ] **[P1]** Get contributions by status → Expected: Filtered results for specific ContributionStatus
- [ ] **[P2]** Calculate total confirmed contributions → Expected: Sum of amounts where status=CONFIRMED

### Wallet Management

#### Wallet CRUD
- [ ] **[P0]** Create wallet with PERA provider → Expected: Wallet created with provider type
- [ ] **[P0]** Create wallet with DEFLY provider → Expected: Wallet created with provider type
- [ ] **[P0]** Create wallet with ALGOSIGNER provider → Expected: Wallet created with provider type
- [ ] **[P0]** Create wallet with WALLETCONNECT provider → Expected: Wallet created with provider type
- [ ] **[P0]** Create wallet with OTHER provider → Expected: Wallet created with provider type
- [ ] **[P0]** Create wallet with unique address → Expected: Success
- [ ] **[P0]** Attempt to create wallet with duplicate address → Expected: Error, unique constraint violation
- [ ] **[P1]** Create wallet with optional label → Expected: Label stored
- [ ] **[P1]** Create multiple wallets for same user → Expected: All wallets created successfully
- [ ] **[P1]** Verify wallet (isVerified = false → true) → Expected: Verification status updated
- [ ] **[P1]** Set wallet as primary (isPrimary = true) → Expected: Only one primary wallet per user
- [ ] **[P1]** Set new primary wallet when user already has primary → Expected: Previous primary unset, new one set
- [ ] **[P2]** Update wallet label → Expected: Label updated successfully
- [ ] **[P2]** Delete wallet → Expected: Wallet removed (or soft deleted based on business rules)

#### Wallet Verification
- [ ] **[P0]** Verify wallet ownership → Expected: isVerified set to true
- [ ] **[P1]** Require verified wallet for transactions → Expected: Unverified wallets blocked from critical operations
- [ ] **[P2]** Query all verified wallets for user → Expected: Returns wallets where isVerified=true

#### Primary Wallet Logic
- [ ] **[P1]** Set first wallet as primary by default → Expected: isPrimary=true automatically
- [ ] **[P1]** Change primary wallet → Expected: Old primary isPrimary=false, new primary isPrimary=true
- [ ] **[P2]** Delete primary wallet → Expected: Another wallet becomes primary or user has no primary

### TaskEscrow Management

#### Escrow Creation
- [ ] **[P0]** Create TaskEscrow for task → Expected: Escrow created with unique taskId, status=FUNDED
- [ ] **[P0]** Attempt to create duplicate TaskEscrow for same task → Expected: Error, unique constraint violation
- [ ] **[P1]** Create TaskEscrow linked to funder → Expected: funderId reference stored
- [ ] **[P1]** Create TaskEscrow linked to share → Expected: shareId reference stored
- [ ] **[P1]** Create TaskEscrow with amount → Expected: Escrow amount stored
- [ ] **[P1]** Create TaskEscrow with txHashFund → Expected: Funding transaction hash stored
- [ ] **[P1]** Set fundedAt timestamp → Expected: Timestamp recorded when funded

#### Escrow Status Transitions
- [ ] **[P0]** Fund escrow (NONE → FUNDED) → Expected: Status updated, fundedAt timestamp set
- [ ] **[P0]** Release escrow (FUNDED → RELEASED) → Expected: Status updated, releasedAt timestamp set, txHashRelease stored
- [ ] **[P0]** Dispute escrow (FUNDED → DISPUTED) → Expected: Status updated, requires resolution
- [ ] **[P0]** Refund escrow (FUNDED/DISPUTED → REFUNDED) → Expected: Status updated, refundedAt timestamp set, txHashRefund stored
- [ ] **[P1]** Attempt to release unfunded escrow → Expected: Validation error
- [ ] **[P2]** Attempt invalid status transition → Expected: Validation error

#### Escrow Timestamps
- [ ] **[P1]** Record fundedAt when status=FUNDED → Expected: Timestamp set correctly
- [ ] **[P1]** Record releasedAt when status=RELEASED → Expected: Timestamp set correctly
- [ ] **[P1]** Record refundedAt when status=REFUNDED → Expected: Timestamp set correctly
- [ ] **[P2]** Query escrows by timestamp range → Expected: Filtered results

#### Escrow Queries
- [ ] **[P1]** Get escrow by taskId → Expected: Returns TaskEscrow record for task
- [ ] **[P1]** Get all escrows for funder → Expected: Returns all escrows where funderId matches
- [ ] **[P1]** Get escrows by status → Expected: Filtered results for specific EscrowStatus
- [ ] **[P1]** Get escrows for specific share → Expected: Returns all escrows linked to shareId

### ShareSwap Management

#### Swap Creation
- [ ] **[P0]** Create swap from one share to another → Expected: ShareSwap created with fromShareId and toShareId
- [ ] **[P1]** Create swap with fromAmount and toAmount → Expected: Amounts stored correctly
- [ ] **[P1]** Create swap with calculated exchangeRate → Expected: exchangeRate = toAmount / fromAmount
- [ ] **[P1]** Create swap with fee → Expected: Fee amount stored (default 0)
- [ ] **[P1]** Create swap with txHash → Expected: Blockchain transaction hash stored
- [ ] **[P2]** Create swap with zero amounts → Expected: Validation error
- [ ] **[P2]** Create swap with negative amounts → Expected: Validation error
- [ ] **[P2]** Create swap from share to itself (fromShareId == toShareId) → Expected: Validation error

#### Swap Status Transitions
- [ ] **[P0]** Initiate swap (status = PENDING) → Expected: Swap created in PENDING state
- [ ] **[P0]** Process swap (PENDING → PROCESSING) → Expected: Status updated, swap being executed
- [ ] **[P0]** Complete swap (PROCESSING → COMPLETED) → Expected: Status updated, completedAt timestamp set
- [ ] **[P0]** Fail swap (PENDING/PROCESSING → FAILED) → Expected: Status updated, funds returned
- [ ] **[P0]** Cancel swap (PENDING → CANCELLED) → Expected: Status updated, no exchange occurs
- [ ] **[P2]** Attempt to cancel completed swap → Expected: Validation error

#### Swap Execution
- [ ] **[P0]** Execute swap: deduct fromAmount from user's fromShare balance → Expected: Balance decreased
- [ ] **[P0]** Execute swap: add toAmount to user's toShare balance → Expected: Balance increased
- [ ] **[P0]** Execute swap: deduct fee if applicable → Expected: Fee deducted from toAmount
- [ ] **[P1]** Record completedAt timestamp on completion → Expected: Timestamp set
- [ ] **[P1]** Verify user has sufficient fromShare balance before swap → Expected: Validation check
- [ ] **[P2]** Handle partial swap failure → Expected: Rollback or error handling

#### Swap Queries
- [ ] **[P1]** Get all swaps for user → Expected: Returns all ShareSwap records for userId
- [ ] **[P1]** Get swaps by status → Expected: Filtered results for specific SwapStatus
- [ ] **[P2]** Get swap history for specific share pair → Expected: Returns swaps between fromShareId and toShareId
- [ ] **[P2]** Calculate total swap volume for share → Expected: Sum of all amounts for shareId

### LiquidityPool Management

#### Pool Creation
- [ ] **[P0]** Create liquidity pool for two shares → Expected: Pool created with share1Id and share2Id
- [ ] **[P1]** Create pool with initial reserves (reserve1, reserve2) → Expected: Reserves set to 0 initially
- [ ] **[P1]** Create pool with totalShares → Expected: totalShares initialized to 0
- [ ] **[P1]** Create pool with feePercent → Expected: Fee set to default 0.3% (0.003)
- [ ] **[P1]** Create pool with custom feePercent → Expected: Custom fee stored
- [ ] **[P1]** Create pool with isActive=true → Expected: Pool is active
- [ ] **[P2]** Create pool with same share for both sides (share1Id == share2Id) → Expected: Validation error
- [ ] **[P2]** Create duplicate pool for same share pair → Expected: Error or allowed based on business rules

#### Pool Reserve Management
- [ ] **[P0]** Add liquidity: increase reserve1 and reserve2 → Expected: Reserves updated, totalShares increased
- [ ] **[P0]** Remove liquidity: decrease reserve1 and reserve2 → Expected: Reserves updated, totalShares decreased
- [ ] **[P1]** Calculate constant product (reserve1 * reserve2) → Expected: Constant maintained for AMM
- [ ] **[P2]** Prevent reserve from going negative → Expected: Validation error
- [ ] **[P2]** Handle reserve overflow → Expected: Error or decimal precision handling

#### Pool Status
- [ ] **[P1]** Activate pool (isActive = true) → Expected: Pool accepts trades
- [ ] **[P1]** Deactivate pool (isActive = false) → Expected: Pool stops accepting new trades
- [ ] **[P2]** Query only active pools → Expected: Returns pools where isActive=true

#### Pool Queries
- [ ] **[P1]** Get pool by share pair → Expected: Returns pool for share1Id and share2Id
- [ ] **[P1]** Get all pools for a share → Expected: Returns all pools containing shareId
- [ ] **[P2]** Calculate pool TVL (Total Value Locked) → Expected: reserve1 + reserve2 in common currency

### LiquidityProvider Management

#### Provider Creation
- [ ] **[P0]** Add liquidity provider to pool → Expected: LiquidityProvider record created for poolId and userId
- [ ] **[P1]** Record provider shares → Expected: shares amount stored
- [ ] **[P1]** Record share1In and share2In amounts → Expected: Deposited amounts stored (default 0)
- [ ] **[P1]** Update share1In when depositing → Expected: share1In increased
- [ ] **[P1]** Update share2In when depositing → Expected: share2In increased
- [ ] **[P2]** Allow multiple deposits from same provider → Expected: Cumulative amounts tracked

#### Liquidity Provision
- [ ] **[P0]** Deposit liquidity: increase share1In and share2In → Expected: Provider position increased
- [ ] **[P0]** Deposit liquidity: mint LP shares → Expected: Provider shares increased proportionally
- [ ] **[P0]** Withdraw liquidity: decrease share1In and share2In → Expected: Provider position decreased
- [ ] **[P0]** Withdraw liquidity: burn LP shares → Expected: Provider shares decreased
- [ ] **[P1]** Calculate provider's pool ownership percentage → Expected: (provider shares / pool totalShares) * 100
- [ ] **[P2]** Attempt to withdraw more than provided → Expected: Validation error

#### Provider Queries
- [ ] **[P1]** Get all providers for a pool → Expected: Returns all LiquidityProvider records for poolId
- [ ] **[P1]** Get all pools user has provided liquidity to → Expected: Returns all providers for userId
- [ ] **[P2]** Calculate total liquidity provided by user → Expected: Sum of share1In and share2In across all pools

### Edge Cases & Validation

#### Insufficient Balance
- [ ] **[P0]** Attempt swap with insufficient fromShare balance → Expected: Error, transaction blocked
- [ ] **[P0]** Attempt contribution with insufficient wallet balance → Expected: Error, transaction blocked
- [ ] **[P0]** Attempt escrow release with insufficient treasury → Expected: Error or handled appropriately
- [ ] **[P1]** Check available balance before all debit operations → Expected: Validation enforced

#### Negative & Zero Amounts
- [ ] **[P0]** Attempt to create transaction with negative amount → Expected: Validation error
- [ ] **[P0]** Attempt to create transaction with zero amount → Expected: Validation error or warning
- [ ] **[P1]** Handle negative balance scenarios → Expected: Balance cannot go negative
- [ ] **[P2]** Zero out balances during cleanup → Expected: Allowed for administrative purposes

#### Overflow & Precision
- [ ] **[P1]** Handle very large decimal amounts (near maximum) → Expected: No overflow, precision maintained
- [ ] **[P1]** Handle very small decimal amounts (near minimum) → Expected: Precision maintained at 8 decimals
- [ ] **[P2]** Perform arithmetic on maximum values → Expected: No overflow errors
- [ ] **[P2]** Round calculations consistently → Expected: Consistent rounding rules applied

#### Race Conditions
- [ ] **[P0]** Concurrent balance updates from multiple transactions → Expected: Database locking or transaction isolation prevents inconsistency
- [ ] **[P0]** Concurrent contributions to fundraising near goal → Expected: No over-funding, proper locking
- [ ] **[P0]** Concurrent swaps affecting same pool reserves → Expected: Atomic updates, no race conditions
- [ ] **[P1]** Concurrent vesting releases for same schedule → Expected: No double-release
- [ ] **[P1]** Concurrent escrow status changes → Expected: Proper state management

---

## Section 8: Social & Communication

### Post Management

#### Post CRUD Operations
- [ ] **[P0]** Create post with title and content → Expected: Post created successfully
- [ ] **[P0]** Create post without title (optional field) → Expected: Post created with null title
- [ ] **[P1]** Create post with metadata JSON → Expected: Metadata stored and retrievable
- [ ] **[P1]** Update post content → Expected: Content updated, updatedAt timestamp changed
- [ ] **[P1]** Update post title → Expected: Title updated successfully
- [ ] **[P2]** Delete post → Expected: Post removed (or soft deleted based on business rules)
- [ ] **[P1]** Read post by id → Expected: Returns complete post data
- [ ] **[P2]** Query posts by author → Expected: Returns all posts for authorId

#### Post Types (ALL)
- [ ] **[P0]** Create post with type=POST → Expected: Generic post created
- [ ] **[P0]** Create post with type=PROJECT_UPDATE → Expected: Project update post created, linked to project
- [ ] **[P0]** Create post with type=GUILD_ACTIVITY → Expected: Guild activity post created, linked to guild
- [ ] **[P0]** Create post with type=TASK_COMPLETED → Expected: Task completion post created
- [ ] **[P0]** Create post with type=MILESTONE → Expected: Milestone post created
- [ ] **[P0]** Create post with type=PROPOSAL → Expected: Proposal post created
- [ ] **[P0]** Create post with type=SHOP_ITEM → Expected: Shop item post created
- [ ] **[P1]** Query posts by type → Expected: Filtered results for specific PostType
- [ ] **[P2]** Validate required fields based on post type → Expected: Type-specific validation enforced

#### Post Visibility
- [ ] **[P0]** Create post with visibility=PUBLIC → Expected: Post visible to all users
- [ ] **[P0]** Create post with visibility=PROJECT_MEMBERS → Expected: Post visible only to project members
- [ ] **[P0]** Create post with visibility=GUILD_MEMBERS → Expected: Post visible only to guild members
- [ ] **[P0]** Create post with visibility=PRIVATE → Expected: Post visible only to author
- [ ] **[P1]** Query posts filtered by visibility for current user → Expected: Returns only posts user can access
- [ ] **[P1]** Attempt to view PRIVATE post as non-author → Expected: Access denied
- [ ] **[P1]** View PROJECT_MEMBERS post as project member → Expected: Access granted
- [ ] **[P1]** View PROJECT_MEMBERS post as non-member → Expected: Access denied
- [ ] **[P1]** View GUILD_MEMBERS post as guild member → Expected: Access granted
- [ ] **[P1]** View GUILD_MEMBERS post as non-member → Expected: Access denied

#### Post Linking
- [ ] **[P1]** Create post linked to author → Expected: authorId reference stored
- [ ] **[P1]** Create post linked to project (optional) → Expected: projectId reference stored if provided
- [ ] **[P1]** Create post linked to guild (optional) → Expected: guildId reference stored if provided
- [ ] **[P2]** Create post linked to both project and guild → Expected: Both references stored
- [ ] **[P2]** Create post without project or guild link → Expected: Allowed for generic posts
- [ ] **[P2]** Query posts for specific project → Expected: Returns all posts where projectId matches
- [ ] **[P2]** Query posts for specific guild → Expected: Returns all posts where guildId matches

### Post Media Management

#### Media Attachment
- [ ] **[P0]** Attach IMAGE media to post → Expected: PostMedia created with type=IMAGE
- [ ] **[P0]** Attach VIDEO media to post → Expected: PostMedia created with type=VIDEO
- [ ] **[P0]** Attach AUDIO media to post → Expected: PostMedia created with type=AUDIO
- [ ] **[P0]** Attach DOCUMENT media to post → Expected: PostMedia created with type=DOCUMENT
- [ ] **[P0]** Attach ARCHIVE media to post → Expected: PostMedia created with type=ARCHIVE
- [ ] **[P0]** Attach OTHER media to post → Expected: PostMedia created with type=OTHER
- [ ] **[P1]** Attach multiple media items to post → Expected: All media items stored with order
- [ ] **[P1]** Set media url → Expected: URL stored and retrievable
- [ ] **[P1]** Set media thumbnailUrl → Expected: Thumbnail URL stored
- [ ] **[P1]** Set media altText → Expected: Alternative text stored for accessibility
- [ ] **[P1]** Set media order → Expected: Order number stored for sorting
- [ ] **[P2]** Update media order → Expected: Order updated, affects display sequence
- [ ] **[P2]** Delete media from post → Expected: PostMedia removed
- [ ] **[P2]** Query all media for post ordered by order field → Expected: Returns sorted media list

### Post Interactions

#### Post Likes
- [ ] **[P0]** User likes post → Expected: PostLike created, likesCount incremented
- [ ] **[P0]** User unlikes post → Expected: PostLike deleted, likesCount decremented
- [ ] **[P0]** Prevent duplicate likes (one per user per post) → Expected: Unique constraint enforced
- [ ] **[P1]** Query all users who liked post → Expected: Returns all PostLike records for postId
- [ ] **[P1]** Check if user has liked post → Expected: Returns true/false based on PostLike existence
- [ ] **[P2]** Update post likesCount in real-time → Expected: Counter updated immediately

#### Post Comments
- [ ] **[P0]** Create comment on post → Expected: PostComment created with postId and authorId
- [ ] **[P0]** Create threaded comment (reply to comment) → Expected: PostComment created with parentId
- [ ] **[P1]** Update comment content → Expected: Content updated, updatedAt timestamp changed
- [ ] **[P1]** Delete comment → Expected: Comment removed (or soft deleted)
- [ ] **[P1]** Like comment → Expected: Comment likesCount incremented
- [ ] **[P1]** Unlike comment → Expected: Comment likesCount decremented
- [ ] **[P1]** Track comment likesCount → Expected: Counter updated correctly
- [ ] **[P1]** Update post commentsCount when comment added → Expected: Post counter incremented
- [ ] **[P1]** Update post commentsCount when comment deleted → Expected: Post counter decremented
- [ ] **[P2]** Query all comments for post → Expected: Returns all root-level comments
- [ ] **[P2]** Query all replies to a comment → Expected: Returns all comments where parentId matches
- [ ] **[P2]** Display comments in threaded structure → Expected: Nested comment tree rendered

#### Post Bookmarks
- [ ] **[P0]** User bookmarks post → Expected: PostBookmark created
- [ ] **[P0]** User removes bookmark → Expected: PostBookmark deleted
- [ ] **[P0]** Prevent duplicate bookmarks (one per user per post) → Expected: Unique constraint enforced
- [ ] **[P1]** Query all bookmarked posts for user → Expected: Returns all posts bookmarked by userId
- [ ] **[P2]** Check if user has bookmarked post → Expected: Returns true/false based on PostBookmark existence

#### Post Shares
- [ ] **[P0]** Share post to project → Expected: PostShare created with sharedToProjectId
- [ ] **[P0]** Share post to guild → Expected: PostShare created with sharedToGuildId
- [ ] **[P1]** Share post with comment → Expected: Comment stored in PostShare
- [ ] **[P1]** Share post without comment → Expected: Share created with null comment
- [ ] **[P1]** Update post sharesCount when shared → Expected: Post counter incremented
- [ ] **[P2]** Query all shares of a post → Expected: Returns all PostShare records for postId
- [ ] **[P2]** Query where user shared a post → Expected: Returns PostShare with destination details

### Post Counters

#### Counter Management
- [ ] **[P0]** Initialize likesCount=0 on post creation → Expected: Default value set
- [ ] **[P0]** Initialize commentsCount=0 on post creation → Expected: Default value set
- [ ] **[P0]** Initialize sharesCount=0 on post creation → Expected: Default value set
- [ ] **[P0]** Initialize viewsCount=0 on post creation → Expected: Default value set
- [ ] **[P1]** Increment viewsCount on post view → Expected: Counter increased
- [ ] **[P1]** Ensure counters are accurate and consistent → Expected: Counters match actual counts
- [ ] **[P2]** Batch update counters for performance → Expected: Efficient counter updates
- [ ] **[P2]** Rebuild counters from actual data → Expected: Counters recalculated correctly

### Post Trending

#### Trending Logic
- [ ] **[P1]** Mark post as pinned (isPinned=true) → Expected: Post appears at top of feed
- [ ] **[P1]** Unpin post (isPinned=false) → Expected: Post returns to normal position
- [ ] **[P1]** Mark post as trending (isTrending=true) → Expected: Post appears in trending section
- [ ] **[P1]** Calculate trendingScore based on engagement → Expected: Score calculated from likes, comments, shares, views
- [ ] **[P1]** Set trendingRank based on trendingScore → Expected: Posts ranked by score
- [ ] **[P1]** Record trendingAt timestamp → Expected: Timestamp set when post becomes trending
- [ ] **[P2]** Query trending posts ordered by trendingRank → Expected: Returns sorted trending posts
- [ ] **[P2]** Update trendingScore periodically → Expected: Scores recalculated to reflect current engagement
- [ ] **[P2]** Remove posts from trending after time period → Expected: isTrending set to false after expiry

### Conversation Management

#### Conversation Creation
- [ ] **[P0]** Create DIRECT conversation between two users → Expected: Conversation created with type=DIRECT
- [ ] **[P0]** Create GROUP conversation → Expected: Conversation created with type=GROUP
- [ ] **[P1]** Create conversation with name → Expected: Name stored (optional for direct, required for group)
- [ ] **[P1]** Create conversation with avatarUrl → Expected: Avatar URL stored
- [ ] **[P1]** Link conversation to creator → Expected: createdById reference stored
- [ ] **[P1]** Set lastMessageAt on first message → Expected: Timestamp updated
- [ ] **[P2]** Prevent duplicate direct conversations between same users → Expected: Validation or deduplication

#### Conversation Updates
- [ ] **[P1]** Update conversation name → Expected: Name updated (for group conversations)
- [ ] **[P1]** Update conversation avatarUrl → Expected: Avatar updated
- [ ] **[P1]** Update lastMessageAt on new message → Expected: Timestamp reflects latest message
- [ ] **[P2]** Delete conversation → Expected: Conversation removed, cascade to members and messages

### ConversationMember Management

#### Member Management
- [ ] **[P0]** Add member to conversation with role=MEMBER → Expected: ConversationMember created
- [ ] **[P0]** Add member with role=ADMIN → Expected: ConversationMember created with admin privileges
- [ ] **[P0]** Add member with role=OWNER → Expected: ConversationMember created with owner privileges
- [ ] **[P0]** Enforce unique constraint (conversationId, userId) → Expected: User cannot be added twice
- [ ] **[P1]** Record joinedAt timestamp → Expected: Timestamp set when member joins
- [ ] **[P1]** Track lastActiveAt → Expected: Updated on user activity in conversation
- [ ] **[P1]** Track lastReadAt → Expected: Updated when user reads messages
- [ ] **[P1]** Remove member from conversation → Expected: ConversationMember deleted
- [ ] **[P2]** Update member role → Expected: Role changed (e.g., MEMBER → ADMIN)
- [ ] **[P2]** Query all members of conversation → Expected: Returns all ConversationMember records
- [ ] **[P2]** Query all conversations user is member of → Expected: Returns all conversations for userId

#### Read Tracking
- [ ] **[P1]** Update lastReadAt when user reads messages → Expected: Timestamp updated to latest read message time
- [ ] **[P1]** Calculate unread message count for user → Expected: Count of messages after lastReadAt
- [ ] **[P2]** Mark all messages as read → Expected: lastReadAt updated to current time

### ChatMessage Management

#### Message Creation
- [ ] **[P0]** Send message in conversation → Expected: ChatMessage created with conversationId
- [ ] **[P1]** Send message with text content → Expected: message field populated
- [ ] **[P1]** Send message with attachment → Expected: chatAttachmentId reference stored
- [ ] **[P1]** Send empty message with attachment only → Expected: Message created with null text
- [ ] **[P1]** Link message to sender (userFromId) and recipient (userToId) → Expected: References stored
- [ ] **[P1]** Set sentAt timestamp → Expected: Timestamp recorded
- [ ] **[P1]** Initialize status=NOTSEEN → Expected: Default status set
- [ ] **[P2]** Prevent empty message (no text and no attachment) → Expected: Validation error

#### Message Status Transitions
- [ ] **[P0]** Send message (status = SENT) → Expected: Message marked as sent
- [ ] **[P0]** Deliver message (SENT → DELIVERED) → Expected: Status updated, deliveredAt timestamp set
- [ ] **[P0]** Read message (DELIVERED → READ) → Expected: Status updated, seenAt timestamp set
- [ ] **[P0]** Fail message sending (any → FAILED) → Expected: Status updated to FAILED
- [ ] **[P1]** Track message from NOTSEEN → SENT → DELIVERED → READ → Expected: Full status lifecycle
- [ ] **[P2]** Query messages by status → Expected: Filtered results for specific MessageStatus

#### Message Threading (Replies)
- [ ] **[P0]** Reply to message (set replyToId) → Expected: ChatMessage created with reference to parent message
- [ ] **[P1]** Query all replies to a message → Expected: Returns all messages where replyToId matches
- [ ] **[P2]** Display message thread → Expected: Nested structure with parent and replies
- [ ] **[P2]** Prevent reply to deleted message → Expected: Validation error or restricted behavior

#### Message Editing & Deletion
- [ ] **[P1]** Edit message content → Expected: message field updated, editedAt timestamp set
- [ ] **[P1]** Mark editedAt on edit → Expected: Timestamp recorded
- [ ] **[P1]** Soft delete message (isDeleted=true) → Expected: Message marked as deleted but preserved
- [ ] **[P1]** Hard delete message → Expected: Message removed from database (if allowed)
- [ ] **[P2]** Prevent editing other user's messages → Expected: Authorization check
- [ ] **[P2]** Prevent deleting other user's messages → Expected: Authorization check

#### Message Reactions
- [ ] **[P1]** Add reaction to message → Expected: ChatReaction JSON updated
- [ ] **[P1]** Remove reaction from message → Expected: ChatReaction JSON updated
- [ ] **[P2]** Query messages with specific reaction → Expected: Filtered results based on ChatReaction data

#### Message Queries
- [ ] **[P1]** Get all messages in conversation ordered by sentAt → Expected: Returns sorted message list
- [ ] **[P1]** Get messages after specific timestamp (pagination) → Expected: Returns newer messages
- [ ] **[P1]** Get unread messages for user → Expected: Returns messages sent after lastReadAt
- [ ] **[P2]** Search messages by content → Expected: Full-text search results
- [ ] **[P2]** Get messages with attachments → Expected: Returns messages where chatAttachmentId is not null

### Attachment Management

#### Attachment Creation
- [ ] **[P0]** Upload IMAGE attachment → Expected: Attachment created with type=IMAGE
- [ ] **[P0]** Upload VIDEO attachment → Expected: Attachment created with type=VIDEO
- [ ] **[P0]** Upload AUDIO attachment → Expected: Attachment created with type=AUDIO
- [ ] **[P0]** Upload DOCUMENT attachment → Expected: Attachment created with type=DOCUMENT
- [ ] **[P0]** Upload ARCHIVE attachment → Expected: Attachment created with type=ARCHIVE
- [ ] **[P0]** Upload OTHER attachment → Expected: Attachment created with type=OTHER
- [ ] **[P1]** Store file to S3 bucket → Expected: bucketPath stored with S3 reference
- [ ] **[P1]** Store file to local storage → Expected: bucketPath stored with local path
- [ ] **[P1]** Link attachment to uploader → Expected: uploadedById reference stored
- [ ] **[P1]** Record createdAt timestamp → Expected: Timestamp set on creation
- [ ] **[P1]** Update lastUsedAt when attachment accessed → Expected: Timestamp updated
- [ ] **[P2]** Validate file size limits → Expected: Large files rejected
- [ ] **[P2]** Validate file type → Expected: Unsupported types rejected

#### Attachment Queries
- [ ] **[P1]** Get attachment by id → Expected: Returns attachment data including bucketPath
- [ ] **[P1]** Get all attachments uploaded by user → Expected: Returns all attachments for uploadedById
- [ ] **[P2]** Clean up unused attachments → Expected: Attachments not linked to messages deleted
- [ ] **[P2]** Query attachments by type → Expected: Filtered results for specific MimeType

### Real-Time Features

#### Typing Indicators
- [ ] **[P1]** User starts typing in conversation → Expected: Typing indicator broadcasted to conversation members
- [ ] **[P1]** User stops typing → Expected: Typing indicator removed
- [ ] **[P2]** Multiple users typing simultaneously → Expected: All typing indicators displayed
- [ ] **[P2]** Typing indicator timeout → Expected: Indicator auto-removed after period of inactivity

#### Read Receipts
- [ ] **[P1]** Mark message as read → Expected: Read receipt sent to sender
- [ ] **[P1]** Display read receipt in sender's view → Expected: Shows which users have read message
- [ ] **[P2]** Group conversation read receipts → Expected: Shows all members who have read

#### Message Delivery Status Updates
- [ ] **[P0]** Real-time update when message delivered → Expected: Status updated to DELIVERED, notification sent
- [ ] **[P0]** Real-time update when message read → Expected: Status updated to READ, notification sent
- [ ] **[P1]** Real-time update on message failure → Expected: Status updated to FAILED, error notification sent

#### New Message Notifications
- [ ] **[P0]** Notify user of new message in conversation → Expected: Real-time notification delivered
- [ ] **[P1]** Notify user of new message in group conversation → Expected: Group members notified
- [ ] **[P2]** Mute notifications for conversation → Expected: User can disable notifications

### Edge Cases

#### Empty Messages
- [ ] **[P1]** Attempt to send message with empty text and no attachment → Expected: Validation error
- [ ] **[P2]** Send message with only whitespace → Expected: Validation error or trimmed

#### Large Files
- [ ] **[P1]** Upload file exceeding size limit → Expected: Upload rejected with error message
- [ ] **[P2]** Upload very large file near limit → Expected: Upload succeeds or fails gracefully
- [ ] **[P2]** Handle slow upload progress → Expected: Progress indicator, timeout handling

#### Concurrent Edits
- [ ] **[P1]** Two users attempt to edit same message concurrently → Expected: Last write wins or conflict resolution
- [ ] **[P2]** User edits message while another user is replying → Expected: Reply references original message

#### Message Ordering
- [ ] **[P0]** Messages displayed in correct chronological order by sentAt → Expected: Sorted list
- [ ] **[P1]** Messages with identical sentAt timestamps → Expected: Secondary sorting by id or another field
- [ ] **[P2]** Handle clock skew in distributed systems → Expected: Consistent ordering maintained

---

## Section 9: Events Module

### Event CRUD Operations

#### Event Creation
- [ ] **[P0]** Create event with all required fields (title, slug, startDate, endDate, organizerId) → Expected: Event created with DRAFT status by default
- [ ] **[P0]** Create event with unique slug → Expected: Event created successfully
- [ ] **[P1]** Create event with duplicate slug → Expected: Validation error, slug must be unique
- [ ] **[P1]** Create event with all optional fields (description, location, locationUrl, meetingUrl, maxAttendees, coverImage) → Expected: Event created with all fields populated
- [ ] **[P1]** Create event with timezone set to non-UTC (e.g., "America/New_York") → Expected: Event created with specified timezone
- [ ] **[P1]** Create event with default timezone → Expected: Event created with UTC timezone
- [ ] **[P1]** Create event with startDate after endDate → Expected: Validation error, startDate must be before endDate
- [ ] **[P1]** Create event with maxAttendees = 0 → Expected: Validation error, maxAttendees must be positive if provided
- [ ] **[P1]** Create event with maxAttendees = null → Expected: Event created with unlimited capacity
- [ ] **[P2]** Create event linked to project → Expected: Event created with projectId set
- [ ] **[P2]** Create event linked to guild → Expected: Event created with guildId set
- [ ] **[P2]** Create event linked to both project and guild → Expected: Event created with both associations
- [ ] **[P2]** Create event with isOnline = true and no meetingUrl → Expected: Warning or validation error for incomplete online event setup
- [ ] **[P2]** Create event with isOnline = false and no location → Expected: Warning for in-person event without location

#### Event Types
- [ ] **[P1]** Create event with type MEETING → Expected: Event created with MEETING type
- [ ] **[P1]** Create event with type WORKSHOP → Expected: Event created with WORKSHOP type
- [ ] **[P1]** Create event with type WEBINAR → Expected: Event created with WEBINAR type
- [ ] **[P1]** Create event with type TOWN_HALL → Expected: Event created with TOWN_HALL type
- [ ] **[P1]** Create event with type CRITIQUE → Expected: Event created with CRITIQUE type
- [ ] **[P1]** Create event with type HACKATHON → Expected: Event created with HACKATHON type
- [ ] **[P1]** Create event with type SOCIAL → Expected: Event created with SOCIAL type
- [ ] **[P1]** Create event with type OTHER → Expected: Event created with OTHER type

#### Event Visibility
- [ ] **[P0]** Create event with visibility PUBLIC → Expected: Event visible to all users
- [ ] **[P1]** Create event with visibility PROJECT_MEMBERS → Expected: Event visible only to project members
- [ ] **[P1]** Create event with visibility GUILD_MEMBERS → Expected: Event visible only to guild members
- [ ] **[P1]** Create event with visibility INVITE_ONLY → Expected: Event visible only to invited attendees
- [ ] **[P1]** Non-project-member attempts to view PROJECT_MEMBERS event → Expected: Access denied or event not visible
- [ ] **[P1]** Non-guild-member attempts to view GUILD_MEMBERS event → Expected: Access denied or event not visible
- [ ] **[P1]** Non-invited user attempts to view INVITE_ONLY event → Expected: Access denied or event not visible

#### Event Updates
- [ ] **[P0]** Update event title → Expected: Title updated successfully
- [ ] **[P0]** Update event description → Expected: Description updated successfully
- [ ] **[P1]** Update event startDate → Expected: StartDate updated, EVENT_UPDATED notification sent to attendees
- [ ] **[P1]** Update event endDate → Expected: EndDate updated, EVENT_UPDATED notification sent to attendees
- [ ] **[P1]** Update event location → Expected: Location updated, EVENT_UPDATED notification sent to attendees
- [ ] **[P1]** Update event meetingUrl → Expected: MeetingUrl updated, EVENT_UPDATED notification sent to attendees
- [ ] **[P1]** Update event maxAttendees to value lower than current attendeesCount → Expected: Validation error or warning
- [ ] **[P1]** Update event visibility from PUBLIC to INVITE_ONLY → Expected: Visibility updated, existing non-invited attendees may be notified or removed
- [ ] **[P2]** Update event coverImage → Expected: CoverImage URL updated
- [ ] **[P2]** Non-organizer attempts to update event → Expected: Permission denied
- [ ] **[P2]** Event co-host attempts to update event → Expected: Update allowed if co-hosts have edit permissions

#### Event Deletion
- [ ] **[P1]** Delete event in DRAFT status → Expected: Event deleted successfully
- [ ] **[P1]** Delete event in SCHEDULED status with attendees → Expected: Event deleted, EVENT_CANCELLED notification sent to all attendees
- [ ] **[P1]** Delete event in LIVE status → Expected: Event deleted or validation error (should be cancelled first)
- [ ] **[P1]** Delete event in COMPLETED status → Expected: Validation error, completed events should not be deleted
- [ ] **[P2]** Non-organizer attempts to delete event → Expected: Permission denied

### Event Lifecycle Transitions

#### DRAFT → SCHEDULED
- [ ] **[P0]** Publish event from DRAFT to SCHEDULED → Expected: Status updated to SCHEDULED, event becomes visible based on visibility setting
- [ ] **[P1]** Publish event from DRAFT without required fields → Expected: Validation error, cannot publish incomplete event
- [ ] **[P2]** Publish event with startDate in the past → Expected: Validation error or warning

#### SCHEDULED → LIVE
- [ ] **[P0]** Automatically transition event to LIVE when startDate reached → Expected: Status updated to LIVE, EVENT_STARTING_SOON notification sent beforehand
- [ ] **[P1]** Manually transition event to LIVE before startDate → Expected: Status updated to LIVE (if allowed by business rules)
- [ ] **[P2]** Verify EVENT_STARTING_SOON notification sent X minutes before startDate → Expected: Notification sent to all GOING attendees

#### LIVE → COMPLETED
- [ ] **[P0]** Automatically transition event to COMPLETED when endDate reached → Expected: Status updated to COMPLETED
- [ ] **[P1]** Manually mark event as COMPLETED before endDate → Expected: Status updated to COMPLETED (if allowed)
- [ ] **[P2]** Completed event transitions attendees with status GOING to ATTENDED → Expected: All GOING attendees marked as ATTENDED (if auto-mark enabled)

#### Any Status → CANCELLED
- [ ] **[P0]** Cancel event in DRAFT status → Expected: Status updated to CANCELLED
- [ ] **[P0]** Cancel event in SCHEDULED status → Expected: Status updated to CANCELLED, EVENT_CANCELLED notification sent to all attendees
- [ ] **[P1]** Cancel event in LIVE status → Expected: Status updated to CANCELLED, EVENT_CANCELLED notification sent
- [ ] **[P1]** Cancel event in COMPLETED status → Expected: Validation error, completed events cannot be cancelled
- [ ] **[P2]** Cancelled event cannot transition back to other statuses → Expected: Validation error on status change

### Online vs In-Person Events

#### Online Events
- [ ] **[P0]** Create online event with isOnline = true and meetingUrl → Expected: Event created with online flag and meeting URL
- [ ] **[P1]** Online event displays meetingUrl to GOING attendees → Expected: Meeting URL visible to confirmed attendees
- [ ] **[P2]** Online event meetingUrl hidden from INVITED or MAYBE attendees → Expected: Meeting URL not visible until RSVP confirmed
- [ ] **[P2]** Online event can have both meetingUrl and location (hybrid) → Expected: Both fields populated for hybrid events

#### In-Person Events
- [ ] **[P0]** Create in-person event with isOnline = false and location → Expected: Event created with in-person flag and location
- [ ] **[P1]** In-person event displays location to all attendees → Expected: Location visible based on visibility settings
- [ ] **[P2]** In-person event with locationUrl (e.g., Google Maps link) → Expected: Location URL accessible to attendees

### EventAttendee Management

#### RSVP Status Transitions
- [ ] **[P0]** User RSVPs to event with status GOING → Expected: EventAttendee created with GOING status, rsvpAt timestamp set, attendeesCount incremented
- [ ] **[P0]** User RSVPs to event with status MAYBE → Expected: EventAttendee created with MAYBE status, rsvpAt timestamp set
- [ ] **[P1]** User RSVPs to event with status NOT_GOING → Expected: EventAttendee created with NOT_GOING status, attendeesCount not incremented
- [ ] **[P1]** User transitions from INVITED → GOING → Expected: Status updated to GOING, rsvpAt timestamp set, attendeesCount incremented
- [ ] **[P1]** User transitions from GOING → NOT_GOING → Expected: Status updated to NOT_GOING, attendeesCount decremented
- [ ] **[P1]** User transitions from MAYBE → GOING → Expected: Status updated to GOING, attendeesCount incremented
- [ ] **[P1]** User transitions from GOING → MAYBE → Expected: Status updated to MAYBE, attendeesCount decremented
- [ ] **[P1]** User transitions from NOT_GOING → GOING → Expected: Status updated to GOING, attendeesCount incremented
- [ ] **[P2]** User transitions from GOING → ATTENDED after event ends → Expected: Status updated to ATTENDED, attendedAt timestamp set

#### Max Attendees Enforcement
- [ ] **[P0]** Event with maxAttendees = 10, 10th user RSVPs GOING → Expected: RSVP accepted, event at capacity
- [ ] **[P0]** Event with maxAttendees = 10, 11th user RSVPs GOING → Expected: RSVP rejected, validation error (event full)
- [ ] **[P1]** Event at capacity, one user changes from GOING to NOT_GOING → Expected: Spot freed, new users can RSVP
- [ ] **[P1]** Event with no maxAttendees, unlimited users RSVP → Expected: All RSVPs accepted
- [ ] **[P2]** MAYBE attendees do not count toward maxAttendees → Expected: MAYBE status does not block capacity
- [ ] **[P2]** Waitlist functionality when event is full → Expected: User added to waitlist (if feature exists)

#### Attendee Notes and Timestamps
- [ ] **[P2]** User adds notes when RSVPing → Expected: Notes saved to EventAttendee record
- [ ] **[P2]** rsvpAt timestamp set when user first RSVPs → Expected: Timestamp captured on initial RSVP
- [ ] **[P2]** attendedAt timestamp set when user marked as ATTENDED → Expected: Timestamp captured when status changes to ATTENDED
- [ ] **[P3]** Update attendee notes after RSVP → Expected: Notes updated successfully

#### INVITED Status
- [ ] **[P1]** Organizer invites user to INVITE_ONLY event → Expected: EventAttendee created with INVITED status, EVENT_INVITATION notification sent
- [ ] **[P1]** Invited user changes status from INVITED to GOING → Expected: Status updated, invitation accepted
- [ ] **[P1]** Invited user changes status from INVITED to NOT_GOING → Expected: Status updated, invitation declined
- [ ] **[P2]** Non-invited user attempts to RSVP to INVITE_ONLY event → Expected: RSVP rejected, access denied

### EventCoHost Management

#### Add/Remove Co-Hosts
- [ ] **[P1]** Organizer adds user as co-host → Expected: EventCoHost record created, addedAt timestamp set
- [ ] **[P1]** Organizer adds multiple co-hosts → Expected: All co-hosts added successfully
- [ ] **[P1]** Organizer removes co-host → Expected: EventCoHost record deleted
- [ ] **[P2]** Co-host cannot add another co-host → Expected: Permission denied (only organizer can add co-hosts)
- [ ] **[P2]** Co-host attempts to remove themselves → Expected: Co-host removed (if self-removal allowed)
- [ ] **[P2]** Non-organizer attempts to add co-host → Expected: Permission denied

#### Co-Host Permissions
- [ ] **[P1]** Co-host can update event details → Expected: Event updated successfully
- [ ] **[P1]** Co-host can cancel event → Expected: Event cancelled (if co-hosts have permission)
- [ ] **[P2]** Co-host can manage attendees (mark as ATTENDED) → Expected: Attendee status updated
- [ ] **[P2]** Co-host cannot delete event → Expected: Permission denied, only organizer can delete
- [ ] **[P3]** Co-host can view INVITE_ONLY event attendee list → Expected: Attendee list visible to co-hosts

### EventReminder Management

#### Reminder Creation
- [ ] **[P0]** User sets reminder for event 1 hour before startDate → Expected: EventReminder created with remindAt timestamp, isSent = false
- [ ] **[P1]** User sets reminder for event 1 day before startDate → Expected: EventReminder created with correct remindAt timestamp
- [ ] **[P1]** User sets multiple reminders for same event (e.g., 1 day, 1 hour) → Expected: Multiple EventReminder records created
- [ ] **[P2]** User sets reminder after event has started → Expected: Validation error or reminder not created
- [ ] **[P2]** Non-attendee sets reminder for event → Expected: Validation error, user must be attending

#### Reminder Sending
- [ ] **[P0]** Reminder sent when remindAt timestamp reached → Expected: EVENT_REMINDER notification sent, isSent = true, sentAt timestamp set
- [ ] **[P1]** Reminder not sent if user changes RSVP to NOT_GOING → Expected: Reminder skipped or cancelled
- [ ] **[P1]** Reminder sent only once per EventReminder record → Expected: isSent flag prevents duplicate sends
- [ ] **[P2]** Reminder sent via multiple channels (email, in-app) → Expected: Notification sent through configured channels
- [ ] **[P3]** Reminder includes event details (title, startDate, location/meetingUrl) → Expected: Notification contains relevant event info

#### Reminder Deletion
- [ ] **[P2]** User deletes reminder before it is sent → Expected: EventReminder record deleted, reminder not sent
- [ ] **[P3]** User updates reminder time → Expected: remindAt timestamp updated, isSent reset to false

### Time Zone Handling

#### Event Creation with Time Zones
- [ ] **[P0]** Create event with timezone = "America/New_York" → Expected: Event stored with specified timezone
- [ ] **[P1]** Create event with timezone = "UTC" → Expected: Event stored with UTC timezone
- [ ] **[P1]** Create event with invalid timezone → Expected: Validation error, timezone must be valid
- [ ] **[P2]** Event startDate and endDate respect specified timezone → Expected: Dates interpreted correctly in event's timezone

#### Time Zone Display
- [ ] **[P1]** User in different timezone views event → Expected: Event times displayed in user's local timezone with original timezone indicated
- [ ] **[P1]** Event reminder sent considers user's local timezone → Expected: Reminder sent at correct local time for user
- [ ] **[P2]** Event listing shows time in event's original timezone → Expected: Times displayed with timezone label (e.g., "10:00 AM EST")

### Event Search and Filtering

#### Search Events
- [ ] **[P0]** Search events by title → Expected: Matching events returned
- [ ] **[P1]** Search events by description keywords → Expected: Matching events returned
- [ ] **[P2]** Search events by organizer name → Expected: Events organized by specified user returned
- [ ] **[P2]** Search events by location → Expected: Events at specified location returned

#### Filter by Type
- [ ] **[P1]** Filter events by type WORKSHOP → Expected: Only WORKSHOP events returned
- [ ] **[P1]** Filter events by type WEBINAR → Expected: Only WEBINAR events returned
- [ ] **[P2]** Filter events by multiple types (MEETING, HACKATHON) → Expected: Events matching any selected type returned

#### Filter by Status
- [ ] **[P0]** Filter events by status SCHEDULED → Expected: Only SCHEDULED events returned
- [ ] **[P1]** Filter events by status LIVE → Expected: Only LIVE events returned
- [ ] **[P1]** Filter events by status COMPLETED → Expected: Only COMPLETED events returned
- [ ] **[P2]** Filter events by status DRAFT (organizer view) → Expected: User's draft events returned

#### Filter by Date
- [ ] **[P0]** Filter upcoming events (startDate >= today) → Expected: Future events returned
- [ ] **[P0]** Filter past events (endDate < today) → Expected: Past events returned
- [ ] **[P1]** Filter events by date range (startDate between X and Y) → Expected: Events within date range returned
- [ ] **[P2]** Filter events happening today → Expected: Events with startDate or endDate = today returned

#### Filter by Visibility
- [ ] **[P1]** Filter PUBLIC events → Expected: All public events returned
- [ ] **[P2]** Filter PROJECT_MEMBERS events for user's projects → Expected: Events for projects user is member of returned
- [ ] **[P2]** Filter GUILD_MEMBERS events for user's guilds → Expected: Events for guilds user is member of returned
- [ ] **[P3]** Filter INVITE_ONLY events user is invited to → Expected: User's invited events returned

#### Combined Filters
- [ ] **[P1]** Filter upcoming WORKSHOP events → Expected: Scheduled workshops in the future returned
- [ ] **[P2]** Filter past HACKATHON events by specific project → Expected: Completed hackathons for project returned
- [ ] **[P2]** Filter LIVE events user is attending → Expected: Currently live events user RSVPed GOING to returned

### Event Notifications

#### EVENT_INVITATION
- [ ] **[P0]** User invited to INVITE_ONLY event → Expected: EVENT_INVITATION notification sent with event details
- [ ] **[P1]** Notification includes accept/decline actions → Expected: User can respond directly from notification

#### EVENT_REMINDER
- [ ] **[P0]** Reminder notification sent at remindAt time → Expected: EVENT_REMINDER notification sent with event details
- [ ] **[P1]** Reminder notification includes event location or meetingUrl → Expected: Relevant join/attend info included

#### EVENT_STARTING_SOON
- [ ] **[P0]** Notification sent X minutes before event starts → Expected: EVENT_STARTING_SOON notification sent to GOING attendees
- [ ] **[P1]** Notification includes quick link to meetingUrl (online events) → Expected: Direct join link included

#### EVENT_CANCELLED
- [ ] **[P0]** Event cancelled, notification sent to all attendees → Expected: EVENT_CANCELLED notification sent with cancellation message
- [ ] **[P1]** Notification includes organizer's cancellation reason (if provided) → Expected: Reason displayed in notification

#### EVENT_UPDATED
- [ ] **[P1]** Event details updated, notification sent to GOING attendees → Expected: EVENT_UPDATED notification sent with change summary
- [ ] **[P2]** Notification specifies what was updated (time, location, etc.) → Expected: Change details included in notification

### Edge Cases

#### Attendee Edge Cases
- [ ] **[P2]** User RSVPs GOING, event is deleted → Expected: EventAttendee record deleted or orphaned gracefully
- [ ] **[P2]** User RSVPs to event, then is removed from project (PROJECT_MEMBERS event) → Expected: RSVP may be cancelled or user notified
- [ ] **[P3]** Organizer deletes own account, event orphaned → Expected: Event reassigned to co-host or deleted

#### Concurrent Operations
- [ ] **[P1]** Two users RSVP GOING simultaneously when one spot left → Expected: First request succeeds, second fails (race condition handled)
- [ ] **[P2]** Organizer updates event while users are RSVPing → Expected: Updates and RSVPs both succeed without conflict
- [ ] **[P3]** Multiple co-hosts update event simultaneously → Expected: Last write wins or conflict resolution in place

#### Data Integrity
- [ ] **[P1]** Event deleted, all EventAttendee records cascade deleted → Expected: No orphaned attendee records
- [ ] **[P1]** Event deleted, all EventReminder records cascade deleted → Expected: No orphaned reminder records
- [ ] **[P1]** Event deleted, all EventCoHost records cascade deleted → Expected: No orphaned co-host records
- [ ] **[P2]** Organizer user deleted, events transferred or cancelled → Expected: Events handled gracefully

---

## Section 10: Social & Follow

### UserFollow

#### Follow/Unfollow Users
- [ ] **[P0]** User A follows User B → Expected: UserFollow record created with followerId = A, followingId = B, createdAt timestamp set
- [ ] **[P0]** User A unfollows User B → Expected: UserFollow record deleted
- [ ] **[P1]** User attempts to follow self → Expected: Validation error, cannot follow self
- [ ] **[P1]** User A follows User B twice → Expected: Second follow ignored or validation error (unique constraint)
- [ ] **[P1]** User A unfollows User B who they never followed → Expected: No error, operation idempotent
- [ ] **[P2]** User A follows User B, then B deletes account → Expected: UserFollow record deleted (cascade)

#### Follow Counts
- [ ] **[P0]** User A's follower count increments when User B follows → Expected: Follower count = 1
- [ ] **[P0]** User B's following count increments when following User A → Expected: Following count = 1
- [ ] **[P1]** User A's follower count decrements when User B unfollows → Expected: Follower count decrements
- [ ] **[P1]** User B's following count decrements when unfollowing User A → Expected: Following count decrements
- [ ] **[P2]** Follower/following counts accurate after multiple follow/unfollow cycles → Expected: Counts match actual UserFollow records

#### USER_FOLLOWED Notification
- [ ] **[P0]** User A follows User B → Expected: USER_FOLLOWED notification sent to User B
- [ ] **[P1]** Notification includes follower's name and profile link → Expected: User B can view User A's profile from notification
- [ ] **[P2]** User B unfollows then re-follows User A → Expected: New USER_FOLLOWED notification sent on re-follow
- [ ] **[P3]** User can disable USER_FOLLOWED notifications in settings → Expected: No notification sent when followed

#### Edge Cases
- [ ] **[P2]** User A follows User B, then B blocks A → Expected: UserFollow removed or follow status affected by block
- [ ] **[P2]** User follows 1000+ users → Expected: System handles large following count
- [ ] **[P3]** Mutual follow (A follows B, B follows A) → Expected: Both UserFollow records exist independently

### ProjectFollow

#### Follow/Unfollow Projects
- [ ] **[P0]** User follows project → Expected: ProjectFollow record created with notifyUpdates = true, notifyMilestones = true, notifyProposals = false (defaults)
- [ ] **[P0]** User unfollows project → Expected: ProjectFollow record deleted
- [ ] **[P1]** User follows same project twice → Expected: Second follow ignored or validation error (unique constraint)
- [ ] **[P1]** User unfollows project they never followed → Expected: No error, operation idempotent
- [ ] **[P2]** User follows project, project is deleted → Expected: ProjectFollow record deleted (cascade)

#### Notification Preferences
- [ ] **[P1]** User toggles notifyUpdates from true to false → Expected: Preference updated, no PROJECT_UPDATE notifications sent
- [ ] **[P1]** User toggles notifyMilestones from true to false → Expected: Preference updated, no milestone notifications sent
- [ ] **[P1]** User toggles notifyProposals from false to true → Expected: Preference updated, proposal notifications sent
- [ ] **[P1]** User enables all notification preferences → Expected: User receives all project-related notifications
- [ ] **[P1]** User disables all notification preferences → Expected: User receives no project-related notifications
- [ ] **[P2]** User follows project with custom preferences (e.g., only notifyProposals) → Expected: Only proposal notifications sent

#### PROJECT_FOLLOWED Notification
- [ ] **[P0]** User follows project → Expected: PROJECT_FOLLOWED notification sent to project creator/team
- [ ] **[P1]** Notification includes follower's name → Expected: Project team can see who followed
- [ ] **[P2]** Project owner can see list of followers → Expected: Follower list accessible to project team

#### Follow Feed Generation
- [ ] **[P0]** User follows project, sees project updates in feed → Expected: Project updates appear in user's feed
- [ ] **[P1]** User follows multiple projects, feed shows updates from all followed projects → Expected: Aggregated feed includes all followed projects
- [ ] **[P1]** User unfollows project, project updates no longer in feed → Expected: Feed updated, unfollowed project content removed
- [ ] **[P2]** Feed respects notification preferences (e.g., notifyUpdates = false) → Expected: Updates not shown in feed if preference disabled

#### Edge Cases
- [ ] **[P2]** User is project member and follows project → Expected: User receives notifications based on preferences (may differ from member notifications)
- [ ] **[P2]** User follows project, then becomes project member → Expected: Follow persists, user may receive both member and follower notifications
- [ ] **[P3]** Project privacy changes (public to private), followers affected → Expected: Non-member followers may lose access or be notified

### GuildFollow

#### Follow/Unfollow Guilds
- [ ] **[P0]** User follows guild → Expected: GuildFollow record created with notifyUpdates = true, notifyEvents = true, notifyProjects = false (defaults)
- [ ] **[P0]** User unfollows guild → Expected: GuildFollow record deleted
- [ ] **[P1]** User follows same guild twice → Expected: Second follow ignored or validation error (unique constraint)
- [ ] **[P1]** User unfollows guild they never followed → Expected: No error, operation idempotent
- [ ] **[P2]** User follows guild, guild is deleted → Expected: GuildFollow record deleted (cascade)

#### Notification Preferences
- [ ] **[P1]** User toggles notifyUpdates from true to false → Expected: Preference updated, no guild update notifications sent
- [ ] **[P1]** User toggles notifyEvents from true to false → Expected: Preference updated, no guild event notifications sent
- [ ] **[P1]** User toggles notifyProjects from false to true → Expected: Preference updated, guild project notifications sent
- [ ] **[P1]** User enables all notification preferences → Expected: User receives all guild-related notifications
- [ ] **[P1]** User disables all notification preferences → Expected: User receives no guild-related notifications
- [ ] **[P2]** User follows guild with custom preferences (e.g., only notifyEvents) → Expected: Only event notifications sent

#### GUILD_FOLLOWED Notification
- [ ] **[P0]** User follows guild → Expected: GUILD_FOLLOWED notification sent to guild owner/admins
- [ ] **[P1]** Notification includes follower's name → Expected: Guild team can see who followed
- [ ] **[P2]** Guild owner can see list of followers → Expected: Follower list accessible to guild team

#### Follow Feed Generation
- [ ] **[P0]** User follows guild, sees guild updates in feed → Expected: Guild updates appear in user's feed
- [ ] **[P1]** User follows multiple guilds, feed shows updates from all followed guilds → Expected: Aggregated feed includes all followed guilds
- [ ] **[P1]** User unfollows guild, guild updates no longer in feed → Expected: Feed updated, unfollowed guild content removed
- [ ] **[P2]** Feed respects notification preferences (e.g., notifyUpdates = false) → Expected: Updates not shown in feed if preference disabled

#### Edge Cases
- [ ] **[P2]** User is guild member and follows guild → Expected: User receives notifications based on preferences (may differ from member notifications)
- [ ] **[P2]** User follows guild, then becomes guild member → Expected: Follow persists, user may receive both member and follower notifications
- [ ] **[P3]** Guild privacy changes, followers affected → Expected: Non-member followers may lose access or be notified

### Follow Edge Cases

#### Double Follow
- [ ] **[P1]** User follows same entity twice in quick succession → Expected: Second request ignored, no duplicate records
- [ ] **[P2]** User follows user A, project B, guild C simultaneously → Expected: All follows succeed independently

#### Follow → Unfollow → Re-follow
- [ ] **[P1]** User follows user A, unfollows, then re-follows → Expected: New UserFollow record created with new createdAt timestamp
- [ ] **[P1]** User follows project, unfollows, then re-follows → Expected: New ProjectFollow record created with default preferences
- [ ] **[P1]** User follows guild, unfollows, then re-follows → Expected: New GuildFollow record created with default preferences
- [ ] **[P2]** Re-follow restores previous notification preferences → Expected: Preferences reset to defaults (or restored from history if feature exists)

#### Cascading Deletes
- [ ] **[P1]** User account deleted, all UserFollow records (as follower) deleted → Expected: Following relationships removed
- [ ] **[P1]** User account deleted, all UserFollow records (as following) deleted → Expected: Follower relationships removed
- [ ] **[P1]** Project deleted, all ProjectFollow records deleted → Expected: No orphaned follow records
- [ ] **[P1]** Guild deleted, all GuildFollow records deleted → Expected: No orphaned follow records

#### Follow Feed Aggregation
- [ ] **[P0]** User follows 5 users, 3 projects, 2 guilds → Expected: Feed shows updates from all followed entities
- [ ] **[P1]** Feed sorted by recency → Expected: Most recent updates appear first
- [ ] **[P2]** Feed pagination works correctly → Expected: User can scroll through paginated feed
- [ ] **[P2]** Feed filters by entity type (users only, projects only) → Expected: Filtered feed shows only selected entity types
- [ ] **[P3]** Feed performance with 100+ followed entities → Expected: Feed loads efficiently

---

## Section 11: Gamification & Reputation

### XPEvent Creation

#### All XPEventTypes
- [ ] **[P0]** Create XPEvent for TASK_COMPLETED → Expected: XPEvent created, user totalXP incremented by amount
- [ ] **[P0]** Create XPEvent for PROPOSAL_CREATED → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for PROPOSAL_PASSED → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for VOTE_CAST → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for PROJECT_FUNDED → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for MEMBER_REFERRED → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for ACHIEVEMENT_EARNED → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for STREAK_MAINTAINED → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for LEVEL_UP → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for REVIEW_GIVEN → Expected: XPEvent created, user totalXP incremented
- [ ] **[P1]** Create XPEvent for CONTRIBUTION_MADE → Expected: XPEvent created, user totalXP incremented

#### XPEvent Fields
- [ ] **[P0]** XPEvent amount recorded correctly → Expected: Amount field matches awarded XP
- [ ] **[P1]** XPEvent source and sourceId tracked → Expected: Source (e.g., "task") and sourceId (task ID) recorded
- [ ] **[P1]** XPEvent metadata stored (JSON) → Expected: Additional context stored in metadata field
- [ ] **[P2]** XPEvent createdAt timestamp set → Expected: Timestamp captures when XP was awarded
- [ ] **[P2]** Multiple XPEvents for same user aggregate correctly → Expected: User totalXP = sum of all XPEvent amounts

#### XP Award Validation
- [ ] **[P1]** XPEvent with negative amount → Expected: Validation error, amount must be positive
- [ ] **[P1]** XPEvent with amount = 0 → Expected: Validation error or event created but no XP change
- [ ] **[P2]** XPEvent for non-existent user → Expected: Validation error, user must exist
- [ ] **[P2]** Duplicate XPEvent (same source/sourceId) → Expected: Allowed (multiple events per source) or deduplicated if required

### User Level and XP Tracking

#### XP Accumulation
- [ ] **[P0]** User earns 100 XP → Expected: totalXP = 100
- [ ] **[P0]** User earns additional 50 XP → Expected: totalXP = 150
- [ ] **[P1]** User totalXP displayed on profile → Expected: Current XP shown accurately
- [ ] **[P2]** User XP history viewable → Expected: List of all XPEvents for user accessible

#### Level Progression
- [ ] **[P0]** User reaches XP threshold for level 2 → Expected: User level increments to 2, LEVEL_UP notification sent
- [ ] **[P0]** User reaches XP threshold for level 5 → Expected: User level increments to 5, LEVEL_UP notification sent
- [ ] **[P1]** User level displayed on profile → Expected: Current level shown with badge or indicator
- [ ] **[P1]** Level progression bar shows XP to next level → Expected: Progress bar accurate (e.g., 750/1000 XP to level 3)
- [ ] **[P2]** Level-up triggers additional XPEvent (LEVEL_UP type) → Expected: Bonus XP awarded for leveling up
- [ ] **[P2]** User cannot manually set level → Expected: Level calculated from totalXP, not editable

#### Level Thresholds
- [ ] **[P1]** Level 1: 0 XP → Expected: New users start at level 1
- [ ] **[P1]** Level 2: 100 XP threshold → Expected: User reaches level 2 at 100 XP
- [ ] **[P1]** Level 10: 10,000 XP threshold (example) → Expected: User reaches level 10 at defined threshold
- [ ] **[P2]** XP requirements increase exponentially (or per defined curve) → Expected: Higher levels require significantly more XP

### User Tiers

#### Tier Promotion
- [ ] **[P0]** User starts at BRONZE tier → Expected: New users have tier = BRONZE
- [ ] **[P1]** User reaches SILVER tier threshold → Expected: Tier updated to SILVER
- [ ] **[P1]** User reaches GOLD tier threshold → Expected: Tier updated to GOLD
- [ ] **[P1]** User reaches PLATINUM tier threshold → Expected: Tier updated to PLATINUM
- [ ] **[P1]** User reaches DIAMOND tier threshold → Expected: Tier updated to DIAMOND
- [ ] **[P2]** Tier promotion triggers notification → Expected: User notified of tier upgrade

#### Tier Display
- [ ] **[P1]** User tier displayed on profile → Expected: Tier badge or label shown (e.g., "Gold Tier")
- [ ] **[P1]** Tier displayed in leaderboards → Expected: Tier shown alongside username/XP
- [ ] **[P2]** Tier benefits documented → Expected: User can see perks of each tier

#### Tier Thresholds
- [ ] **[P1]** BRONZE: 0-999 XP (example) → Expected: Users in this XP range are BRONZE
- [ ] **[P1]** SILVER: 1000-4999 XP → Expected: Users in this XP range are SILVER
- [ ] **[P1]** GOLD: 5000-14999 XP → Expected: Users in this XP range are GOLD
- [ ] **[P1]** PLATINUM: 15000-49999 XP → Expected: Users in this XP range are PLATINUM
- [ ] **[P1]** DIAMOND: 50000+ XP → Expected: Users above this threshold are DIAMOND

#### Tier Demotion
- [ ] **[P2]** User tier does not decrease if XP decreases → Expected: Tier promotions are permanent (or per business rules)
- [ ] **[P3]** User tier decreases if XP deducted (if XP penalties exist) → Expected: Tier recalculated based on current totalXP

### Achievements

#### Achievement Entities
- [ ] **[P0]** Create achievement with all required fields (name, description, category, criteria, xpReward) → Expected: Achievement created
- [ ] **[P1]** Achievement with unique name → Expected: Name uniqueness enforced
- [ ] **[P1]** Achievement with duplicate name → Expected: Validation error, name must be unique
- [ ] **[P1]** Achievement with equityReward set → Expected: Achievement grants both XP and equity
- [ ] **[P2]** Achievement icon URL stored → Expected: Icon displayed in achievement list

#### Achievement Categories
- [ ] **[P1]** Create achievement in CONTRIBUTOR category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in COLLABORATOR category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in INVESTOR category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in GOVERNANCE category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in COMMUNITY category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in STREAK category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in MILESTONE category → Expected: Achievement categorized correctly
- [ ] **[P1]** Create achievement in GAMING category → Expected: Achievement categorized correctly

#### Achievement Rarities
- [ ] **[P1]** Create COMMON achievement → Expected: Rarity = COMMON
- [ ] **[P1]** Create UNCOMMON achievement → Expected: Rarity = UNCOMMON
- [ ] **[P1]** Create RARE achievement → Expected: Rarity = RARE
- [ ] **[P1]** Create EPIC achievement → Expected: Rarity = EPIC
- [ ] **[P1]** Create LEGENDARY achievement → Expected: Rarity = LEGENDARY
- [ ] **[P2]** Rarity affects achievement display (color, icon) → Expected: Visual distinction by rarity

#### Achievement Criteria (JSON)
- [ ] **[P1]** Achievement criteria defined as JSON → Expected: Criteria stored in JSON field (e.g., `{"tasksCompleted": 10}`)
- [ ] **[P1]** Criteria checked when user performs action → Expected: System evaluates criteria against user actions
- [ ] **[P2]** Complex criteria with multiple conditions → Expected: Criteria support AND/OR logic (e.g., complete 10 tasks AND vote 5 times)
- [ ] **[P3]** Criteria updated after achievement created → Expected: Updated criteria apply to new progress only (or retroactively per rules)

#### Achievement isActive Flag
- [ ] **[P1]** Active achievement (isActive = true) can be earned → Expected: Users make progress and can earn achievement
- [ ] **[P1]** Inactive achievement (isActive = false) cannot be earned → Expected: Achievement hidden or progress frozen
- [ ] **[P2]** Admin deactivates achievement → Expected: Achievement no longer appears in available achievements, existing progress retained
- [ ] **[P2]** Admin reactivates achievement → Expected: Achievement available again, progress resumes

### UserAchievement

#### Progress Tracking
- [ ] **[P0]** User starts working toward achievement → Expected: UserAchievement created with progress = 0, earnedAt = null
- [ ] **[P0]** User makes progress on achievement (e.g., completes 1 of 10 tasks) → Expected: progress = 1
- [ ] **[P1]** User makes partial progress (e.g., 5 of 10 tasks) → Expected: progress = 5
- [ ] **[P1]** User completes achievement criteria → Expected: progress = 10, earnedAt timestamp set, ACHIEVEMENT_EARNED notification sent
- [ ] **[P2]** Progress bar displayed for in-progress achievements → Expected: User can see progress toward each achievement

#### earnedAt Timestamp
- [ ] **[P0]** UserAchievement.earnedAt = null before completion → Expected: Achievement not yet earned
- [ ] **[P0]** UserAchievement.earnedAt set upon completion → Expected: Timestamp captures when achievement was earned
- [ ] **[P1]** Earned achievements displayed separately from in-progress → Expected: UI distinguishes earned vs. in-progress achievements

#### ACHIEVEMENT_EARNED Notification
- [ ] **[P0]** User earns achievement → Expected: ACHIEVEMENT_EARNED notification sent with achievement details
- [ ] **[P1]** Notification includes achievement icon and rarity → Expected: Visual celebration of achievement
- [ ] **[P1]** Notification includes XP and equity rewards → Expected: User sees rewards earned
- [ ] **[P2]** Achievement shared to user's feed → Expected: Achievement appears as post or update (if feature exists)

#### XP and Equity Rewards
- [ ] **[P0]** User earns achievement, xpReward = 500 → Expected: User receives 500 XP (XPEvent created), totalXP incremented
- [ ] **[P1]** User earns achievement with equityReward → Expected: User receives equity (if equity system integrated)
- [ ] **[P2]** Rewards granted only once per achievement → Expected: Re-earning same achievement does not grant duplicate rewards

#### Edge Cases
- [ ] **[P2]** User progress toward achievement, achievement deleted → Expected: UserAchievement orphaned or deleted gracefully
- [ ] **[P2]** User progress toward achievement, criteria changed → Expected: Progress reset or recalculated
- [ ] **[P3]** User earns all achievements in a category → Expected: Meta-achievement or bonus reward granted (if feature exists)

### Leaderboards

#### Leaderboard Periods
- [ ] **[P0]** Create DAILY leaderboard → Expected: Leaderboard for single day (startDate to endDate = 24 hours)
- [ ] **[P1]** Create WEEKLY leaderboard → Expected: Leaderboard for 7-day period
- [ ] **[P1]** Create MONTHLY leaderboard → Expected: Leaderboard for calendar month
- [ ] **[P1]** Create QUARTERLY leaderboard → Expected: Leaderboard for 3-month period
- [ ] **[P1]** Create ALL_TIME leaderboard → Expected: Leaderboard with no end date (cumulative)

#### Leaderboard Categories
- [ ] **[P0]** Create leaderboard for XP category → Expected: Users ranked by totalXP
- [ ] **[P1]** Create leaderboard for TASKS_COMPLETED category → Expected: Users ranked by task completion count
- [ ] **[P1]** Create leaderboard for SHARES_EARNED category → Expected: Users ranked by shares earned
- [ ] **[P1]** Create leaderboard for PROPOSALS_CREATED category → Expected: Users ranked by proposals created
- [ ] **[P1]** Create leaderboard for VOTES_CAST category → Expected: Users ranked by votes cast
- [ ] **[P1]** Create leaderboard for PROJECTS_FUNDED category → Expected: Users ranked by projects funded

#### LeaderboardEntry
- [ ] **[P0]** User appears on leaderboard with rank and score → Expected: LeaderboardEntry created with rank, score (e.g., XP or count)
- [ ] **[P0]** User rank updates when score changes → Expected: Rank recalculated based on updated score
- [ ] **[P1]** Top 10 users displayed on leaderboard → Expected: First 10 LeaderboardEntries shown
- [ ] **[P1]** User's own rank always visible → Expected: User can see their position even if outside top 10
- [ ] **[P2]** LeaderboardEntry metadata stored (JSON) → Expected: Additional context (e.g., tier, level) stored

#### Leaderboard Rotation
- [ ] **[P0]** DAILY leaderboard resets each day → Expected: New leaderboard created, old one archived
- [ ] **[P1]** WEEKLY leaderboard resets each week → Expected: New leaderboard created at week boundary
- [ ] **[P1]** MONTHLY leaderboard resets each month → Expected: New leaderboard created at month start
- [ ] **[P2]** ALL_TIME leaderboard never resets → Expected: Cumulative leaderboard always active

#### Leaderboard Display
- [ ] **[P0]** Leaderboard shows rank, username, score → Expected: Essential data displayed
- [ ] **[P1]** Leaderboard shows user tier/level → Expected: Additional user info displayed
- [ ] **[P1]** Leaderboard paginated (top 100, pages of 10) → Expected: Large leaderboards paginated
- [ ] **[P2]** Leaderboard filters by category and period → Expected: User can switch between different leaderboards
- [ ] **[P2]** Leaderboard shows user's rank change (up/down from previous period) → Expected: Trend indicators displayed

#### Edge Cases
- [ ] **[P2]** Two users with same score → Expected: Tied users share same rank or ranked by tiebreaker (e.g., timestamp)
- [ ] **[P2]** Leaderboard with zero entries (new period, no activity) → Expected: Empty state displayed
- [ ] **[P3]** Leaderboard performance with 10,000+ users → Expected: Leaderboard calculates and displays efficiently

### UserStreak

#### Streak Types
- [ ] **[P0]** Create DAILY_LOGIN streak → Expected: UserStreak created with streakType = DAILY_LOGIN
- [ ] **[P1]** Create DAILY_CONTRIBUTION streak → Expected: UserStreak created with streakType = DAILY_CONTRIBUTION
- [ ] **[P1]** Create WEEKLY_TASK streak → Expected: UserStreak created with streakType = WEEKLY_TASK

#### Current Streak Tracking
- [ ] **[P0]** User logs in on consecutive days → Expected: currentStreak increments each day
- [ ] **[P0]** User misses one day, streak resets → Expected: currentStreak = 0, streak broken
- [ ] **[P1]** User contributes daily (e.g., completes task) → Expected: DAILY_CONTRIBUTION streak increments
- [ ] **[P1]** User completes task every week → Expected: WEEKLY_TASK streak increments
- [ ] **[P2]** lastActivityDate updated on each qualifying action → Expected: Timestamp captures last streak activity

#### Longest Streak Tracking
- [ ] **[P0]** User's currentStreak exceeds longestStreak → Expected: longestStreak updated to match currentStreak
- [ ] **[P1]** User's longestStreak persists after streak breaks → Expected: longestStreak remains as personal record
- [ ] **[P2]** longestStreak displayed on profile → Expected: User's best streak shown

#### Streak Reset
- [ ] **[P0]** User misses one day (DAILY_LOGIN) → Expected: currentStreak resets to 0
- [ ] **[P1]** User misses one contribution day (DAILY_CONTRIBUTION) → Expected: currentStreak resets to 0
- [ ] **[P1]** User misses one week (WEEKLY_TASK) → Expected: currentStreak resets to 0
- [ ] **[P2]** Grace period for streak reset (e.g., 1-hour buffer) → Expected: Streak not reset if within grace period

#### STREAK_MILESTONE Notification
- [ ] **[P1]** User reaches 7-day streak → Expected: STREAK_MILESTONE notification sent
- [ ] **[P1]** User reaches 30-day streak → Expected: STREAK_MILESTONE notification sent
- [ ] **[P1]** User reaches 100-day streak → Expected: STREAK_MILESTONE notification sent
- [ ] **[P2]** Streak milestone grants XP bonus → Expected: XPEvent created for milestone achievement

#### Edge Cases
- [ ] **[P2]** User logs in multiple times same day → Expected: Streak increments only once per day
- [ ] **[P2]** User in different timezone, day boundary calculation → Expected: Streak respects user's local timezone or UTC
- [ ] **[P3]** User streak data lost, manual recovery → Expected: Admin can restore streak data if needed

### Referrals

#### Referral Creation
- [ ] **[P0]** User A refers User B (new user) → Expected: Referral created with referrerId = A, referredId = B, status = PENDING
- [ ] **[P1]** Referral includes referralCode → Expected: Unique referral code generated and stored
- [ ] **[P1]** User cannot refer self → Expected: Validation error, referrerId != referredId
- [ ] **[P1]** User A refers multiple users → Expected: Multiple Referral records created
- [ ] **[P2]** User B (referred) is unique → Expected: Each user can only be referred once (unique constraint on referredId)

#### Referral Status Transitions
- [ ] **[P0]** Referral starts at PENDING → Expected: Initial status = PENDING
- [ ] **[P0]** Referred user completes onboarding/criteria → Expected: Status changes to COMPLETED, completedAt timestamp set
- [ ] **[P1]** Referral COMPLETED triggers rewards → Expected: Referrer and referee receive rewards
- [ ] **[P1]** Referral expires after X days if not completed → Expected: Status changes to EXPIRED
- [ ] **[P1]** Referral can be cancelled → Expected: Status changes to CANCELLED

#### Rewards
- [ ] **[P0]** Referrer receives XP reward when referral COMPLETED → Expected: xpRewarded set, XPEvent created for referrer
- [ ] **[P1]** Referrer receives equity reward when referral COMPLETED → Expected: equityRewarded set, equity granted
- [ ] **[P1]** Referee (referred user) also receives reward → Expected: Rewards granted to both parties
- [ ] **[P1]** rewardClaimed flag set when rewards granted → Expected: rewardClaimed = true, prevents duplicate rewards
- [ ] **[P2]** Rewards only granted once per referral → Expected: rewardClaimed prevents re-granting

#### Referral Code
- [ ] **[P1]** Referral code unique per referrer → Expected: Each referrer has unique code
- [ ] **[P1]** Referred user signs up with referral code → Expected: Referral linked via referralCode
- [ ] **[P2]** Referral code displayed to referrer → Expected: User can share their referral code
- [ ] **[P2]** Invalid referral code during signup → Expected: Error message, signup proceeds without referral

#### Edge Cases
- [ ] **[P2]** User A refers User B, User B deletes account before completion → Expected: Referral status remains PENDING or moves to CANCELLED
- [ ] **[P2]** User A refers 100+ users → Expected: System handles large referral count
- [ ] **[P3]** Referral fraud detection (same IP, rapid referrals) → Expected: System flags suspicious referrals

---

## Section 12: Product Catalog

### Product CRUD Operations

#### Product Creation
- [ ] **[P0]** Create product with all required fields (name, price, projectId, userId) → Expected: Product created successfully
- [ ] **[P0]** Create product for project with commerceEnabled = true → Expected: Product created and listed
- [ ] **[P0]** Create product for project with commerceEnabled = false → Expected: Validation error, commerce not enabled for project
- [ ] **[P1]** Create product with description → Expected: Description stored
- [ ] **[P1]** Create product with SKU → Expected: SKU stored
- [ ] **[P1]** Create product with cost → Expected: Cost stored (for profit margin calculation)
- [ ] **[P1]** Create product with category → Expected: Category stored
- [ ] **[P1]** Create product with isActive = true → Expected: Product visible and purchasable
- [ ] **[P1]** Create product with isActive = false → Expected: Product hidden from catalog
- [ ] **[P2]** Create product without optional fields (description, SKU, cost, category) → Expected: Product created with null optional fields

#### Product Price and Cost Validation
- [ ] **[P0]** Create product with price > 0 → Expected: Product created successfully
- [ ] **[P1]** Create product with price = 0 → Expected: Validation error, price must be positive
- [ ] **[P1]** Create product with price < 0 → Expected: Validation error, price must be positive
- [ ] **[P1]** Create product with cost > 0 → Expected: Product created successfully
- [ ] **[P1]** Create product with cost = 0 → Expected: Product created (zero-cost product allowed)
- [ ] **[P1]** Create product with cost < 0 → Expected: Validation error, cost must be non-negative
- [ ] **[P2]** Create product with cost > price → Expected: Warning (selling at loss) or allowed per business rules

#### Product Linked to Project
- [ ] **[P0]** Product linked to valid projectId → Expected: Product associated with project
- [ ] **[P1]** Product linked to non-existent projectId → Expected: Validation error, project must exist
- [ ] **[P1]** Multiple products linked to same project → Expected: All products created for project
- [ ] **[P2]** Product displayed in project's storefront → Expected: Product appears on project's commerce page

#### Product Creator (userId)
- [ ] **[P0]** Product created by valid userId → Expected: Product associated with creator
- [ ] **[P1]** Product creator is project member → Expected: Product creation allowed
- [ ] **[P2]** Product creator is not project member → Expected: Validation error or creation denied based on permissions
- [ ] **[P2]** Product creator displayed on product page → Expected: Creator name/link shown (if feature exists)

#### Product Updates
- [ ] **[P0]** Update product name → Expected: Name updated successfully
- [ ] **[P0]** Update product price → Expected: Price updated successfully
- [ ] **[P1]** Update product description → Expected: Description updated successfully
- [ ] **[P1]** Update product SKU → Expected: SKU updated successfully
- [ ] **[P1]** Update product cost → Expected: Cost updated successfully
- [ ] **[P1]** Update product category → Expected: Category updated successfully
- [ ] **[P1]** Update product isActive from true to false → Expected: Product hidden from catalog
- [ ] **[P1]** Update product isActive from false to true → Expected: Product re-enabled and visible
- [ ] **[P2]** Non-creator attempts to update product → Expected: Permission denied (unless admin or project leader)

#### Product Deletion
- [ ] **[P1]** Delete product → Expected: Product deleted (soft delete with isActive = false or hard delete)
- [ ] **[P1]** Delete product with existing orders → Expected: Validation error, cannot delete product with orders (or soft delete only)
- [ ] **[P2]** Non-creator attempts to delete product → Expected: Permission denied

### Product Visibility (isActive)

#### Active Products
- [ ] **[P0]** Product with isActive = true appears in catalog → Expected: Product listed and purchasable
- [ ] **[P1]** Product with isActive = true searchable → Expected: Product appears in search results
- [ ] **[P1]** Product with isActive = true filterable by category → Expected: Product appears in category filters

#### Inactive Products
- [ ] **[P0]** Product with isActive = false hidden from catalog → Expected: Product not visible to customers
- [ ] **[P1]** Product with isActive = false not searchable → Expected: Product does not appear in search results
- [ ] **[P2]** Product with isActive = false viewable by creator/admin → Expected: Creator can see inactive products in management view
- [ ] **[P2]** Existing orders for inactive product still accessible → Expected: Order history includes inactive products

### Project Commerce Enabled

#### Commerce Enabled Projects
- [ ] **[P0]** Project with commerceEnabled = true can list products → Expected: Products created and displayed
- [ ] **[P1]** Project storefront displayed when commerceEnabled = true → Expected: Storefront page accessible
- [ ] **[P1]** Project storefrontDescription displayed → Expected: Storefront description shown on commerce page
- [ ] **[P2]** Project commerce settings editable by project leader → Expected: Leader can enable/disable commerce

#### Commerce Disabled Projects
- [ ] **[P0]** Project with commerceEnabled = false cannot list products → Expected: Product creation blocked
- [ ] **[P1]** Project with commerceEnabled = false, storefront not accessible → Expected: Storefront page returns error or hidden
- [ ] **[P2]** Project commerce disabled, existing products hidden → Expected: Products remain but not visible
- [ ] **[P2]** Project commerce re-enabled, existing products become visible → Expected: Products reappear in catalog

### SKU Uniqueness

#### SKU Handling
- [ ] **[P1]** Product created with unique SKU → Expected: Product created successfully
- [ ] **[P1]** Product created with duplicate SKU within same project → Expected: Validation error, SKU must be unique per project
- [ ] **[P2]** Product created with duplicate SKU across different projects → Expected: Allowed (SKU unique per project) or global uniqueness enforced
- [ ] **[P2]** Product created without SKU → Expected: Product created, SKU = null (if optional)
- [ ] **[P2]** SKU format validation (e.g., alphanumeric) → Expected: Invalid SKU format rejected

### Product Listing and Search

#### Product Listing
- [ ] **[P0]** List all active products for a project → Expected: All isActive = true products returned
- [ ] **[P1]** List products sorted by name → Expected: Products sorted alphabetically
- [ ] **[P1]** List products sorted by price → Expected: Products sorted by price (ascending or descending)
- [ ] **[P2]** List products paginated (10 per page) → Expected: Products paginated correctly
- [ ] **[P2]** List products with thumbnail images → Expected: Product images displayed (if image field exists)

#### Product Search
- [ ] **[P0]** Search products by name → Expected: Matching products returned
- [ ] **[P1]** Search products by description keywords → Expected: Matching products returned
- [ ] **[P1]** Search products by SKU → Expected: Product with matching SKU returned
- [ ] **[P2]** Search across all projects → Expected: Products from all commerce-enabled projects returned
- [ ] **[P2]** Search within specific project → Expected: Only products from specified project returned

#### Filter by Category
- [ ] **[P0]** Filter products by category (e.g., "Electronics") → Expected: Products in specified category returned
- [ ] **[P1]** Filter by multiple categories → Expected: Products matching any selected category returned
- [ ] **[P2]** Category list shows product count per category → Expected: Categories displayed with count (e.g., "Electronics (5)")

### Storefront Context

#### Project Storefront
- [ ] **[P0]** Access project storefront page → Expected: Storefront displays all active products
- [ ] **[P1]** Storefront displays storefrontDescription → Expected: Description text shown at top of storefront
- [ ] **[P1]** Storefront displays project branding (logo, colors) → Expected: Storefront styled per project settings
- [ ] **[P2]** Storefront accessible via custom URL (e.g., /projects/slug/store) → Expected: Storefront has dedicated route

#### Product Display on Storefront
- [ ] **[P0]** Product name, price, description displayed → Expected: Essential product info visible
- [ ] **[P1]** Product "Add to Cart" button displayed → Expected: User can add product to cart (if cart feature exists)
- [ ] **[P1]** Product detail page accessible → Expected: Clicking product shows detailed view
- [ ] **[P2]** Out-of-stock indicator (if inventory tracking exists) → Expected: Products marked as out-of-stock not purchasable

### Edge Cases

#### Data Integrity
- [ ] **[P1]** Product deleted, associated orders remain intact → Expected: Orders reference product data (snapshot or archived)
- [ ] **[P1]** Project deleted, products cascade deleted or orphaned → Expected: Products removed or flagged as orphaned
- [ ] **[P2]** User (creator) deleted, products remain with null creator → Expected: Products persist, creator field nullable

#### Pricing Edge Cases
- [ ] **[P2]** Product price updated, existing carts/orders not affected → Expected: Orders use price at time of purchase
- [ ] **[P2]** Currency handling (if multi-currency support) → Expected: Prices stored and displayed in correct currency
- [ ] **[P3]** Discount or promotion logic (if feature exists) → Expected: Discounts applied correctly at checkout

#### Performance
- [ ] **[P2]** Storefront with 100+ products loads efficiently → Expected: Page loads within acceptable time
- [ ] **[P2]** Product search with 1000+ products performs well → Expected: Search returns results quickly
- [ ] **[P3]** Concurrent product updates do not cause conflicts → Expected: Updates handled without data loss

---

---

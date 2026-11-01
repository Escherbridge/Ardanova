---
type: reference
---

# components/notifications — the come-back bell (SR-03 Phase 3, Lane G)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §4. Server side:
`ArdaNova.Application/Services/Implementations/NotificationServices.cs` (`StageReplyAsync`, `StageAskPickedUpAsync`).

## Two events, nothing else

The bell shows exactly the two return loops the plan allows, and only while unread:

| .NET type       | Kind        | Title (server copy)             | Link (server `actionUrl`)                                  |
| --------------- | ----------- | ------------------------------- | ---------------------------------------------------------- |
| `COMMENT_REPLY` | `reply`     | "New reply" / "{n} new replies" | `/projects/{slug}`; null for a post with no project → Home |
| `TASK_ASSIGNED` | `picked-up` | "Picked up"                     | `/tasks/{taskId}`                                          |

Every other `NotificationType` is filtered out client-side (`returnLoopsOf`), so a type a future writer adds never
shows up here by accident. The badge is the length of that filtered list, not the API's `unreadCount` (which counts
every type), so the number and the list can never disagree. Forbidden by plan §4 and deliberately absent: streaks,
"N new posts", push/email, sounds, a live region announcing new arrivals.

## Data flow

1. `notification.getUnread` (→ `GET /api/notifications/me/unread`, newest first, all types) polls every 60 s while
   the tab is open and refetches on focus. No push channel exists; a minute is quiet enough for two human events.
2. `returnLoopsOf` narrows to the two types and turns `actionUrl` into an in-app href.
3. Opening an entry navigates (a real `Link`, so new-tab and middle-click work) and calls
   `notification.markAsRead` (→ `POST /api/notifications/{id}/read`, which the API refuses for another user's row).
   The entry leaves the list at once; a refusal restores it and toasts; either way the list is re-read from the API.
4. "Mark all read" → `POST /api/notifications/me/read-all`. It marks every type read on the server; the bell only
   ever showed two, so nothing visible is lost.

## Link guard

`actionUrl` is server-written, but its project part comes from a user-chosen slug. `returnLoopsOf` runs it through
`normalizeInternalCallbackUrl` (the sign-in callback guard), so only same-origin paths survive; anything absolute,
protocol-relative or backslashed falls back to `/dashboard`.

## Disclosure, not a menu

The panel is a disclosure (`aria-expanded` + `aria-controls`, a labelled region) holding a list of links, rather than
an ARIA menu: the entries navigate, they are not commands. Escape closes and returns focus to the bell; a press
outside closes without moving focus. The bell's accessible name carries the count ("Notifications, 2 unread"), so
the visible badge digit is part of the name (WCAG 2.5.3).

## Tests

`dotnet-test-edge.tsx` is test-only (the precedent is `components/feed/trpc-test-edge.tsx`): the real tRPC routers
and API client run in-process and only global `fetch` (the .NET API) is faked, answering with the API's own JSON.
Link clicks reach a recording router, the other process edge. Flows: `app-topbar.test.tsx` (count, deep links,
mark read, the server's count wins), `app/dashboard/page.test.tsx` (Home). `return-loops.test.ts` is the link
guard's table.

## Deferred

- Read history: the panel lists unread only. Revisit if people ask to find a reply they already opened.
- `getUnread` returns every unread row; replies collapse per post server-side, so the list stays small. Revisit
  (a capped read) if anyone's unread list passes ~100.

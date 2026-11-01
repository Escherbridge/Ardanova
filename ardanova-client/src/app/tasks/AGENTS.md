---
type: reference
---

# app/tasks: the task page and its one action (SR-03 Phase 3, Lane W)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §2 (HELP_WANTED routing, paid-task
readiness), §6 row W, Revisions 1-2. Copy follows `documentation/LINGUISTIC_GUIDE.md` §Just enough; money, account and
verification wording is reused verbatim, never rewritten here. `[id]/commerce/**` is a money page and out of scope.

## Pick up task (`[id]/page.tsx` → `PickUpTask`)

First matching row wins. Facts come from `task.getById` (TaskDto), membership from `project.getById` +
`project.getMembers` (creator or listed member, the same rule as `HierarchyAuthorizationService.IsProjectMemberAsync`).

| Facts                                                                        | Shows                                                                                  |
| ---------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| the viewer holds the task (or just claimed it)                               | disabled **Picked up**                                                                 |
| the last claim was refused with a known code                                 | `TASK_TAKEN` Already taken · `TASK_DONE` Done · `TASK_UNDER_AGREEMENT` Under agreement |
| signed out, not a member, assigned, COMPLETED, escrow ≠ NONE, `hasAgreement` | nothing (the existing "Being done by" line covers it)                                  |
| member + open + unpaid                                                       | **Pick up task** → `task.claim`                                                        |
| member + open + paid (`equityReward > 0`), account ready                     | **Pick up task**                                                                       |
| member + open + paid, setup left to do                                       | **Finish setup** → `/settings/verification?returnTo=/tasks/{id}`                       |
| member + open + paid, under review / unreadable                              | the account evaluation's own message, no action                                        |

- **One readiness read.** `azoaCustodialAccount.getStatus` is enabled only on the paid + member + open row. It mirrors
  `TasksController.Assign` (the API re-checks readiness on the claim; the read only decides which button to show).
  Classification is `evaluateValueActionAccount` (three-way: an account under review is never told to "finish setup").
- **The claim rule lives server-side** (K1's `TaskClaimRule`). The client pre-check only hides a button the API would
  certainly refuse; the 409's `taskClaimCode` (`taskClaimRefusalOf`) is the truth. An uncoded failure (a newer code, a
  readiness 403/409, a non-member's FORBIDDEN) falls back to `MutationErrorNotice`, which carries the API's message.
- `hasAgreement` (an accepted bid's agreement decides the worker) is on the client `Task` type (final join f).
- Every claim settles with a `task.getById` refetch, so "Being done by" shows the real holder after a win or a 409.

## The ask (`[id]/page.tsx` → `HelpAsk`)

`TaskDto.helpWanted { postId, askerId, askerName }` is set only for a public HELP_WANTED. The page reads the post once
(`post.getById`) to show the asker's own words, and offers **Message {askerName}** → `/chats?with={askerId}` to any
signed-in viewer who is not the asker, while the task is not done (same rule as `messageActionFor` on the feed card).
The link opens a draft; nothing is created until the first send (see `../chats/AGENTS.md`).

## Ask for help (`src/components/projects/work-item-detail-modal.tsx` → `AskForHelp`)

The board's work-item modal is the task's other writer into the feed: **Ask for help** swaps in `FieldNoteComposer` with
`projectId` + `taskId` + `author` (help mode by default), so the composer stays the one place an ask is written.

| Facts (task-level selection only)                   | Shows                                              |
| --------------------------------------------------- | -------------------------------------------------- |
| assigned or COMPLETED                               | nothing (the composer's own askable rule)          |
| a public ask by someone else (`helpWanted.askerId`) | "Asked by {name}"                                  |
| signed out or not a member                          | nothing (the API refuses a non-member's ask)       |
| member                                              | **Ask for help** → composer → "Asked." + Open task |

The viewer's own existing ask still shows **Ask for help**: the composer's 409 turns it into "Edit your ask".

**Escape.** Radix's dialog hears Escape on `document` in the capture phase, before the composer's textarea. The modal's
`onEscapeKeyDown` prevents the dismissal while the focused field is driving an open suggestion list (it has
`aria-autocomplete="list"` and a non-empty `aria-activedescendant`, which `HashtagTextarea` sets only while its `#`
list is open). The textarea's own handler then closes the list; the next Escape closes the dialog. This reads ARIA
state the composer already publishes, so no prop or ref crosses the lane boundary.

## Deferred

- **The modal reads membership itself** (`project.getById` + `getMembers`, shared cache keys with the project page's
  `getMembers`). Trigger: a second consumer → lift a `viewerIsMember` prop from the board.

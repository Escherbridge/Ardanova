---
type: spec
status: proposed
priority: P1
source: loop3 projects QA + adversarial critique (2026-10-18)
---
# Projects hiring follow-ups (loop 3)

## Overview

Loop 3 fixed these:

- Hiring and invitations follow the rank rule. Nobody can post, re-role, accept or invite a role at or above their own rank, and nobody can offer FOUNDER.
- Epic, sprint, feature and PBI reads return 404 when the project is hidden.
- Positions posted from Team Positions now appear in that tab, and the tab has a "Create Position" action.
- Co-managers get the hiring panel and the edit page.
- Founders and managers can remove members, and members can leave.
- The board's list view has an assignee picker.
- Accept and Reject each ask for confirmation.
- A task title cannot be blank.

The items below were too large for loop 3, outside its lane, or need a schema change.

## Requirements

1. **Applicant-side application tracking.** An applicant has no screen that lists their opportunity applications or their status, and no way to withdraw. Needs `GET /api/opportunities/applications/mine` (actor-scoped) plus an applicant-only withdraw route, which already exists in the service as a PENDING-only transition, and a "My applications" view.
2. **`opportunity-form.tsx` copy and choices** (root `src/components`, owned by the design/components lane):
   - Remove the `FOUNDER` team-role option, which the API now refuses with 403.
   - Hide the `GUILD_POSITION` type when `entityType=project`. The API now stores it as `PROJECT_ROLE`.
   - Drop "(optional)" from Team Role for project team positions, or say that the default is Contributor.
3. **Deadlines and fill state** (schema change: headcount):
   - Refuse applications after `deadline`.
   - Move a role to FILLED once its headcount is seated.
   - Keep filled roles out of the public list. Today a filled role stays OPEN.
4. **Notifications.** Applicants hear nothing when they are accepted or rejected, and managers hear nothing when someone applies. This needs the notification lane's event bus.
5. **Rank-aware member role picker for co-managers.** The Team tab's role select and the remove control still depend on `isOwner`, which means the creator. A LEADER should get the same controls for members ranked below them. `member-authority.ts` already holds the rule. The tab needs the viewer's role passed in as a prop instead of `isOwner`.
6. **/projects stuck loading after two zero-result searches (mobile).** This was inconclusive and could not be reproduced in a fresh context. Run a manual check with a long-lived profile before investing more time.
7. **A project with any task cannot be deleted.** `DELETE /api/projects/{id}` throws an unhandled FK violation once the project has even one `ProjectTask` row. `ProjectTask_projectId_fkey` has neither `Cascade` nor a friendly `Restrict` message configured, so `ProjectService.DeleteAsync` (`OpportunityServices.cs` sibling `ProjectServices.cs:589-598`, no pre-checks) lets EF Core's delete hit Postgres directly; the resulting `23503` is caught by `ExceptionHandlingMiddleware` and surfaces as a generic `400 "The request contained an invalid value."` — the right status family, a useless message, and the project is never deleted. Reproduced live, twice, on two independent projects (each with exactly one task). **Not currently reachable from the UI** — the client has no Delete Project button or route (the only `project.delete` caller is the create-page's rollback-on-failure path) — so this is latent, not a live incident, and does not block release on its own. Fix is either (a) cascade-delete `ProjectTask` (and audit the other project-owned tables for the same gap — milestones/epics/etc. were not probed this loop) when a project is deleted, or (b) have `ProjectService.DeleteAsync` check for existing tasks/members/opportunities first and refuse with a clear, typed `ValidationError` instead of an opaque 400 — preferred, since silently cascading away a team's task history on project delete is its own hazard. Whoever builds a "Delete Project" UI should not ship it until one of these lands.

## Loop 4 QA re-verification (final loop, 2026-10-18)

Live re-tested against the running local stack (main @ 1b58c40), via signed-actor API calls and a
real Playwright pass — not read off the diff. See `.omc/qa/loop4/projects/` for raw evidence.

- Requirement 5 (rank-aware Team tab controls) — **still open**. `team-tab.tsx` still gates every
  member control on `isOwner` alone (the `isOwner &&` checks that drive the role Select, Remove
  button, and credential controls); a LEADER co-manager still sees none of them, even though the
  API already allows a correctly-ranked LEADER to act (reconfirmed this loop via a direct
  `PUT /api/projects/{id}/members/{id}` as a seated LEADER).
- Requirement 6 (/projects mobile stuck-loading) — **not reproduced, a second time**, in a
  brand-new (non-persistent) mobile context: two consecutive zero-result searches on `/projects`
  at 390px recovered correctly, showed the empty-state copy, and the list recovered after clearing
  the search. Two independent non-reproductions across two loops now; recommend closing or
  demoting to "monitor only" rather than spending more QA time on it.
- "Positions posted from Team Positions now appear in that tab" (the loop-3 fixed-items list above)
  — reconfirmed at both the API level (`GET /api/opportunities/project/{id}` includes the new
  position with `origin=TEAM_POSITION`, which the tab's client-side filter requires) and through a
  live UI pass (Create Position -> Team Positions tab shows it immediately, no reload needed).
- The separate carryover item "Hiring bypasses rank-bound role authority: a CORE_CONTRIBUTOR can
  seat a LEADER who then outranks and demotes them" (flagged `possibly-unfixed CRITICAL` going into
  this loop) is **fixed and verified live**: `OpportunitiesController.UpdateApplicationStatus` calls
  `RoleOfferRefusedAsync` before accepting, so accepting an application now obeys the same
  "below your own rank" rule as posting/re-roling one. Verified: a CORE_CONTRIBUTOR's accept attempt
  on a pending LEADER application gets a 403 `OutranksActor`, the application stays PENDING, and the
  founder (who outranks LEADER) can accept the same application and seat the LEADER normally.
- The separate carryover item "Hidden (DRAFT) project backlog is readable anonymously" is **fixed
  and verified live** across all five endpoints: anonymous `GET` on a DRAFT project's Epic, Sprint,
  Feature, PBI and Task each return 404 (same shape as a truly-missing id), the project's own
  founder gets 200 on the same rows, and the project-scoped backlog list route 404s too rather than
  returning an empty 200.

## Acceptance

- Each requirement has a flow test at the highest public seam: a .NET controller/service flow test for API behaviour, and vitest for pure client logic.
- No requirement weakens `ProjectMemberRolePolicy`. Every authority decision still goes through it.

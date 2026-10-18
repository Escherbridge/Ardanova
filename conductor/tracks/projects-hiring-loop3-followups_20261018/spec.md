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

## Acceptance

- Each requirement has a flow test at the highest public seam: a .NET controller/service flow test for API behaviour, and vitest for pure client logic.
- No requirement weakens `ProjectMemberRolePolicy`. Every authority decision still goes through it.

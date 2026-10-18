---
type: spec
status: proposed
priority: P1
source: loop2 projects QA + adversarial critique (2026-10-18)
---
# Projects hiring follow-ups (loop 2)

## Overview

Loop 2 shipped these:

- Bid reads are limited to the bidder and the role's managers, and no bid carries an email.
- Role hiring runs through opportunity applications: apply, accept, and the applicant is seated as a member.
- The owner can open, pause or close a role, and new roles start OPEN.
- Member roles follow rank rules: nobody promotes themself, nobody grants FOUNDER, only the founder deletes the project, and nobody is seated without consent.
- Removing a member frees their unfinished tasks.
- Every task mutation checks visibility first and returns 404 for a hidden task.

The items below were too large for loop 2 or outside its lane.

## Remaining requirements

1. **Seat the member when a paid bid is accepted (commerce lane, finance on hold).** `TaskCommerceService.AcceptBidAsync` assigns the task directly and writes no `ProjectMember`, so the paid hire still cannot be given other tasks. When commerce resumes, seat the bidder in the same save, using the rule in `OpportunityService.SeatAcceptedApplicantAsync`.
2. **Make token-config setup findable (finance lane).** A project with no token config can never accept a paid bid, and no UI creates one. Add a setup entry point on the Funding tab, or explain the gate on the bids panel.
3. **Give co-managers the owner view.** `app/opportunities/[slug]/page.tsx` sets `isOwner` from `posterId` only. A LEADER or CORE_CONTRIBUTOR co-manager therefore sees the applicant view, even though the API lets them manage the role. Fix: read a manage flag from the API, for example a `viewerCanManage` field on `OpportunityDto`.
4. **Add an accept-applications toggle to the form.** `components/opportunity-form.tsx` has no toggle; it was outside the loop-2 lane. The router now defaults new roles to OPEN. Add an explicit "Accept applications" switch, plus a DRAFT badge the owner can see.
5. **Add task editing.** No UI calls `task.update`, and the task detail page only shows escrow. Add editing for title, description, priority and estimate on the task detail page. Non-financial fields only.
6. **Remove the remaining BFF permission checks.** The `project.ts` and `task.ts` routers still check permissions in Next.js, with narrower rules than the API. Make them thin proxies, as was done for the opportunity and bid routers.
7. **Share the board's error card.** Move the board's "BOARD COULD NOT BE LOADED" card into a shared component and use it on /projects, /tasks, /dashboard and /opportunities.

## Acceptance

- Each requirement has a flow test through the public service or controller seam, or a vitest for pure client logic.
- The QA hiring flow passes end to end: post a role, apply as a second actor, accept as the owner, see the new member on the team, and assign them a different task. Repeat it with a paid bid once commerce resumes.

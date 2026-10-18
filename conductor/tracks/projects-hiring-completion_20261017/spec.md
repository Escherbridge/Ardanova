---
type: spec
---

# Projects hiring completion: specification

## Overview

Loop 1 of the projects, task board and hiring work (branch `loop1/projects`) closed the core gaps:

- Draft work no longer leaks through task and opportunity reads.
- Accepting an application seats the applicant as a `ProjectMember`.
- Tasks can be assigned, claimed and released through `PUT /api/tasks/{id}/assign`.
- Both kinds of application are one-step state machines.

This track holds the remaining work. Each item was either too large for that loop or belongs to another lane.

## Remaining requirements

1. **Role headcount.** An opportunity has no headcount, so accepting someone never fills or closes the role. Add a `headcount`/`filledCount` pair: change the DBML first, then write an additive migration. Increment `filledCount` on acceptance in the same save, and move the role to FILLED when it reaches headcount.
2. **Guild-role acceptance.** Accepting an application to a guild-linked opportunity creates no `GuildMember`. Mirror the project seating through `IGuildMemberService`.
3. **Applicant notification.** No notification fires when an application is accepted or rejected. Emit one through the notification service, inside the same unit of work or through the outbox.
4. **Database-enforced single pending application.** Add a partial unique index on `ProjectApplication(projectId, userId) WHERE status = 'PENDING'` in the DBML, with a hand-written migration. Only the service enforces this today, so two concurrent submits can both pass.
5. **Dev-bypass tRPC context (auth lane).** `createTRPCContext` (`src/server/api/trpc.ts`) calls `auth()`, while pages use `authForPage()`. As a result, every protected mutation returns 401 under `DEV_AUTH_BYPASS`.
   - Patch only the context resolver, and add a regression test that it resolves the same bypass session.
   - Do not loosen `protectedProcedure`.
   - Writes will still fail until the preview user exists as an API user.
6. **Return 403 instead of 400 for refusals (platform lane).** `Forbid()` without a `DefaultForbidScheme` returns 400 with ASP.NET's internal error text. Register a forbid scheme on the actor-assertion handler in `ApiKeyMiddleware.cs`. Controller tests that assert `ForbidResult` stay valid.
7. **More assignee pickers.** Loop 1 added assignment to the board card menu and to `/tasks/create` only. The task detail page and the board's list view still need a picker.

## Acceptance

- Each requirement has a flow test through the public service or controller seam.
- The QA harness flow `.omc/qa-harness/loop1/projects/04-hiring-membership.mjs` passes end to end: apply, accept, see the new member, assign them a task.

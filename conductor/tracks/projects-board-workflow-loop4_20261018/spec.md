---
type: spec
status: proposed
priority: P1
source: loop4 projects QA + adversarial critique (2026-10-18)
---
# Task board workflow and hiring cleanup (loop 4)

## Overview

Loop 4 fixed these:

- Member emails are shown only to managers.
- `GET /projects/{A}/members/{id}` no longer returns a member of another project.
- Writes to a hidden project, epic, sprint or feature now return the same 404 as a missing one.
- Accepting an existing member for a higher role promotes them. A hire the applicant already outranks is refused instead of being marked ACCEPTED.
- Deleting a project that has tasks or milestones returns 409 with a clear message instead of an opaque foreign-key 400.
- Reopening a task clears `completedAt`.
- Task titles are capped at 200 characters, and negative hours are refused.

The items below need a product decision or a schema change, or belong to another lane. Each one was reproduced live in the loop-4 critique (`.omc/qa-harness/loop4/projects-critique/`).

## Requirements

1. **Task status workflow.** `TaskService.UpdateStatusAsync` currently accepts any status. Define the allowed transitions in one domain policy, the same way `ProjectLifecycle` works for projects. The minimum is TODO → IN_PROGRESS → IN_REVIEW → COMPLETED, plus reopen. An assignee must not be able to jump from TODO straight to COMPLETED, and the move to COMPLETED belongs to a manager or reviewer. Route both `PATCH /tasks/{id}/status` and the `Status` field of `PUT /tasks/{id}` through this policy. The board's drag-and-drop must show the server's refusal.
2. **Assignee edit scope.** An assignee can currently rewrite a task's title, description and due date through `PUT /tasks/{id}`, the same way a manager can. Split the update in `TasksController.Update`: the assignee may change progress fields (status via the workflow, actual hours), and scope fields (title, description, due date, estimate, priority, type) are for managers only. Use the same narrow-diff pattern as `OpportunitiesController` re-role: gate only the fields that changed.
3. **Due-date sanity.** A due date in 1970 is accepted. Refuse due dates that are before the project's creation date on create and update. Existing rows are not touched.
4. **Positions as positions.** These depend on req 3 of `projects-hiring-loop3-followups_20261018`:
   - Accepting someone should fill the position. Today it stays OPEN with no headcount.
   - Deadlines are never enforced.
   - Applicants cannot withdraw an opportunity application. The service has the PENDING-only transition, but no applicant route exists.
5. **One hiring system.** There are two separate hiring paths with different rules: project applications (`ProjectApplicationService`, free-text role title, always CONTRIBUTOR) and position applications (`OpportunityService`, a manager-chosen rank). Decide whether project applications become "apply to the CONTRIBUTOR position" or stay a separate path. Either way, both paths must share one seating function so the rank rules and the promote-or-refuse rule in §Hiring an existing member cannot drift apart.
6. **Numeric enum values cause a 500** (platform lane, `Program.cs` JSON options). Sending `"status": 7` or another out-of-range number reaches the service as an undefined enum value. Reject undefined enum values at model binding, for example with a `JsonStringEnumConverter` that disallows integers, and return a 400.
7. **Length limits beyond task titles.** Descriptions, cover letters and opportunity titles have no length limit. Add caps in the services to match the UI, along with the same zod `max` in the routers.
8. **Project delete beyond planned work.** The 409 guard covers tasks and milestones. Other children (members, applications, comments, updates, support) still hit the foreign-key restriction, which shows up as the opaque 400. Choose between a broader "delete only an empty project" check and a soft delete (CANCELLED plus hidden), then add a "Delete project" control only for an empty DRAFT.

## Acceptance

- Each requirement has a flow test at the highest public seam: a .NET controller or service flow test using list-backed repositories, as in `ProjectHiringFlowTests`.
- No requirement weakens `ProjectMemberRolePolicy` or the 404-before-403 visibility rule (`ProjectVisibilityExtensions.IsVisibleAsync`, `HiddenOrRefusedAsync`).

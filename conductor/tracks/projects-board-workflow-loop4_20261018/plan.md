---
type: plan
---
# Track: Task board workflow and hiring cleanup (loop 4)

> Spec: [spec.md](spec.md).

## Phase 1: Board workflow [P1]
- [ ] Add a `TaskWorkflow` domain policy for allowed transitions and who may make each one, and route both status write paths through it (req 1)
- [ ] Split `TasksController.Update` into progress fields for the assignee and scope fields for managers, gating only the fields that changed (req 2)
- [ ] Refuse due dates before the project's creation date (req 3)
- [ ] Show the server's refusal on the board when a drag-and-drop move is refused (req 1)

## Phase 2: Hiring [P1, depends on projects-hiring-loop3-followups req 3]
- [ ] Fill a position on accept once its headcount is seated, and enforce deadlines (req 4)
- [ ] Add an applicant withdraw route for opportunity applications (req 4)
- [ ] Decide on one hiring path and extract a shared seating function used by both services (req 5)

## Phase 3: Input hygiene [P2]
- [ ] Refuse integer and undefined enum values at model binding with a 400 (req 6, platform lane)
- [ ] Add length caps to descriptions, cover letters and opportunity titles, mirrored in zod (req 7)

## Phase 4: Delete [P2]
- [ ] Choose between an empty-only delete and a soft delete, then expose "Delete project" only for an empty DRAFT (req 8)

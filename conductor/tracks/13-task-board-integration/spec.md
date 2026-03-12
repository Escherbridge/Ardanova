# Track 13 — Task Board Integration — Technical Specification

## Overview

The tasks page at `/tasks` has a polished Kanban board UI with board and list views, project filtering, sprint progress stats, and priority/tag badges. All of it is powered by 8 hardcoded `sampleTasks` objects. No API calls are made. The backend is fully implemented and ready to serve real data.

This track wires the frontend to the backend across four concerns:

1. **Task listing** — Replace hardcoded data with live queries via `api.task`
2. **Task CRUD** — Create, update, and delete tasks through tRPC procedures
3. **Kanban status moves** — Drag/drop or dropdown column changes call `api.task.updateStatus`
4. **Task completion → equity** — Marking a task `done` triggers token allocation distribution and XP award

---

## What Is Broken

### Hardcoded Data (`/tasks/page.tsx`)

```ts
// sampleTasks array: 8 static objects, no API involvement
const sampleTasks = [ ... ];

// All filtering, stats, and board grouping derived from sampleTasks
const tasksByStatus = taskStatuses.reduce(...sampleTasks.filter(...));
```

No `api.task.*` calls exist anywhere in the tasks page. The "New Task" button has no handler. The task card dropdown ("Edit task", "Move to...", "Delete") is wired to nothing.

### Status Mismatch

The UI defines 4 statuses: `todo`, `in_progress`, `review`, `done`.
The backend defines 6: `backlog`, `todo`, `in_progress`, `in_review`, `done`, `cancelled`.

The frontend must map `review` → `in_review` and add `backlog` and `cancelled` columns, or suppress them behind a toggle.

---

## What Already Exists

### tRPC Router (`src/server/api/routers/task.ts`)

All procedures are implemented as thin `apiClient` proxies:

| Procedure | Type | Backend call |
|-----------|------|-------------|
| `task.getAll` | query | `apiClient.tasks.search(...)` — paginated, filterable |
| `task.getMyTasks` | protectedQuery | `apiClient.tasks.getByUserId(userId)` |
| `task.getById` | query | `apiClient.tasks.getById(id)` |
| `task.create` | mutation | `apiClient.tasks.create(...)` + auto-generates draft Opportunity |
| `task.update` | mutation | `apiClient.tasks.update(id, ...)` |
| `task.updateStatus` | mutation | `apiClient.tasks.updateStatus(id, status)` |
| `task.delete` | mutation | `apiClient.tasks.delete(id)` |

### API Client (`src/lib/api/ardanova/endpoints/tasks.ts`)

`TasksEndpoint` has: `getById`, `getAll`, `getPaged`, `search`, `getByUserId`, `getByProjectId`, `create`, `update`, `updateStatus`, `delete`.

The `Task` type includes `equityReward` (not surfaced in the current UI — shown as `reward` tokens in sample data).

### Backend (.NET)

- **`TasksController`** — full CRUD + `PATCH /{id}/status` + `GET /project/{projectId}` + `GET /user/{userId}` + `GET /search`
- **`TaskService`** — handles `SearchAsync`, `CreateAsync`, `UpdateAsync`, `UpdateStatusAsync`, `DeleteAsync` with project/user enrichment
- **`ProjectTokenService`** — `AllocateToTaskAsync`, `DistributeAsync` — allocates and distributes equity tokens to a task's assignee
- **`TokenBalanceService`** — `CreditAsync` — credits the token balance for the recipient user

---

## Task → Equity Pipeline

When a task transitions to `done` (status = `COMPLETED` on the backend), equity should be distributed to the assignee:

```
User marks task done
  → api.task.updateStatus({ id, status: "done" })
    → TaskService.UpdateStatusAsync sets completedAt
  → api.projectTokens.getAllocationsByTask(taskId)
    → find allocation with status = RESERVED
  → api.projectTokens.distribute(allocationId, assigneeId)
    → ProjectTokenService.DistributeAsync
      → TokenAllocation.status = DISTRIBUTED
      → TokenBalanceService.CreditAsync(assigneeId, configId, tokenAmount, CONTRIBUTOR)
  → [optional] XP award via XpEvents endpoint
```

The key prerequisite is that a `TokenAllocation` for the task must already exist (status `RESERVED`). This allocation is created when a project lead assigns equity to a task at creation time via `ProjectTokensEndpoint.allocateToTask(configId, { taskId, equityPercentage })`.

### Equity Display on Task Cards

The `TaskAllocationDto` returned by `getAllocationsByTask` contains:
- `equityPercentage` — percentage of project equity
- `tokenAmount` — raw token count
- `status` — `RESERVED` | `DISTRIBUTED` | `REVOKED` | `BURNED`

Task cards should surface this as `{equityPercentage}% equity ({tokenAmount} tokens)` instead of the hardcoded `reward` field.

---

## Architecture Notes

### Data Fetching

Use `api.task.getMyTasks` as the primary query for the tasks page (current user's assigned tasks). Use `api.task.getAll` with `projectId` filter when a project is selected.

For equity display, fetch task allocations lazily per-card or batch via `getAllocationsByTask` in a secondary query. Avoid blocking the board render on allocation fetches.

### Optimistic Updates for Status Moves

Status changes should be optimistic: immediately update the local query cache, then call `updateStatus`. On error, roll back and show a toast.

### Create Task Modal

The "New Task" button should open a modal (not a new page). Required fields: title, description, project, type, priority. Optional: due date, effort size, assignee.

Note: `task.create` auto-generates a draft Opportunity when a task is created — this is invisible to the user and requires no UI.

### Project-Scoped Filtering

The project filter dropdown must be populated from live project data, not from task results. Use `api.projects.getAll` or equivalent to populate the select. The current implementation derives unique project IDs from `sampleTasks`, which won't work with paginated live data.

---

## Key Entities

| Entity | Location | Key Fields |
|--------|----------|------------|
| `Task` (frontend type) | `endpoints/tasks.ts` | id, projectId, title, status, priority, taskType, assignedToId, equityReward, dueDate |
| `ProjectTask` (backend entity) | `ArdaNova.Domain/Models/Entities/` | id, projectId, status (TaskStatus enum), equityReward, completedAt, escrowStatus |
| `TokenAllocation` | `ArdaNova.Domain/Models/Entities/` | taskId, equityPercentage, tokenAmount, status (AllocationStatus), holderClass |
| `TokenBalance` | `ArdaNova.Domain/Models/Entities/` | userId, projectTokenConfigId, holderClass, totalBalance, availableBalance |

## Backend Status Enum

```
TaskStatus: BACKLOG | TODO | IN_PROGRESS | IN_REVIEW | COMPLETED | CANCELLED
```

Frontend tRPC schema uses lowercase: `backlog | todo | in_progress | in_review | done | cancelled`.

Note: backend uses `COMPLETED`, tRPC router maps this from `done`.

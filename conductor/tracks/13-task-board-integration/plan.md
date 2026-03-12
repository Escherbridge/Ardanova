# Track 13 — Task Board Integration

## 1. Wire Task Listing

- [ ] **[P0] Replace sampleTasks with api.task queries**
    - `ardanova-client/src/app/tasks/page.tsx`
    - Convert page to use `api.task.getMyTasks` for the default "My Tasks" view
    - Add `api.task.getAll` query with `projectId` filter when a specific project is selected
    - Remove `sampleTasks` array and all derived constants that reference it
    - Map backend `Task` type to the shape expected by `<TaskCard>` and list view

- [ ] **[P0] Fix backend status enum mismatch**
    - `ardanova-client/src/app/tasks/page.tsx`
    - Update `taskStatuses` config: rename `review` → `in_review`, add `backlog` and `cancelled` columns
    - Default board view shows: `backlog`, `todo`, `in_progress`, `in_review`, `done`
    - `cancelled` visible via a "Show cancelled" toggle or collapsed column

- [ ] **[P0] Populate project filter from live data**
    - `ardanova-client/src/app/tasks/page.tsx`
    - Replace project list derived from `sampleTasks` with `api.projects.getAll` or `api.projects.getMyProjects` query
    - Project filter select renders live project names and IDs

- [ ] **[P0] Add loading and empty states**
    - Show skeleton cards while `getMyTasks` is loading
    - Show empty-column state per column when no tasks exist for that status
    - Show error toast if query fails

## 2. Wire Task Creation

- [ ] **[P0] Create NewTaskModal component**
    - `ardanova-client/src/components/tasks/new-task-modal.tsx`
    - Form fields: title (required), description (required, min 10 chars), project (required), type (required), priority (default: medium), due date, effort (xs/s/m/l/xl), assignee
    - Calls `api.task.create` mutation on submit
    - On success: close modal, invalidate `getMyTasks` query, show success toast
    - On error: show error message inline

- [ ] **[P0] Wire "New Task" button to modal**
    - `ardanova-client/src/app/tasks/page.tsx`
    - "New Task" button opens `<NewTaskModal>`
    - Column-level "+" button opens modal with that column's status pre-selected

- [ ] **[P0] Wire task card "Edit task" dropdown item**
    - `ardanova-client/src/components/tasks/task-card.tsx` (extract from page)
    - "Edit task" opens modal in edit mode pre-filled with task data
    - Calls `api.task.update` mutation
    - On success: invalidate query, close modal

- [ ] **[P0] Wire task card "Delete" dropdown item**
    - Add confirmation dialog before calling `api.task.delete`
    - On success: invalidate query, show toast

## 3. Wire Task Status Updates (Kanban Moves)

- [ ] **[P0] Add status move action to task card dropdown**
    - `ardanova-client/src/components/tasks/task-card.tsx`
    - "Move to..." submenu with all status options (excluding current status)
    - Calls `api.task.updateStatus({ id, status })` on select
    - Optimistic update: immediately move card to new column in local state
    - On error: roll back optimistic update, show error toast

- [ ] **[P1] Add drag-and-drop column moves**
    - Use `@hello-pangea/dnd` or native HTML5 drag events
    - Dragging a card and dropping on a column header or column body calls `updateStatus`
    - Visual feedback: highlight target column while dragging, opacity on dragged card
    - Optimistic: card moves immediately, reverts on API error

## 4. Wire Task Assignment

- [ ] **[P0] Add assignee picker to task create/edit modal**
    - `ardanova-client/src/components/tasks/new-task-modal.tsx`
    - Fetch project members for the selected project via `api.projects.getMembers` or `api.users.search`
    - Render avatar + name in a searchable select
    - Maps to `assigneeId` in `createTaskSchema` / `updateTaskSchema`

- [ ] **[P0] Wire unassigned slot click on task card**
    - `ardanova-client/src/app/tasks/page.tsx` → `<TaskCard>`
    - Clicking the dashed "+" avatar circle opens a quick-assign popover
    - Shows project member list, clicking a member calls `api.task.update({ id, assigneeId })`

## 5. Wire Task Completion → Equity Flow

- [ ] **[P0] Trigger equity distribution on task completion**
    - `ardanova-client/src/server/api/routers/task.ts` — extend `updateStatus` procedure
    - When `status === "done"` and task has an `assignedToId`:
        1. Call `apiClient.projectTokens.getAllocationsByTask(id)` to find `RESERVED` allocation
        2. If found, call `apiClient.projectTokens.distribute(allocation.id, assignedToId)`
    - If no allocation exists or distribution fails: log warning, do not throw (task status still updates)

- [ ] **[P0] Add XP award on task completion**
    - `ardanova-client/src/server/api/routers/task.ts`
    - After successful distribution, call `apiClient.xpEvents.award` (or equivalent) for the assignee
    - XP amount proportional to task effort/priority (xs=10, s=25, m=50, l=100, xl=200)

- [ ] **[P1] Show equity allocation on task cards**
    - `ardanova-client/src/components/tasks/task-card.tsx`
    - Fetch `getAllocationsByTask` in a secondary query keyed by `taskId`
    - Display: `{equityPercentage}% equity` badge below the token reward
    - Show `DISTRIBUTED` badge (green) vs `RESERVED` badge (amber) based on allocation status
    - Only show if allocation exists; fall back to raw `equityReward` field if no allocation

## 6. Show Equity % on Task Cards

- [ ] **[P0] Surface equityReward from API on task cards**
    - `ardanova-client/src/components/tasks/task-card.tsx`
    - Replace hardcoded `task.reward` with `task.equityReward` from the backend `Task` type
    - Display as token count (raw field) — e.g. `150 tokens`
    - When a `TokenAllocation` exists for the task, display `{equityPercentage}%` alongside or instead

- [ ] **[P1] Add equity breakdown tooltip**
    - Hover on the equity/token display shows: equity %, token amount, allocation status, vesting info if available

## 7. Project-Scoped Task Filtering

- [ ] **[P0] Support URL-based project filter**
    - `ardanova-client/src/app/tasks/page.tsx`
    - Accept `?projectId=...` query param to pre-select project filter on page load
    - Enables deep-linking from project detail page → filtered task board

- [ ] **[P0] Add "Project Tasks" view from project pages**
    - `ardanova-client/src/app/projects/[slug]/page.tsx` or tasks tab
    - "View Tasks" link on project pages navigates to `/tasks?projectId={id}`
    - Tasks tab within a project page uses `api.task.getAll({ projectId })` directly

- [ ] **[P1] Sprint stats reflect real data**
    - `ardanova-client/src/app/tasks/page.tsx`
    - Sprint progress bar and stats (completed SP, in-progress count) computed from live query results
    - `storyPoints` field — map from `estimatedHours` using reverse of `effortToHours` mapping

## 8. Verification

- [ ] **[P0] Build verification**
    - `npm run build` — Next.js frontend builds without type errors
    - `dotnet build` — .NET backend unchanged, still compiles
    - Manual: tasks page loads with real tasks from backend
    - Manual: create a task → it appears in the board
    - Manual: move task to "done" → equity distribution fires (check backend logs)
    - Manual: task card shows equity % from allocation

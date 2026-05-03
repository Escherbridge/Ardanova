# ArdaNova Data Model Fixes — Work Plan

> Generated: 2026-05-02
> Source: Code review + QA audit (docs/data-model-qa.md)
> Total issues: 35+ across schema, .NET backend, and frontend

---

## Phase 0: Data Audit (BLOCKER — must run before Phase 1)

**Goal**: Verify no duplicate data exists before adding unique indexes.

### P0.1 — Run duplicate-data audit on all 9 tables
Run SQL queries against dev/prod database to check for duplicates:
```sql
-- For each table, run:
SELECT guildId, userId, COUNT(*) c FROM GuildMember GROUP BY guildId, userId HAVING c > 1;
SELECT provider, providerAccountId, COUNT(*) c FROM Account GROUP BY provider, providerAccountId HAVING c > 1;
SELECT userId, achievementId, COUNT(*) c FROM UserAchievement GROUP BY userId, achievementId HAVING c > 1;
SELECT leaderboardId, userId, COUNT(*) c FROM LeaderboardEntry GROUP BY leaderboardId, userId HAVING c > 1;
SELECT eventId, userId, COUNT(*) c FROM EventAttendee GROUP BY eventId, userId HAVING c > 1;
SELECT eventId, userId, COUNT(*) c FROM EventCoHost GROUP BY eventId, userId HAVING c > 1;
SELECT followerId, followingId, COUNT(*) c FROM UserFollow GROUP BY followerId, followingId HAVING c > 1;
SELECT userId, projectId, COUNT(*) c FROM ProjectFollow GROUP BY userId, projectId HAVING c > 1;
SELECT userId, guildId, COUNT(*) c FROM GuildFollow GROUP BY userId, guildId HAVING c > 1;
```

### P0.2 — Deduplicate if needed
For each table with duplicates, keep the earliest record (by createdAt), delete the rest.

---

## Phase 1: DBML Schema Fixes

**Goal**: Fix schema source of truth. All changes go to `ardanova-client/prisma/database-architecture.dbml`, then regenerate.

**Dependencies**: Phase 0 complete (no duplicates)

### P1.1 — Add 9 missing unique compound indexes [CRITICAL]
| Table | Index |
|-------|-------|
| Account | `(provider, providerAccountId) [unique]` |
| UserAchievement | `(userId, achievementId) [unique]` |
| LeaderboardEntry | `(leaderboardId, userId) [unique]` |
| GuildMember | `(guildId, userId) [unique]` |
| EventAttendee | `(eventId, userId) [unique]` |
| EventCoHost | `(eventId, userId) [unique]` |
| UserFollow | `(followerId, followingId) [unique]` |
| ProjectFollow | `(userId, projectId) [unique]` |
| GuildFollow | `(userId, guildId) [unique]` |

### P1.2 — Fix GuildMember.role: varchar -> GuildMemberRole enum [HIGH]
Change: `role varchar [not null]` -> `role GuildMemberRole [not null, default: 'MEMBER']`

### P1.3 — Add PlatformTreasury singleton enforcement [MEDIUM]
Add a `singletonKey` column: `singletonKey varchar [not null, unique, default: 'SINGLETON']`

### P1.4 — Add comment to TokenAllocation.taskId [LOW]
Do NOT rename (too much churn). Add note: `taskId varchar [note: 'References ProductBacklogItem.id — PBI-level equity allocation']`

### P1.5 — Regenerate
```bash
cd ardanova-client
npm run generate:prisma    # DBML -> Prisma -> db push
npm run generate:csharp    # DBML -> EF Core entities
```

---

## Phase 2: .NET Backend Fixes

**Goal**: Fix business logic bugs, add missing service behavior, improve data integrity.

**Dependencies**: Phase 1 complete (entities regenerated)

### P2.1 — Fix GuildMember.role type in entity + services [HIGH]
- `GuildMember.cs`: change `public string role` -> `public GuildMemberRole role`
- `GuildServices.cs` line 108: `role = GuildMemberRole.OWNER` (remove .ToString())
- `GuildMemberService.CreateAsync`: change `dto.Role` from string to `GuildMemberRole`
- Update `GuildDtos.cs`: `CreateGuildMemberDto.Role` to `GuildMemberRole` type

### P2.2 — Fix GuildApplication.AcceptAsync to create GuildMember [HIGH — functional bug]
File: `GuildServices.cs` lines 423-436
After setting status to APPROVED, add:
```csharp
var member = new GuildMember {
    GuildId = application.GuildId,
    UserId = application.UserId,
    Role = application.RequestedRole
};
await _guildMemberRepository.AddAsync(member, ct);
guild.MembersCount++;
await _guildRepository.UpdateAsync(guild, ct);
```

### P2.3 — Fix GuildInvitation.AcceptAsync to create GuildMember [HIGH — functional bug]
File: `GuildServices.cs` lines 530-542
Same pattern as P2.2 — create member record on acceptance.

### P2.4 — Add duplicate membership check in GuildMemberService.CreateAsync [HIGH]
Before creating, check: `var existing = await repo.FirstOrDefaultAsync(m => m.GuildId == dto.GuildId && m.UserId == dto.UserId)`
Return conflict if exists.

### P2.5 — Add owner deletion protection in GuildMemberService.DeleteAsync [MEDIUM]
Check if member being deleted is the guild owner. If so, return error.

### P2.6 — Fix denormalized counter updates to be atomic [CRITICAL]
Replace all read-modify-write patterns with atomic SQL:
```csharp
// Instead of: guild.MembersCount++; await _repo.UpdateAsync(guild);
// Use: await _context.Guilds.Where(g => g.Id == guildId)
//          .ExecuteUpdateAsync(s => s.SetProperty(g => g.MembersCount, g => g.MembersCount + 1));
```
Apply to ALL counter fields:
- `Guild.membersCount`, `Guild.reviewsCount`, `Guild.rating`, `Guild.projectsCount`
- `Event.attendeesCount`
- `Post.likesCount`, `Post.commentsCount`, `Post.sharesCount`
- `Opportunity.applicationsCount`, `Opportunity.bidsCount`
- `Project.supportersCount`, `Project.votesCount`, `Project.viewsCount`

### P2.7 — Add GuildReview.rating range validation [MEDIUM]
In `GuildReviewService.CreateAsync`, validate: `if (dto.Rating < 1 || dto.Rating > 5) throw`

### P2.8 — Add guild cascade protection on delete [HIGH]
In `GuildService.DeleteAsync`, check for members, projects, wallets before allowing delete.
Either prevent deletion or cascade cleanup.

### P2.9 — Move auto-opportunity creation to .NET TaskService [HIGH]
File: `TaskServices.cs` — in `CreateAsync`, after task creation:
- Auto-generate slug from task title + ID
- Create Opportunity with `Origin = TASK_GENERATED`, link via `TaskId`
- Wrap in transaction for atomicity
Then remove the logic from `ardanova-client/src/server/api/routers/task.ts` lines 93-111.

### P2.10 — Add Slug auto-generation to CreateOpportunityDto handling [MEDIUM]
The .NET service should auto-generate slugs if not provided, rather than requiring the frontend to send one.

### P2.11 — Add task authorization to .NET TasksController [HIGH]
Currently only enforced in tRPC router. Add to .NET:
- Update/delete: require user is task assignee, project member with sufficient role, or admin

---

## Phase 3: Frontend Fixes

**Goal**: Fix enum mismatches, dead code, type safety, and architecture violations.

**Dependencies**: Phase 2 complete (especially P2.9 before removing tRPC business logic)

### P3.1 — Fix enum mismatches in task router [HIGH]
File: `ardanova-client/src/server/api/routers/task.ts`
- Remove local Zod enums for TaskType, TaskPriority
- Pass backend enum values through directly (SCREAMING_SNAKE_CASE)
- Remove `effortToHours()` conversion — pass `estimatedHours` directly
- Remove mapping functions from `tasks/create/page.tsx` (mapPriorityEnumToRouter, mapTaskTypeEnumToRouter)

### P3.2 — Fix PBIStatus phantom BLOCKED value [HIGH]
File: `ardanova-client/src/lib/api/ardanova/endpoints/backlog.ts`
- Remove `BLOCKED` from `PbiStatus` type (not in DBML)
File: `ardanova-client/src/server/api/routers/backlog.ts`
- Remove `BLOCKED` from the router's type definition

### P3.3 — Fix task create page enum issues [MEDIUM]
File: `ardanova-client/src/app/tasks/create/page.tsx`
- PBI mode: use `PBIType` enum options instead of `TaskType`
- Fix `priorityColors` map: replace `CRITICAL` with `URGENT`
- Fix `EffortEstimate`: either use static local values or remove the `useEnumOptions` call
- Remove `as any` casts on PBI type/priority

### P3.4 — Fix tasks page dead PBI code [MEDIUM]
File: `ardanova-client/src/app/tasks/page.tsx`
- Remove `pbisData` query (line 324-327) — it calls task.getAll, not PBI endpoint
- Remove `mapPbiToRow` function (lines 173-188) — never called
- Remove `pbiStatusToColumn` mapping (line 165-170) — unused
- Keep PBI filter UI only if PBI support will be added soon; otherwise remove

### P3.5 — Remove business logic from task router [HIGH]
File: `ardanova-client/src/server/api/routers/task.ts`
- Remove opportunity auto-creation (lines 93-111) — now handled by .NET (P2.9)
- Remove frontend authorization checks (lines 220-222, 255-257, 279-281) — now in .NET (P2.11)
- Router should be a thin proxy: call apiClient, return result

### P3.6 — Fix type safety in API client types [MEDIUM]
File: `ardanova-client/src/lib/api/ardanova/endpoints/tasks.ts`
- Remove duplicate `assigneeId` field (keep only `assignedToId`)
- Add missing fields to `UpdateTaskDto`: `taskType`, `status`, `estimatedHours`, `dueDate`, `assignedToId`, `pbiId`
- Remove `[key: string]: unknown` index signatures

File: `ardanova-client/src/lib/api/ardanova/endpoints/opportunities.ts`
- Remove `status` from `CreateOpportunityDto` (not in .NET DTO)
- Add proper typed fields instead of `[key: string]: unknown`

### P3.7 — Reduce `any` usage in opportunities-tab [LOW]
File: `ardanova-client/src/components/projects/opportunities-tab.tsx`
- Replace `any` types with proper interfaces based on API response shapes
- Type the inline add form props

### P3.8 — Fix task create page validation [LOW]
File: `ardanova-client/src/app/tasks/create/page.tsx`
- Use `trim()` before length check for description (lines 144-148)
- Match client validation to tRPC schema (`.min(10)` on trimmed value)

---

## Phase 4: Tests

**Goal**: Add test coverage for all fixes.

**Dependencies**: Phases 2-3 complete

### P4.1 — .NET unit/integration tests
- Guild application acceptance creates member
- Guild invitation acceptance creates member
- Guild owner cannot be deleted from guild
- Duplicate membership prevention
- Counter atomic updates under concurrency
- Task creation auto-generates opportunity atomically
- GuildReview rating validation (1-5)
- Task authorization in controller

### P4.2 — Frontend tests (optional, lower priority)
- Task create page enum rendering
- Tasks page without dead PBI code
- Task router thin proxy behavior

---

## Execution Order Summary

```
P0.1 Data audit (BLOCKER)
P0.2 Dedup if needed
  |
  v
P1.1-P1.4 DBML changes (single batch)
P1.5 Regenerate (prisma + csharp)
  |
  v
P2.1 GuildMember.role enum type
P2.2 GuildApplication.AcceptAsync fix  ---|
P2.3 GuildInvitation.AcceptAsync fix   ---|-- can parallelize
P2.4 Duplicate membership check        ---|
P2.5 Owner deletion protection         ---|
P2.6 Atomic counter updates
P2.7 Rating validation
P2.8 Guild cascade protection
P2.9 Move opportunity creation to .NET  --- must complete before P3.5
P2.10 Slug auto-generation
P2.11 Task authorization in .NET        --- must complete before P3.5
  |
  v
P3.1 Fix enum mismatches in task router
P3.2 Fix PBIStatus phantom BLOCKED
P3.3 Fix task create page enums       ---|
P3.4 Fix tasks page dead code         ---|-- can parallelize
P3.5 Remove business logic from router ---|
P3.6 Fix API client types             ---|
P3.7 Reduce any usage                 ---|
P3.8 Fix validation                   ---|
  |
  v
P4.1 .NET tests
P4.2 Frontend tests
```

## Decisions Needed from Team

1. **TokenAllocation.taskId**: Rename to `pbiId` (high churn) or add comment (recommended)?
2. **EffortEstimate**: Add as DBML enum + column, or keep as frontend-only conversion?
3. **PBI on tasks page**: Implement properly or remove dead code?
4. **Soft delete policy**: Standardize `deletedAt` across entities or keep hard delete?
5. **Guild deletion**: Prevent when has members/data, or cascade cleanup?

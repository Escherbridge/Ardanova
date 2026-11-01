---
type: plan
track: mvp-readiness-loop_20261026
status: approved
date: 2026-10-31
revision: 2
title: SR-03 Phase 3 - the feed is the main interaction
computed_at_commit: 9a40e43
---

# SR-03 Phase 3: the feed is the main interaction

## 0. Sequence and the G0 gate

Phase 3 waits for four things to merge, in this order:

1. The money branch (`wip/money-idempotency`). It touches the DBML, migration 20261028000000, generated EF/zod files, `ArdaNova.Infrastructure/DependencyInjection.cs` and `User.cs`.
2. The copy lane.
3. Phase 2.
4. **G0.** Nothing in Phase 3 starts until every item below is done:
   - Re-grep every `owns` list at the new HEAD. The lists below were computed at 9a40e43; Phase 2 file names come from its uncommitted worktree, so their confidence is `none` until merge.
   - Run the feed backfill on the released environment: a dry run first, reviewed, then the real run. It was deferred in ledger:649. **The backfill needs Phase 2 DEPLOYED to the released environment, not just merged.**
   - Have the QA personas post organic seed content BEFORE Home switches to the feed. The owner chose QA-persona organic content (ledger:373). **At G0 the seed is plain POSTs only.** HELP_WANTED asks are seeded through the API in the window between the backend deploy and the client deploy (§9), because the type doesn't exist until S3 and K1 ship.
   - Phase 2 confirms the asks in §7.

After G0: **S0** (the generator) → **S3** (schema) → waves (§6).

## 1. Experience spec

**Home (`/dashboard`, label "Home") is the feed.** It replaces the hero, the lists and the Studio CTAs (dashboard/page.tsx:252-307, :432).

- **Composer on top.** Audience picker (field-note-audience.ts), optional attach to a project or guild you belong to, and inline `#` suggestions. Three prompts: "What moved?" and "What's stuck?" write a POST or PROJECT_UPDATE. **"Who can help?" writes HELP_WANTED.** It opens a task picker over the attached project's open, unassigned tasks (`task.getAll({projectId,status:"TODO"})`, task.ts:163-175).
- **The composer is the authoring studio.** Studio is benched: it moves to the bottom utilities of the nav and nothing else changes.
- **Infinite ranked stream.** `feed {scope:"home"}` with an asOf-pinned cursor and an IntersectionObserver sentinel. After a successful Publish, the client inserts the new card at the top locally, because it is newer than the cursor's asOf.
- **When `nextCursor` is null** (Phase 2: "nothing visible is left", FeedDtos.cs:3), show the cold-start row:
  - sector chips, limited to sectors seen on loaded cards (`project.sectors` from the DTO, so no new API);
  - "Start a project".
  - On an empty platform: one line plus "Start a project".
- **Right rail (lg+):** "Your work", up to 3 assigned tasks. The platform-wide votes are dropped.

**Card anatomy.** Cards render from one registry: a discriminated union with an exhaustive switch, plus a tolerant fallback for unknown types (ledger:534). From top to bottom:
1. Avatar, name, **subject chip with a Follow toggle**. Without the toggle, a person who follows nothing never gets a personalised feed.
2. Body: the person's own words (`bodyIsUserWritten`), or a system line.
3. Hashtag chips, linking to `/tag/x`.
4. Reasons line. It is rendered **client-side from `Reasons[].kind` + `aboutId`**, never from the server's `text`.
5. Counters and **one** primary action (§2).
6. A preview of at most one reply, plus an inline Reply.

**Other pages:**
- **`/tag/[tag]`** is **signed-in only for now** (default; the owner may reverse it). It shows the tag, the term label and parent when the tag resolves, and a `tag:x` stream.
- **Project page:** a **Feed tab, the default** (recency order, see §7), with a light header (title, summary, tags, status, funding %, Follow, one primary action) and `?tab=` deep links. The Updates tab is retired. Comments stays.
- **Guild Feed tab: cut.** Revisit once any guild has ≥10 cards in the trailing 30 days.

**Navigation (app-sidebar.tsx):** the Nova Studio block (:188-206) is replaced by Home. Groups: Work (Tasks, Chats); Explore (Projects, Opportunities, Guilds, People, Events); Records (Governance, Portfolio). Bottom: Studio, Settings. Topbar: a bell.

## 2. Supporting actors

| Surface | Writes into the feed | Card action |
|---|---|---|
| Composer | POST / PROJECT_UPDATE / GUILD_ACTIVITY, and HELP_WANTED (via "Who can help?") | Reply |
| Tasks / board | HELP_WANTED via "Ask for help" (work-item modal → composer in help mode); TASK_COMPLETED / MILESTONE (existing projection) | HELP_WANTED: see below. TASK_COMPLETED: Open board |
| Public discussion | DISCUSSION (existing) | Reply → native thread (`?tab=comments`) |
| Opportunities / funding / events | existing envelopes | Apply / Back this / See events |
| Chats | **Never** | secondary "Message" only |

**HELP_WANTED action (`primaryActionFor` reads only server-computed facts, M2; first matching row wins):**

| Facts | Action |
|---|---|
| task done | Open board → `/projects/{slug}/board` |
| task assigned | Reply (inline; the state chip reads "Picked up by X") |
| `canClaim` (viewer is a member, task unassigned, status not COMPLETED, no agreement, escrow NONE) | **Pick up task** → `/tasks/{id}` |
| otherwise | **Reply**: a public offer on the card that notifies the asker |

**There is no Apply row.** The only writer of `ProjectTask.opportunityId` is TaskCommerceService.cs:214-227. It creates the task from an ACCEPTED bid with `assignedToId` already set, and :195-201 only accepts a bid from the current assignee. So a task with an opportunity is always assigned, and Apply stays on OPPORTUNITY cards only.

On the task page, a paid task (`equityReward > 0`) first does **one** readiness read (`azoaCustodialAccount.getStatus`). That decides between Claim and "Finish setup" → `/settings/verification?returnTo=/tasks/{id}`. This mirrors TasksController.cs:225-233. A 409 from an agreement or escrow (TaskServices.cs:272-274) maps to Reply. There are no per-card readiness calls.

**"Message"** is secondary: `/chats?with={askerId}` opens a DRAFT, and the conversation is created on the first send, never on page load. The asker comes from a new `helpWanted {postId, askerId, askerName}` on TaskDto.

**HELP_WANTED rules (.NET):**
- `taskId` is required. The task must be on the post's project, the author must be a member, and the task must not be COMPLETED.
- `taskId` is refused on any other post type.
- **One HELP_WANTED per task**, enforced by `Post.taskId [unique]` (several NULLs are allowed) plus `CHECK (taskId IS NULL OR type='HELP_WANTED')`. That pair gives "one per task" without a partial index (§8-8).
- A 23505 maps to 409 **carrying the existing post id**. The composer then offers "Edit your ask" instead of showing an error.
- A **project manager can remove a HELP_WANTED post** on their project. Otherwise an ask whose author has left would block that task forever.
- Hydration drops a card whose `taskId` is NULL.

**Claim is a conditional write (H1).** A new `ITaskClaimStore.TryClaimAsync` runs
`UPDATE "ProjectTask" SET "assignedToId"=@me WHERE id=@id AND "assignedToId" IS NULL AND status <> 'COMPLETED' AND "escrowStatus"='NONE' AND NOT EXISTS (agreement)`.
- 0 rows → 409 "Already picked up". TasksController already maps Conflict to 409 (:492).
- It replaces check-then-write (TasksController.cs:275-279 → TaskServices.cs:276-280) on the self-claim path.
- The asker's TASK_ASSIGNED notification is written in the same transaction, so there is exactly one.
- **K1 writes ONE claim-rule predicate** in the Application layer. K1's TryClaim WHERE clause and K2's `canClaim` both use it, so the two cannot drift.
- Implementations/AGENTS.md:211 currently says `TaskService.AssignAsync` is "the only write of `assignedToId` after creation". TaskClaimStore makes a second writer, so **K1 updates that rule.**
- A manager's assign stays check-then-write and can overwrite a fresh claim. Accepted; trigger to revisit: the first report of a claim being silently overwritten.

## 3. Light metadata

**Project create/edit:** one hashtag field with suggestions replaces:
- the category grid (create/page.tsx:217, :548; edit-form:225);
- "Other";
- tags;
- the projectType picker (filter-only, ProjectServices.cs:181-184).

targetAudience, expectedImpact and timeline leave create.

**Stays structured:** title, summary, problemStatement, solution (copy); funding, status, commerce, token config; task status, assignee, reward, escrow.

**.NET resolution is a full replacement** of the derived fields: SECTOR → `categories`, the **first** PROJECT_TYPE tag → `projectType` (or the default), everything else → `tags`. Removing a tag clears it. No backfill: the legacy categories are SECTOR slugs (ledger:456-458).

**Cut:** guild specialties and opportunity skills.

**P8-16 is fixed in full:** opportunities/page.tsx:45-49 (badges), :70-77 (filter options), :187-191 (tabs) and :217.

## 4. "Addictive but constructive"

**Return loops:**
- A reply to your words (COMMENT_REPLY; this includes the credited assignee's TASK_COMPLETED card).
- Your ask picked up (TASK_ASSIGNED, sent to the HELP_WANTED author).
- Followed and member subjects rise, with reasons.
- Funding marks.
- Public credit.
- Tag discovery.

Notifications are in-app only and collapsed per post (`Notification.postId`, S3). **No self-notify:** a person replying to their own post, or an asker claiming their own task, gets nothing. Nothing produces notifications today: only NotificationsController and the MCP tools read `INotificationService`.

Seed notification copy:

| Type | Title | Message |
|---|---|---|
| COMMENT_REPLY | "New reply" | "{name}: “{first 80 chars}”" |
| TASK_ASSIGNED | "Picked up" | "{name} took {task}" |

**Forbidden:** streaks (UserStreak, DBML:343), views, dwell, negative signals, `viewsCount`, "N new posts", non-person badges, push/email nudges, autoplay, stream leaderboards, "trending" labels, backer names or amounts.

## 5. Copy (new surfaces only)

The copy lane owns every existing page, the guides and P8-17. Phase 3 rewrites no existing copy.

- Card chrome is near-silent: the UGC body is the copy.
- Reasons are fragments, rendered client-side from the kind: "You're on P", "Following P", "New in Construction", "#carpentry".
- Headings ≤4 words. A supporting line ≤8 words, and only if it adds information. Buttons are verb + noun, ≤3 words.
- Empty state = one line + one action. At most one quiet inversion per page.
- No "we/our", no eyebrows. "ArdaNova" only in the logo, titles and legal text.
- Funds/units, never "token" (vocabulary.test.ts).
- Seeds: Reply, Pick up task, Apply, Back this, Open board, Finish setup, Message, Follow; states Open / Picked up by X / Done; "Feed didn't load." + Retry.

## 6. Lanes and partitions

Each file has exactly one owner. Backend paths are relative to `ardanova-backend-api-mcp/api-server/src/`, client paths to `ardanova-client/`. Each lane tests at its own seam; the cross-lane flows run at the join.

| Lane | Size | Owns | Seam test |
|---|---|---|---|
| **S0 generator** (P7-18-UP, ledger:670) | M | `scripts/generate-csharp-models.ts` (Phase 2 edits it too, so S0 runs after that merge); new `ArdaNova.Domain/Models/Entities/*.Custom.cs`; the entity files whose hand-written members move | `generate:csharp` on unchanged DBML → zero git diff; `dotnet build` green |
| **S3 schema** | S | `prisma/database-architecture.dbml` (PostType +HELP_WANTED; `Post.taskId` unique + Ref SET NULL; `Notification.postId` + Ref cascade + index (userId, postId)); `prisma/migrations/2026110x_feed_help_wanted/` (+CHECK); generated `schema.prisma`, `src/lib/zod/**`; `ArdaNova.Domain/Models/Entities/Post.cs`, `ProjectTask.cs`, `Notification.cs`, `ArdaNova.Domain/Models/Enums/PostType.cs`, `ArdaNova.Infrastructure/Data/GeneratedModelConfigurations.cs` | Scratch DB: deploy ×2 is a no-op; `migrate diff --exit-code` 0; a second HELP_WANTED on the same task → 23505; taskId on a POST → CHECK violation |
| **K1 writes** | M | `ArdaNova.Application/Services/Implementations/PostService.cs`, `TaskServices.cs`, `NotificationServices.cs`; `ArdaNova.Application/Services/Interfaces/IPostService.cs`, `ITaskService.cs`, `INotificationService.cs`; `ArdaNova.Application/DTOs/PostDtos.cs`, `TaskDtos.cs`; `ArdaNova.Application/Mappings/MappingProfile.cs`; `ArdaNova.API/Controllers/PostsController.cs` (add a Conflict arm, :225-232), `TasksController.cs`; new `ArdaNova.Infrastructure/Tasks/TaskClaimStore.cs`; new shared claim-rule predicate under `ArdaNova.Application/`; `ArdaNova.Infrastructure/DependencyInjection.cs`; `ArdaNova.Application/Services/Implementations/AGENTS.md` (updates §Task assignment :211; appends L's §Hashtags/§Follow text at the join) | Real PG: HELP_WANTED rules (201/403/400; 409 with the existing post id); a manager removes an ask; **two parallel claims → one 200, one 409, one notification**; a claim on a COMPLETED task → 409; reply → asker +1, self → 0 |
| **K2 feed read** | M | Phase 2's `ArdaNova.Application/Feed/**` (read and projector), `Feed/AGENTS.md`, `ArdaNova.Application/DTOs/FeedDtos.cs`, `ArdaNova.Infrastructure/Feed/FeedReadStore.cs`, `ArdaNova.API/Controllers/FeedController.cs`: viewer relation (isMember, isFollowing, `canClaim` through K1's predicate), task facts (paid, assignee name, state), HELP_WANTED hydration, any §7 ask Phase 2 leaves undone | Real PG: a member gets `canClaim`, a follower doesn't; after the task is deleted the card is dropped |
| **L metadata + follow** | M | `ArdaNova.Application/Services/Implementations/ProjectServices.cs` (ProjectService hashtags), `TaxonomyService.cs`, `ArdaNova.Application/Services/Interfaces/ITaxonomyService.cs`, `ArdaNova.Application/DTOs/ProjectDtos.cs`; new `ProjectFollowService.cs` + `IProjectFollowService.cs`; `ArdaNova.API/Controllers/ProjectsController.cs`; `ArdaNova.Application/DependencyInjection.cs`. L's AGENTS.md notes are handed to K1 as text | Hashtags replace (removing a tag clears it; the first PROJECT_TYPE wins); follow a hidden project → 404; a double follow → idempotent 200; no follower-list route |
| **P plumbing** | S | `src/server/api/routers/post.ts`, `project.ts`, `task.ts`, Phase 2's `routers/feed.ts`; `src/lib/api/ardanova/endpoints/posts.ts`, `projects.ts`, `tasks.ts`, Phase 2's `feed.ts`, `index.ts`; `src/server/api/root.ts` (no change planned) | Router tests with only the HTTP edge faked |
| **F feed kit** | L | `src/components/feed/**`, new `src/components/hashtags/**`, new `src/lib/feed/**` (`primaryActionFor`, registry) | Table-driven over every type and HELP_WANTED fact row → exactly one action; an unknown type renders as POST; the sentinel pages |
| **G Home + shell** | M | `src/app/dashboard/page.tsx`, new `src/app/tag/[tag]/**`, `src/components/app-sidebar.tsx`, `app-topbar.tsx` + test, new `src/components/notifications/**` | A zero-follow persona reaches cold-start chips → tag → Follow |
| **H project page** | M | `src/app/projects/[slug]/page.tsx`, `src/components/projects/overview-tab.tsx`, `updates-tab.tsx`, `index.ts` | Opens on Feed; after Publish the card shows at the top at once and is still first after reload |
| **W tasks + chats** | M | `src/app/tasks/[id]/page.tsx`, `src/components/projects/work-item-detail-modal.tsx`, `src/app/chats/page.tsx` | Paid task + not ready → "Finish setup"; `?with=` creates nothing until send |
| **I light forms** | M | `src/app/projects/create/page.tsx`, `src/components/projects/project-edit-form.tsx`, `src/app/opportunities/page.tsx` | A tags-only project lists under its sector; the bounty tab and type filter return TASK_BOUNTY |

**Order:**
- G0 → S0 → S3.
- **Wave 1:** K1 ∥ K2 ∥ L ∥ F (the F0 hashtag primitive first) ∥ P (against the spec).
- **Wave 2** (after F + P): G ∥ H ∥ W ∥ I.

**Join:**
- One sweep: `dotnet test` with `ARDANOVA_TEST_POSTGRES`; `npm ci` + lockfile guard + tsc + vitest.
- Cross-lane Playwright flows on the local stack:
  1. **B** (a member) posts "Framing the east wall Saturday, need two hands #carpentry" as HELP_WANTED on T.
  2. **A**, a MEMBER of P, sees B's words with reason kind ProjectTeam, taps Pick up task, lands on `/tasks/T`, claims it, and B's bell shows 1.
  3. **C** only FOLLOWS P. C sees the same card with reason FollowedProject and action Reply. C replies, and B's bell rises by 1. C's "Message" creates nothing until C sends.
- Review: `/code-review high` on K1/K2/L, `critic` on mechanics.

## 7. Phase 2 asks (H3) and assumptions

**H3 needs a small Phase 2 behaviour change, but no wire change.** Phase 2 already reads project and guild scopes newest-first (FeedReadContracts.cs:79-80) and limits discovery to Home (FeedRanking.cs:194). These cite Phase 2's uncommitted worktree. Still needed:

| # | Ask | Owner if Phase 2 doesn't do it |
|---|---|---|
| Q1 | Project/guild policy: per-author cap → null (today `HomeAuthorCap`, FeedService.cs:475). Under recency a cap reorders a one-writer project | K2 |
| Q2 | Tag policy: subject cap → null, per-author cap only (FeedService.cs:474) | K2 |
| Q3 | The tag scope also matches cards whose project's sectors contain the term the tag resolves to. Today it is `hashtags @> [tag]` only (FeedService.cs:273-277) | K2 |

**Assumptions (reversal cost):**

| # | Assumption | If wrong |
|---|---|---|
| A-1 | The DTO keeps Phase 2's shape (FeedDtos.cs) | Small: F retypes |
| A-6 | Home stays at `/dashboard` (page.tsx:84-87) | Small: `/home` alias |
| A-7 | Recency for project/guild with no caps and no discovery; tag gets the per-author cap only; Home gets full ranking | Small (Q1/Q2) |

## 8. Pushback (evidence)

1. **No "task opened" projection.** Tasks project only on completion (FeedProjectionRules.cs:222-226), and there is no needs-help state (DBML:427-433). The person-written HELP_WANTED replaces it.
2. **Claiming is members-only.** Membership means the creator or a member, not followers (HierarchyAuthorizationService.cs:69-82; TasksController.cs:221-223). There is no membership-request API. Non-members get Reply.
3. **No "draft with Nova".** Studio drafts are templates (studio/page.tsx:403-442), and AI branding of templates undoes SR-01 (ledger:204).
4. problemStatement and solution are copy. The form only ever offered the 10 legacy categories (create/page.tsx:217).
5. **TaxonomyTerm has no aliases** (DBML:2919-2935). Phase 2 folds case but not separators (Hashtags.cs `Normalize`).
6. **Project follow doesn't exist** (only the entity, DBML:1964), so Phase 2's `FollowedProjects` is always empty. It must not copy guild follow's flaws:
   - no visibility check (GuildServices.cs:1279);
   - an exists-then-insert race (:1283);
   - a public follower list and a check on any user (GuildsController.cs:285-304).
7. There are two update writers (field-note-composer.tsx:61; updates-tab.tsx:31). The composer wins.
8. **M4's partial unique index is replaced** by `taskId [unique]` + CHECK. Prisma 6.19.3 has no partial-index model, the repo has none in any migration, and the migrate gate depends on `migrate diff --exit-code` 0. A plain unique on a column only HELP_WANTED rows may fill gives the same guarantee and can be expressed in the DBML.

## 9. Deploy and rollback

**Deploy:** migrate (additive) → deploy the backend (a fresh boot reloads the PG enum map, `MapEnum`, ArdaNova.Infrastructure/DependencyInjection.cs:50) → client.

**Rollback:** before rolling the backend back past S3, run
`UPDATE "Post" SET type='POST', "taskId"=NULL WHERE type='HELP_WANTED'`.
An old backend cannot read an enum label it doesn't know.

## 10. Risks and deferred

| Item | Mitigation / trigger |
|---|---|
| DMs have no block or consent check (ChatService.cs:135-184) | Draft-until-send only; add a per-sender limit on new DIRECT conversations at the first abuse report |
| The copy lane and the create page | Branch after the merge (§0) |
| DOM growth | Virtualize when sessions pass ~200 cards |
| Per-post notification collapse may double under concurrent replies | Accepted (harmless) |
| Re-asking about a released task | Deferred. Re-ask = update the same post and bump the timestamp Phase 2's recency ordering reads (column named once Phase 2 merges). Trigger: a released task's ask goes unseen |
| Tag follows, aliases, membership requests, email/push, native-thread reply notifications, backing affinity, guild Feed tab, signed-out tag pages, `ardanova-ai-client/` | Deferred; triggers: repeat tag visits, near-duplicate unresolved tags, DM offers outgrowing manual adds, the P5-20 sender going real, a soak review, a guild with ≥10 cards in 30 days |

## Revision 1 (critic)

**Blockers fixed:**
- B1: A is a member; C (a follower) takes the non-member path.
- B2: the live route is TaskServices.cs:266-284, now owned by K1.

**High fixed:**
- H1: the claim is a conditional update with a parallel test.
- H2: action routing table, draft DMs, paid-task readiness and the DM risk.
- H3: answered with Phase 2 evidence; asks Q1-Q3.
- H4: G0 backfill and seed content, the cold-start row at a null cursor, the Follow toggle.

**Medium fixed:**
- M1: single owners for every file, K split into K1/K2, merge sequence.
- M2: server-side viewer and task facts; asker on TaskDto.
- M3: follow hardening.
- M4: plain unique + CHECK, a deliberate deviation (§8-8).
- M5: rollback rule; the backend is one deploy step.
- M6: signed-in tag pages.
- M7: lanes test at their seams; cross-lane flows at the join.
- M8: full-replacement hashtags.

**Low:** P8-16 lines, the `ArdaNova.` path prefix, "Who can help?" → HELP_WANTED, votes dropped from the rail, notification copy, no-self-notify, S0 added.

**Cuts adopted:** guild and opportunity tags, signed-out tag pages, the votes rail, Studio "Share as update", the guild Feed tab.

## Revision 2 (critic re-check: APPROVE WITH CHANGES; M4 unique + CHECK accepted)

- **HIGH:** the HELP_WANTED "Apply" row and join step 4 are deleted. The only writer of `ProjectTask.opportunityId` (TaskCommerceService.cs:214-227, :195-201) creates an already-assigned task, so the row could never fire.
- **MED:** G0 seeds plain POSTs only; HELP_WANTED asks are seeded between the backend and client deploys.
- **MED:** K1 owns Implementations/AGENTS.md, updates the :211 single-writer rule, and writes the one claim predicate shared with K2's `canClaim`.
- **LOW:** TryClaim also requires status <> COMPLETED. Accepted with a trigger: a manager's check-then-write assign can overwrite a fresh claim. Accepted as harmless: collapsed notifications may double. The backfill needs Phase 2 deployed, not just merged.
- **M4 consequences:** the 409 carries the existing post id and the composer offers "Edit your ask"; a manager can remove an ask; re-ask is deferred with a trigger.

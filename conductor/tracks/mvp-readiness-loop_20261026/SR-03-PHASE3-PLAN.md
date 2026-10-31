---
type: plan
track: mvp-readiness-loop_20261026
status: proposed
date: 2026-10-31
title: SR-03 Phase 3 - the feed is the main interaction
computed_at_commit: 9a40e43
---

# SR-03 Phase 3: the feed is the main interaction

The partitions below were computed at 9a40e43, which is 32c33af plus one ledger line. **Two gates come before any lane branches:** Phase 2 must be merged, and the copy lane must be merged. Both move UI files, so every `owns` list below is a hypothesis until it is re-grepped at that HEAD.

## 1. Experience spec

**Home (`/dashboard`, label "Home") is the feed.** It replaces the hero, the "core loop" lists and the Studio CTA (dashboard/page.tsx:252-307). From top to bottom:
- **Composer.** Has an audience picker (reuses field-note-audience.ts), an optional attach to a project or guild you belong to, inline `#` suggestions, and three plain prompts.
- **Sector chips.** Only for viewers who follow nothing: 8 SECTOR terms, each linking to `/tag/[slug]`.
- **Infinite ranked stream.** `useInfiniteQuery` on `feed {cursor,limit,scope:"home"}`, with an IntersectionObserver sentinel about 2 screens ahead. The cursor is pinned to `asOf`, and "Refresh" re-pins it. There is no end marker. On error the stream shows one line and Retry.
- **Right rail (lg+): "Your work".** Up to 3 assigned tasks and up to 3 open votes, moved from today's lists (dashboard/page.tsx:226-235). On mobile this becomes a link to `/tasks`. `SetupPrompt` stays.

**Card anatomy (each `postType` renders from one registry: a discriminated union with an exhaustive switch, plus a fallback for unknown codes, following the Phase 0 tolerant-reader rule, ledger:534):**
1. Avatar, name, subject chip, relative time.
2. **Body = the person's own words** (`Post.content`, or text hydrated from the source). A system card shows its title plus the existing system line (FeedProjectionRules.cs:182-197).
3. Hashtag chips, each linking to `/tag/x`.
4. **Reasons line:** one muted fragment.
5. Footer: approvals and replies counters, then **one** primary action.
6. A preview of at most one reply, plus an inline Reply input on person-written types.

Funding cards show a percent bar only. HELP_WANTED cards show their state: Open / Picked up by X / Done.

**`/tag/[tag]`** shows the tag, the resolved label and its parent sector when the tag maps to a term ("Carpentry · Construction"), and a stream with `scope:"tag:x"`. For signed-in viewers the composer is pre-filled with `#x`. The page works signed out.

**Project and guild pages** get a **Feed tab as the default** (scope `project:{id}` / `guild:{id}`) and a light header: title, summary, hashtags, status, funding %, Follow, and one primary action (Join or Back). `?tab=` deep links are supported. The Updates tabs are retired (project page tabs :57-69, default tab :121-123; guild page :34-45). Comments stays because DISCUSSION cards link into it.

**Navigation (app-sidebar.tsx):**
- The "Nova Studio" block at the top (:188-206) is removed, and **Home** becomes the first item.
- Groups: Work (Tasks, Chats); Explore (Projects, Opportunities, Guilds, People, Events); Records (Governance, Portfolio).
- Bottom utilities: Studio, Settings.
- The topbar gets a bell.

**Cold start never dead-ends.** A viewer with no follows gets only discovery items (the Phase 2 ranker), plus the sector chips. If the platform itself is empty, the page shows one line and one action ("Start a project").

## 2. Supporting-actor wiring

| Surface | Writes into the feed | Card action that links back into it |
|---|---|---|
| Composer (Home/project/guild/tag) | POST / PROJECT_UPDATE / GUILD_ACTIVITY with #tags (person-written; PostService.cs:198-237) | Reply (inline) |
| Tasks / board | **HELP_WANTED (new, person-written, `Post.taskId`)** via "Ask for help", which opens the composer in help mode. TASK_COMPLETED / MILESTONE (existing projection) | "Pick up task" → `/tasks/[id]`; "Open board" → `/projects/[slug]/board` |
| Public project discussion | DISCUSSION (existing; PROJECT/MILESTONE targets PUBLIC, backlog talk PROJECT_MEMBERS) | "Reply" → `/projects/[slug]?tab=comments` (native thread; PostService.cs:486 refuses a second thread) |
| Opportunities / funding / guilds / events | existing envelopes | Apply → `/opportunities/[slug]`; Back this → `/projects/[slug]/invest`; Open guild; See events |
| Studio | Nothing directly. "Share as update" hands the text to the composer through a sessionStorage key; nothing posts until the person taps Publish | none |
| Chats | **Never.** DMs and groups have no project link (Conversation, DBML:1772-1785) | "Message" → `/chats?with={userId}` (new param; getOrCreateDirect exists, chat.ts:105) |

**HELP_WANTED rules (enforced in .NET):**
- `taskId` is required, the task must belong to the post's project, the author must be a project member, and the task must not be COMPLETED.
- One live HELP_WANTED per task (a second one returns 409).
- Audience is the post's own; `OnAVisibleProject` hides a DRAFT project's cards.
- Hydration drops the card when `taskId` is NULL (the task was deleted; the FK is SET NULL). This is the defence-in-depth rule (Feed/AGENTS.md:186-193).

**No new projection rules.** The reasons are in §7-1.

## 3. Light metadata

| Form | Becomes copy + hashtags | Stays structured (why) |
|---|---|---|
| Project create/edit | The required category grid (create/page.tsx:217,548; edit-form:225), "Other" free text, the tags field, and the projectType picker (it only feeds filters, ProjectServices.cs:181-184) become **one hashtag field with suggestions**. targetAudience/expectedImpact/timeline leave create and stay under "More" in edit | title, summary, problemStatement, solution (**these are copy**); funding goal/unit, status, commerce, token config (money and permissions) |
| Guild form | specialties (guild-form.tsx:413-421) | membership and roles |
| Opportunity form | skills | type, compensation, deadline, maxApplications (bids and commerce) |
| Tasks | — | status, assignee, equityReward, escrow, due date (payments and permissions) |

.NET resolves hashtags against TaxonomyTerm: SECTOR → `categories` (whole-value codes), PROJECT_TYPE → `projectType` when it isn't set, everything else → `tags` / `specialties` / `skills`. **There is no backfill.** The legacy categories already are SECTOR slugs (ledger:456-458), and subject hashtags are derived at read time. P8-16 (the dead opportunity type filter, opportunities/page.tsx:70-77, :217) is fixed here.

## 4. "Addictive but constructive"

**What brings people back (allowed):**
1. Someone replied to your words. This is a COMMENT_REPLY notification, and it also fires for a credited assignee's TASK_COMPLETED card.
2. Your ask was picked up. This is TASK_ASSIGNED, sent to the HELP_WANTED author when someone claims the task.
3. Projects you follow, back or belong to rise on Home, with reasons, and funding marks show at 25/50/75/100.
4. Credit is public (SR-03-D1).
5. Discovery that is relevant: tag pages, and every 5th slot is a subject you don't follow.

Notifications are **in-app only**, come from just those two producers, and are collapsed per post. Today **nothing produces a notification at all**: only NotificationsController and the MCP tools read `INotificationService`.

**Forbidden:**
- Streaks (UserStreak DBML:343 stays unwired).
- Views, dwell or negative signals; `viewsCount` is never rendered.
- "N new posts" pills, and badges for anything other than person-to-person events.
- Push or email nudges, autoplay, leaderboards in the stream, "trending" labels.
- Backer names or amounts.

## 5. Copy (new Phase 3 surfaces only)

The copy lane owns every existing page, BRAND_GUIDELINES/LINGUISTIC_GUIDE and **P8-17**. Phase 3 rewrites no existing copy. Lane G removes the dashboard hero as a **structural** change, and whatever strings survive keep the copy lane's wording.

Rules for card chrome, the composer, reasons, tag pages and empty/cold-start states:
- **Card chrome is near-silent.** The UGC body is the copy, and there is no explanatory text on a card.
- Headings ≤4 words. A supporting line (≤8 words) only when it adds information. Buttons are verb + noun, ≤3 words.
- Empty state = one line + one action.
- At most one quiet inversion per page.
- No "we/our", no eyebrows. "ArdaNova" appears only in the logo, titles and legal text.
- Vocabulary allowlist: funds/units, never "token" (src/lib/vocabulary/vocabulary.test.ts).

Seed strings:
- Prompts: "What moved?" / "What's stuck?" / "Who can help?"
- Actions: Reply, Pick up task, Back this, Apply, Open board, Open project, Open guild, See events, Message
- Reasons: "Because you follow X", "You back X", "You're on X", "New in #x"
- States: Open / Picked up by X / Done
- Errors and empty: "Feed didn't load." + Retry; "Nothing here yet." + "Start a project"

## 6. Lanes, partitions, order

**Shared files (the orchestrator integrates them at the join):**
- `Application/DependencyInjection.cs` (L registers ProjectFollowService)
- `Services/Implementations/AGENTS.md` (K adds §Posts help-wanted/replies; L adds §Hashtags and §Follow)
- `ardanova-client/src/server/api/root.ts` (no change planned)
- The ledger

**Phase 2 files** (ranker, feed read/hydration, `routers/feed.ts`): owner is listed per lane, with `confidence: none` until the file names exist on main.

| Lane | Size | Owns (backend paths relative to `ardanova-backend-api-mcp/api-server/src/`, client paths relative to `ardanova-client/`) | Flow test (public seam) |
|---|---|---|---|
| **S3 schema** | S | `prisma/database-architecture.dbml` (PostType +HELP_WANTED; `Post.taskId` + Ref SET NULL + index); `prisma/migrations/2026110x000000_feed_help_wanted/`; generated `prisma/schema.prisma`, `src/lib/zod/**`; `Domain/Models/Entities/Post.cs`, `ProjectTask.cs`, `Domain/Models/Enums/PostType.cs`, `Infrastructure/Data/GeneratedModelConfigurations.cs` (restore hand-written members, P7-18-UP ledger:670) | Scratch DB: deploy ×2, the second applies nothing; `migrate diff --exit-code` 0; delete a task → `taskId` NULL |
| **K feed writes** | M | `Application/Services/Implementations/PostService.cs`, `Services/Interfaces/IPostService.cs`, `DTOs/PostDtos.cs`, `API/Controllers/PostsController.cs`, new test files; Phase 2 hydration (task state, `replyPreview` if A-4 is missing) | Real PG: member B posts HELP_WANTED on task T → 201; non-member → 403; task from another project → 400; second live post → 409; A replies → B has 1 COMMENT_REPLY, B replying to self → 0 |
| **L metadata + follow** | M | `Implementations/ProjectServices.cs` (ProjectService hashtags; ProjectTaskService.AssignAsync notifies the asker), `GuildServices.cs`, `OpportunityServices.cs`, `TaxonomyService.cs` + `ITaxonomyService.cs` (resolver; reuse Phase 2's if one exists), `DTOs/ProjectDtos.cs`, `GuildDtos.cs`, `OpportunityDtos.cs`, new `ProjectFollowService.cs` + `IProjectFollowService.cs` (mirrors IGuildFollowService), `API/Controllers/ProjectsController.cs`; Phase 2 ranker/scope predicate if A-2/A-3 fail | Create a project with `#Construction #carpentry #leeds` → categories CONSTRUCTION, and `?category=CONSTRUCTION` lists it; A follows P → Home has P's card with "Because you follow P"; A claims T → B has 1 TASK_ASSIGNED |
| **P client plumbing** | S | `src/server/api/routers/post.ts`, `project.ts` (hashtags; `categories` optional; follow/unfollow/isFollowing), `guild.ts`, `opportunity.ts`; `src/lib/api/ardanova/endpoints/posts.ts`, `projects.ts`, `guilds.ts`, `opportunities.ts`; router tests | Router tests with only the HTTP edge faked: `post.create` forwards `type:HELP_WANTED,taskId`; `project.create` passes hashtags with no categories |
| **F feed kit** | L | `src/components/feed/**`, new `src/components/hashtags/**`, new `src/lib/feed/**` (pure `primaryActionFor(card, viewer)`, card registry, composer-draft storage) | Vitest + testing-library, fake transport serving 2 pages: the sentinel loads page 2; table-driven over every postType: body first, reasons line, exactly one action with the right href; unknown type renders as POST |
| **G Home + shell** | M | `src/app/dashboard/page.tsx`, new `src/app/tag/[tag]/**`, `src/components/app-sidebar.tsx`, `src/components/app-topbar.tsx` + test, new `src/components/notifications/**` | Playwright (local stack): zero-follow A sees chips + cards → `/tag/construction` → follows P |
| **H scoped pages** | M | `src/app/projects/[slug]/page.tsx`, `src/components/projects/overview-tab.tsx`, `updates-tab.tsx`, `index.ts`; `src/app/guilds/[slug]/page.tsx`, `src/components/guilds/overview-tab.tsx`, `updates-tab.tsx`, `index.ts` | The project opens on Feed; a composer post with #tag lands at the top; `?tab=comments` opens the thread |
| **W supporting actors** | M | `src/app/tasks/[id]/page.tsx` (Pick up / Message), `src/components/projects/work-item-detail-modal.tsx` ("Ask for help"), `src/app/chats/page.tsx` (`?with=`), `src/app/studio/**`, `src/lib/studio/**` | A non-member's task page shows Message → a DM with the asker opens; Studio "Share as update" opens the pre-filled composer and nothing is posted until Publish |
| **I light forms** | M | `src/app/projects/create/page.tsx`, `src/components/projects/project-edit-form.tsx`, `src/components/guild-form.tsx`, `src/components/opportunity-form.tsx`, `src/app/opportunities/page.tsx` | Create a project from title + problem + making + tags → the header shows them; the "Task bounty" filter returns TASK_BOUNTY |

**Order:**
- **G0:** Phase 2 and the copy lane are merged → re-grep partitions, check A-1..A-5 (about 30 min).
- **Wave 1:** S3 ∥ F (+ the F0 hashtag primitive first) ∥ P (written to the spec).
- **Wave 2** (after S3): K ∥ L. P is then finalized against their DTOs.
- **Wave 3** (after F + P): G ∥ H ∥ W ∥ I.
- **Join:** one sweep. `dotnet test` on both projects with `ARDANOVA_TEST_POSTGRES`; `npm ci` + lockfile guard + tsc + vitest; the Playwright suite. Then adversarial review: `/code-review high` on K+L (HELP_WANTED authz, follow, notification fan-out) and `critic` on ranking mechanics.

**The track flow test** runs end to end on the local stack:
1. A follows a construction project P.
2. B posts "Framing the east wall Saturday, need two hands #carpentry" as HELP_WANTED on task T.
3. A's Home shows B's words with "Because you follow P".
4. A taps "Pick up task", lands on `/tasks/T` and claims it.
5. B's bell shows 1.

**Deploy:** migrate (additive) → **restart the BFF** (Npgsql caches enum types per data source, ledger:554) → backend → client. No HELP_WANTED rows exist until the new client writes them.

**Assumptions about Phase 2 (each with its reversal cost):**

| # | Assumption | If wrong |
|---|---|---|
| A-1 | The server parses `#tags` from content; DTO `hashtags[]` is canonical (case- and separator-folded) | F only displays the DTO; S |
| A-2 | `tag:x` matches post tags OR the subject's resolved terms | L extends the predicate; M (otherwise tag pages look empty) |
| A-3 | Affinity reads ProjectFollow/GuildFollow/UserFollow, membership and backing | L adds the sources; S |
| A-4 | The DTO carries deep-link ids (slugs, taskId, thread) + `replyPreview` | K adds them; S-M. Never N+1 from the client |
| A-5 | The feed router is public with the viewer attached (post.ts:26-33 pattern); a resolved HELP_WANTED is demoted | L adds the demotion; S |
| A-6 | Home stays at `/dashboard` (callbackUrls page.tsx:84-87, admin redirects) | add a `/home` alias; S |

## 7. Pushback (evidence)

1. **No "task opened" projection (pushback on the coordinator's suggestion).**
   - Tasks project only on completion (FeedProjectionRules.cs:222-226).
   - TaskStatus has no "needs help" state (DBML:427-433), so unassigned is not the same as needing help.
   - Boards are created in bulk, so projecting every open task would broadcast backlog grooming.
   - A projected card carries only a system line, which breaks UGC-first.
   - Instead: HELP_WANTED is person-written.
2. **"Pick up this task" is members-only.** The assignee must be a project member (TasksController.cs:221-223), and a claim must be self + unassigned (:275-279). `ProjectMembershipRequest` is an entity with no API. Non-members get "Message".
3. **No "draft with Nova" in the composer.** Studio drafts are local string templates (studio/page.tsx:403-426, "Local draft generated for review" :442). The only live Nova is idea refinement, capped at 10 a day (components/ai/AGENTS.md). Branding templates as AI undoes SR-01 (ledger:204). The composer gets plain prompts; Studio gets "Share as update".
4. **problemStatement and solution are copy, not metadata, so they stay.** The categories the form offers only come from the legacy 10-value enum (create/page.tsx:217). Members never saw the broad vocabulary, and hashtags fix that.
5. **TaxonomyTerm has no aliases** (DBML:2919-2935). `#builds` will not bridge to CONSTRUCTION. Unresolved tags stay as free tags this phase.
6. **Project follow does not exist.** There is an entity (DBML:1964) but no service, route or UI, so the owner's flow test cannot run without Lane L.
7. **Updates are written in two places.** The field-note composer writes PROJECT_UPDATE posts (field-note-composer.tsx:61), and the Updates tab writes ProjectUpdate rows (updates-tab.tsx:31). One composer wins; the old rows keep projecting.

## 8. Risks and deferred

| Item | Mitigation / trigger to revisit |
|---|---|
| Phase 2 contract drift | G0 check; F codes against the merged DTO type |
| The copy lane collides on the 3,952-line create page | I branches after the copy merge |
| Help-wanted spam | One live post per task; ranker diversity caps |
| DOM growth on infinite scroll | Virtualize when sessions pass about 200 cards |
| Tag follows (new table) | Revisit when tag pages see repeat visits |
| Taxonomy aliases | Revisit when the top unresolved tags near-duplicate existing terms |
| Membership-request API | Revisit when help offers by DM outgrow manual adds |
| Email/push | Revisit when the P5-20 email sender is real |
| Reply notifications on native threads, event detail page, public feed on landing | After the Phase 3 soak |
| crewai `ardanova-ai-client/`; deleting Studio | No investment; deletion is the owner's call |

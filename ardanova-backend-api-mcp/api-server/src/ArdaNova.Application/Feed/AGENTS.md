# Feed projector (SR-03 Phase 1, Lane A)

Every feedable event becomes ONE envelope row in `Post`: `type` (PostType), provenance
`(sourceType, sourceId)` (UNIQUE, CHECK both-or-neither), and nullable target FKs
`projectId / guildId / opportunityId / eventId` (ON DELETE SET NULL). A post a person wrote keeps
`sourceType` NULL. Ranking and body hydration are Phase 2 (Lane B), not here.

## Why a pull projector

`FeedProjectorCycle` READS committed source rows and makes the envelopes match them. It is not an EF
`SaveChangesInterceptor` and not an edit to each domain service:

- Seven `BeginTransactionAsync` call sites in Application would let an interceptor project a change that
  later ROLLS BACK (a phantom funding card), and a unique-index race inside it would abort the domain write.
- A pull reader only ever sees what committed, and a failed insert costs one card, never a sponsorship.

The split mirrors the outbox: `FeedProjectorCycle` (this directory) holds every rule and decision;
`Infrastructure/Feed` holds the reader, the envelope store (unique-violation handling, child-row deletes,
the advisory lock) and the hosted timer.

## Mapping

`FeedProjectionRules.Enter` (does this row earn a card, for whom) and `Retain` (does a stored card stay,
for whom). Retain is never narrower than Enter, and never wider on privacy.

| source (FeedSourceType) | envelope type | audience | author | enters when | leaves when |
|---|---|---|---|---|---|
| Project (PROJECT) | PROJECT_LAUNCHED | PUBLIC | creator | `publishedAt` set | project deleted |
| ProjectUpdate | PROJECT_UPDATE | PUBLIC | writer | created | deleted |
| ProjectMilestone | MILESTONE | PUBLIC | `assigneeId`, else project creator | `completedAt` set | deleted / `completedAt` cleared |
| ProjectTask | TASK_COMPLETED | PUBLIC | `assignedToId`, else project creator | `completedAt` set | deleted / reopened |
| ProjectTokenConfig | FUNDING_PROGRESS, sourceId `{cfgId}:{25,50,75,100}` | PUBLIC | project creator | highest mark reached (only that one) | config deleted |
| ProjectMember | MEMBER_JOINED | PUBLIC | the member | joined, unless they created the project | removed |
| ProjectComment (root only) | DISCUSSION | PROJECT/MILESTONE target: PUBLIC; EPIC/SPRINT/FEATURE/PBI/TASK: PROJECT_MEMBERS | writer | created | deleted |
| Guild | GUILD_FOUNDED | PUBLIC | owner | created | deleted |
| GuildUpdate | GUILD_UPDATE | PUBLIC | writer | created | deleted |
| GuildMember | MEMBER_JOINED | PUBLIC | the member | joined, unless they own the guild | removed |
| Opportunity | OPPORTUNITY_OPENED | PUBLIC | poster | status OPEN | deleted / back to DRAFT |
| OpportunityUpdate | OPPORTUNITY_UPDATE | PUBLIC | writer | created, opportunity not DRAFT | deleted / opportunity re-drafted |
| OpportunityComment (root only) | DISCUSSION | PUBLIC | writer | created, opportunity not DRAFT | deleted / opportunity re-drafted |
| Event | EVENT_SCHEDULED | the event's own: PUBLIC / PROJECT_MEMBERS / GUILD_MEMBERS | organizer | SCHEDULED or LIVE, not INVITE_ONLY | deleted / DRAFT / CANCELLED / INVITE_ONLY |

Proposals and products are excluded for MVP.

Project-scoped envelopes (including an opportunity's or event's project) carry `projectId`, so
`PostService.OnAVisibleProject` hides every card of a DRAFT project from strangers, exactly as it hides its
field notes. Guilds have no draft state.

Choices the data model forced (reported to the lane owner):

- **No "opened at" / "scheduled at" columns.** Opportunity and Event entry is observed through `updatedAt`
  plus status, and the card's time is the `updatedAt` at first projection. A DRAFT → OPEN → CLOSED flip
  inside one look-back window is missed (rare; a backfill does not recover it either).
- **Funding projects the HIGHEST mark only.** 10% → 60% in one sponsorship writes `:50`, not `:25` and
  `:50`. A mark stays once reached (a refund does not retract "reached 50%"). Goal and raise are read
  through `ProjectFundingLedger` and never leave the rule: metadata is `{"percent":N}` and nothing else -
  no backer, no amount, whatever `showBackerName` says, because the reader never loads investments.
- **Replies are not cards.** Only root comments become DISCUSSION; a card's metadata
  `{"thread":{source,targetType?,targetId?,opportunityId?,commentId}}` deep-links the native thread, and
  `PostService.AddCommentAsync` refuses a reply on a DISCUSSION envelope so there is never a second thread.
- **`ProjectMilestoneService.UpdateAsync` can set status COMPLETED without stamping `completedAt`**; only
  `CompleteAsync` stamps it, so only that path produces a MILESTONE card.
- **Founder / guild owner are not "joined" news**: `ProjectMember.userId == Project.createdById` and
  `GuildMember.userId == Guild.ownerId` are skipped (guild creation already writes the owner's row).
- **TASK_COMPLETED / MILESTONE credit the person who did the work** (owner decision, review round 1):
  `assignedToId` (task) / `assigneeId` (milestone), else the project creator. The assignee is already public on
  task reads (`TaskDto.AssignedToId`/`AssignedTo`), so the card publishes nothing new. The credit is fixed at
  projection: re-assigning a finished task does not re-author its card. No milestone endpoint sets `assigneeId`
  yet, so milestone cards credit the founder until one does.

## Windows (`Infrastructure/Feed/FeedSourceReader`)

Each kind is read by every timestamp that can change its card, so a change is judged within one cycle:
Project `publishedAt|updatedAt`; milestone/task `completedAt|updatedAt` (CompleteAsync does not bump
`updatedAt`); funding, opportunity, event `updatedAt`; opportunity updates/comments their own `createdAt`
OR the opportunity's `updatedAt` (opening or re-drafting it changes whether they may show); the rest
`createdAt`/`joinedAt`. The look-back (10 min) must cover at least two cycles so a slow commit is seen.

**High-water mark, not a fixed window.** `FeedProjectorWatermark` records, PER KIND, when the last cycle that
read and applied that kind began. A cycle reads each kind from `min(lastReadAt - margin, now - LookBack)`, the
margin being `LookBack` itself (a row stamped just before the last read began but committed after it is as late
as anything a normal cycle tolerates). In steady state that is the old `now - LookBack`; after a stall (database
unreachable, lock held elsewhere, host paused) longer than `LookBack`, the window stretches back to the last good
read, so rows committed during the stall are projected instead of silently skipped. The mark advances only after
the apply finishes, and only for the kinds whose read succeeded, so a kind that keeps failing keeps its own
window open and catches up the cycle it reads again (per kind, not one global mark, or a broken kind would be
skipped past while the others advanced).

**Restart behaviour.** The mark lives in memory on `FeedProjectorHostedService` (the cycle is Scoped, so it
cannot hold it). A fresh process has no mark and reads `now - LookBack`: rows committed while the projector was
DOWN for longer than `LookBack` are not projected by the timer. That is the same gap the fixed window always
had at startup; a normal redeploy overlaps or restarts within `LookBack` and loses nothing. The safety nets:
the first cycle after start is a full sweep (removals are always caught), and after any outage longer than
`LookBack` - or when the projector is first enabled on a database - run `POST /api/admin/feed/backfill`
(dry run first). Persisting the mark would close the gap; it needs a table and was not worth one for MVP.
A second instance keeps its own mark; it only ever reads MORE (its mark is older), never less.

**Failure isolation.** A read is one kind at a time (`IFeedSourceReader.ReadChangedSinceAsync(kind, ...)`); a kind
whose read throws is logged at Error, counted (`FeedProjectorRun.SourceReadsFailed`) and skipped while every other
kind goes ahead. In the sweep, an unreadable kind's cards are LEFT ALONE - a failed read is never evidence of a
deletion. Each card's rule evaluation, insert, re-audience and removal is likewise isolated (`CardsFailed`); a
removal batch that throws is retried one envelope at a time. A card that fails on its own does not hold its
kind's mark back (a poison row would otherwise widen that kind's window forever): it is retried every cycle while
its source is inside `now - LookBack`, then only by a backfill, and every failure is logged with its key.
Cancellation is never swallowed.

**Stored-card lookup.** `FindStoredAsync` issues one query per kind with `sourceType = <kind> AND sourceId = ANY(...)`
(`FeedProjectionRules.IsEnvelopeOf`): an equality on the leading column is what lets Postgres seek the
`(sourceType, sourceId)` unique index; `sourceType IS NOT NULL AND sourceId IN (...)` could not.

**Later, additive migration (not in Phase 1):** the window predicates above are timestamp ranges on the
SOURCE tables (`ProjectUpdate.createdAt`, `ProjectTask.completedAt|updatedAt`, `Opportunity.updatedAt`, ...), none
of which is indexed, so every cycle scans those tables. Fine at MVP sizes; add per-table timestamp indexes
(DBML first, additive, `CREATE INDEX CONCURRENTLY` on prod) when a cycle's read time shows up in the logs.

## Cycle, sweep, backfill

- **Cycle (~5 s):** sources in the window -> insert missing envelopes; re-judge the envelope each source
  already has (`Retain`): delete it, or move it to the source's current audience. An event made
  INVITE_ONLY loses its card on the next cycle, not the next sweep.
- **Moved targets:** the judge also compares the stored card's targets with `FeedProjectionRules.TargetsOf(source)`
  (the one function `Enter` builds targets from, so they cannot disagree). Targets decide who may see a card - a
  DRAFT project hides its cards - so a card still pointing at the old rows is wrong about its audience: it is
  removed and re-inserted pointing at the new ones, in one pass (removals apply before inserts, so the
  replacement never collides with the card it replaces). If the source no longer earns entry (`Enter` is null,
  e.g. a closed opportunity that moved), the card is only removed - privacy over history. No service moves an
  event or opportunity today; this guards operator repairs and any later "move" feature.
- **Sweep (hourly, full table):** a deleted source leaves no row to read, so the sweep starts from the
  envelope side and reconciles EVERY envelope, whatever its age, against its current source - a 48 h
  "recent" filter used to gate this (a security review's finding: a source that outlived the window and
  only later lost its target or its eligibility - a deleted comment, an opportunity re-drafted, an event
  made INVITE_ONLY - never got reconsidered). The scan reads envelope ids in `Feed:Projector:SweepBatchSize`
  batches (default 500, keyset-ordered by `id`, kept in memory so the batch boundary needs no translatable
  SQL ordering on a string column); each batch's wide read, source lookup and eventual delete stays bounded
  to that size, however large the table. Plus ANY envelope (any age) whose defining target FK is NULL
  (`HasLostItsTarget`; the FKs are SET NULL) is still found in one pass, unbatched, same as before.
  **Lock:** the single session advisory lock (`FeedEnvelopeStore.RunnerLockKey`) stays held for the whole
  sweep, batches included, rather than being released and re-acquired between batches or capped to a few
  batches per tick behind a persisted cursor. Advisory locks are cooperative - they block only another
  caller of `pg_try_advisory_lock` on this exact key, never ordinary table reads or writes - and the only
  other callers are the hosted service's own next tick (already serialized; it cannot race itself) and the
  admin backfill endpoint, which already answers a losing `TryAcquireRunnerLockAsync` with 409. A
  per-tick-capped cursor was rejected: `FeedProjectorCycle` is registered `Scoped` (a fresh instance per
  tick, see `FeedProjectorRegistration`), so a cursor would need new persisted state, and capping the work
  per tick would mean a given hourly sweep no longer fully reconciles the table - it would just move the
  staleness window from "> 48 h" to "> table_size / batch_size hours", not close it.
- **Backfill:** the same pass with no window (`BackfillAsync`), via `POST /api/admin/feed/backfill`.
  `dryRun` defaults to TRUE; the dry run plans without the lock and returns what it would write.
- **Single runner:** a Postgres session advisory lock (`FeedEnvelopeStore.RunnerLockKey`). A pass that
  cannot take it returns `RunnerBusy` and writes nothing; the admin endpoint answers 409.
- **Idempotency:** a pre-read of existing keys, then one insert per envelope; a 23505 on
  `(sourceType, sourceId)` is `AlreadyProjected`, never an error. A 23503 (author or target deleted
  mid-pass) is `TargetGone` and skipped.

## Configuration

`Feed:Projector:*` (env `Feed__Projector__Enabled`, ...), read by `FeedProjectorOptions.FromConfiguration`:
`Enabled` is OFF unless set `true`, in EVERY environment including Development (review H1: the dev compose runs
`ASPNETCORE_ENVIRONMENT=Development` with `env_file: .env`, whose `DATABASE_URL` points at the shared Railway
database, so a Development default turned any laptop into the shared database's feed writer - and the first
cycle sweeps, i.e. hard-deletes). The local stack (`.omc/scripts/local-stack.ps1`, local DB on 5433) sets it
explicitly. `IntervalSeconds` (5), `LookBackMinutes` (10), `SweepIntervalMinutes` (60), `SweepBatchSize` (500,
the sweep's full-table reconciliation batch; see §Cycle, sweep, backfill).

**A malformed value never stops the API** (owner decision, review M5): `AddFeedProjector` uses
`TryFromConfiguration`; on a bad value the projector stays OFF (whatever `Enabled` says - a half-read
configuration is not one to run) and `FeedProjectorMisconfiguredAlarm` logs the reason at Critical when the host
starts. Payments and sign-in live in the same process, so a feed typo must not take them down. The cycle and
stores are registered regardless, so the admin backfill works with the timer off.

## Phase 2 handoff (not built here)

**PRECONDITION for Phase 2 (review M2, deferred from Phase 1):** retraction is currently a HARD delete. An
opportunity sent back to DRAFT, or an event moved to DRAFT / CANCELLED / INVITE_ONLY, makes `Retain` return null and
`FeedEnvelopeStore.RemoveAsync` deletes the card WITH other people's replies, likes, bookmarks and shares; re-opening
it later projects a FRESH card (new id, zero engagement, new `updatedAt`) that ranking would treat as new and bump.
Before the read API ships, retraction must become a state, not a delete: an additive nullable column on `Post`
(e.g. `retractedAt`, DBML first) that the projector sets instead of deleting and clears on re-entry, restoring the
SAME row; every feed/hydration read filters it out. Deletion stays only for a source that is actually gone.

Ranking (`trendingScore`, Reasons[], diversity caps), the feed read API, and HYDRATION: envelopes store a
title snapshot and, for system kinds, a plain sentence (`FeedEnvelopeCopy`, vocabulary-checked in
`FeedProjectionRulesTests`); user-written bodies (updates, comments) are stored as "" and must be read live
from the source row.

**Defence in depth, required, not optional:** the sweep now reconciles the whole table every hour (see
§Cycle, sweep, backfill), but it is still a background pass with an hour's cadence and a batch loop that
can be mid-flight when a reader hits the card. Hydration MUST independently check that the source row it is
about to read live still exists (and still earns the card - the same `Retain` question) and drop the card
from the response, in-line, if it does not; it cannot assume the sweep already caught a since-deleted or
since-ineligible source. This is the second of two checks on the same invariant, not a substitute for the
first - it is what keeps a reader from ever seeing a stale or revoked card during the window between a
source changing and the next sweep (or batch) reaching that envelope.

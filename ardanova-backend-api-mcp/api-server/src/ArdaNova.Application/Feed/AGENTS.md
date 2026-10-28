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
| ProjectMilestone | MILESTONE | PUBLIC | project creator | `completedAt` set | deleted / `completedAt` cleared |
| ProjectTask | TASK_COMPLETED | PUBLIC | project creator | `completedAt` set | deleted / reopened |
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
- **TASK_COMPLETED is attributed to the project creator**, not the assignee, so the feed does not publish
  who was assigned a task. Revisit with the card renderer if credit to the worker is wanted.

## Windows (`Infrastructure/Feed/FeedSourceReader`)

Each kind is read by every timestamp that can change its card, so a change is judged within one cycle:
Project `publishedAt|updatedAt`; milestone/task `completedAt|updatedAt` (CompleteAsync does not bump
`updatedAt`); funding, opportunity, event `updatedAt`; opportunity updates/comments their own `createdAt`
OR the opportunity's `updatedAt` (opening or re-drafting it changes whether they may show); the rest
`createdAt`/`joinedAt`. The look-back (10 min) must cover at least two cycles so a slow commit is seen.

## Cycle, sweep, backfill

- **Cycle (~5 s):** sources in the look-back -> insert missing envelopes; re-judge the envelope each source
  already has (`Retain`): delete it, or move it to the source's current audience. An event made
  INVITE_ONLY loses its card on the next cycle, not the next sweep.
- **Sweep (hourly, 48 h):** a deleted source leaves no row to read, so the sweep starts from the envelope
  side: envelopes created in the last 48 h whose source row is gone or no longer earns a card, plus ANY
  envelope (any age) whose defining target FK is NULL (`HasLostItsTarget`; the FKs are SET NULL). Older
  cards with a deleted, FK-less source (e.g. an update deleted a month later) are left for Phase 2
  hydration to drop at read time, or for a backfill.
- **Backfill:** the same pass with no window (`BackfillAsync`), via `POST /api/admin/feed/backfill`.
  `dryRun` defaults to TRUE; the dry run plans without the lock and returns what it would write.
- **Single runner:** a Postgres session advisory lock (`FeedEnvelopeStore.RunnerLockKey`). A pass that
  cannot take it returns `RunnerBusy` and writes nothing; the admin endpoint answers 409.
- **Idempotency:** a pre-read of existing keys, then one insert per envelope; a 23505 on
  `(sourceType, sourceId)` is `AlreadyProjected`, never an error. A 23503 (author or target deleted
  mid-pass) is `TargetGone` and skipped.

## Configuration

`Feed:Projector:*` (env `Feed__Projector__Enabled`, ...), read by `FeedProjectorOptions.FromConfiguration`:
`Enabled` blank -> ON in Development, OFF elsewhere (Staging/Production must set it); `IntervalSeconds`
(5), `LookBackMinutes` (10), `SweepIntervalMinutes` (60), `SweepWindowHours` (48). A malformed value is a
startup failure. The cycle and stores are registered regardless, so the admin backfill works with the
timer off.

## Phase 2 handoff (not built here)

Ranking (`trendingScore`, Reasons[], diversity caps), the feed read API, and HYDRATION: envelopes store a
title snapshot and, for system kinds, a plain sentence (`FeedEnvelopeCopy`, vocabulary-checked in
`FeedProjectionRulesTests`); user-written bodies (updates, comments) are stored as "" and must be read live
from the source row, which is also where a card whose source has since vanished should be dropped.

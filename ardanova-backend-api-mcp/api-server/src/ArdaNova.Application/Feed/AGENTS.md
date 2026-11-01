# Feed: projector (SR-03 Phase 1) and ranked read side (Phase 2)

Every feedable event becomes ONE envelope row in `Post`: `type` (PostType), provenance
`(sourceType, sourceId)` (UNIQUE, CHECK both-or-neither), and nullable target FKs
`projectId / guildId / opportunityId / eventId` (ON DELETE SET NULL). A post a person wrote keeps
`sourceType` NULL. The read side (Phase 2: retraction, hashtags, quality score, ranker, paging, cards) is
described from §Retraction on; the projector sections below it are Phase 1 unless marked.

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

"Leaves when" splits in two since Phase 2 (§Retraction): a source that is **deleted** loses its card (hard
delete, with its engagement); a source that still exists but stops earning one (DRAFT, CANCELLED, INVITE_ONLY,
`completedAt` cleared) has its card **retracted** - kept, hidden from every read, restored on re-entry.

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
  already has (`Retain`): retract it, restore it, move it to the source's current audience, or re-tag it from
  the source's edited copy. An event made INVITE_ONLY loses its card on the next cycle, not the next sweep.
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
the sweep's full-table reconciliation batch; see §Cycle, sweep, backfill), `ScoreIntervalMinutes` (5, the quality
pass; see §Quality score).

**A malformed value never stops the API** (owner decision, review M5): `AddFeedProjector` uses
`TryFromConfiguration`; on a bad value the projector stays OFF (whatever `Enabled` says - a half-read
configuration is not one to run) and `FeedProjectorMisconfiguredAlarm` logs the reason at Critical when the host
starts. Payments and sign-in live in the same process, so a feed typo must not take them down. The cycle and
stores are registered regardless, so the admin backfill works with the timer off.

## Retraction (Phase 2, precondition SR-03-P2-PRE)

Withdrawal is a state, not a delete. `Post.retractedAt` (migration `20261031000000_feed_retraction_and_hashtags`):

- `FeedProjectorCycle.Plan.Judge` order matters: **moved targets first** (remove + re-insert, unchanged from
  Phase 1 - a card re-homed to a different audience must not carry replies written for the old one), **then**
  `Retain(source) == null` -> retract (only if not already retracted, so a steady state writes nothing), **then**
  a retracted card whose source earns one again -> restore (`RestoreAsync`: `retractedAt = NULL` and the current
  audience, one statement). `createdAt` is never touched, so a restored card is not bumped as new. Retain, not
  Enter, decides restore: Retain is "may a stored card stay", and a retracted card is a stored card.
- **Hard delete only when the source row is gone** (sweep: `ReadByIdsAsync` misses it, or `HasLostItsTarget`).
- **Reads:** `PostVisibilityPolicy.VisibleTo` (Domain) carries `retractedAt == null` in both its branches, and every
  read seam composes it (`PostReadFilters.ReadableBy`), so detail, comments, like, reply, share and bookmark all
  answer a retracted card with the same NotFound as a missing post - author included. `IsPubliclyVisible` (the
  legacy viewer-less reads) is compiled from the same expression.

## Hashtags (owner direction 2026-10-31: light, copy-driven metadata)

`Post.hashtags text[] NOT NULL DEFAULT '{}'` + GIN index. `Hashtags.Extract`: NFKC, then
`(?<![\p{L}\p{N}\p{M}_/&#-])#([\p{L}\p{N}\p{M}_-]{2,40})(?![\p{L}\p{N}\p{M}_-])`, lowercased, ends trimmed of `-`/`_`,
deduped, first-seen order, at most 10. The look-behind is what keeps `example.com/#anchor`, `page#frag`, `&#39;` and
`##x` out; a markdown heading (`# Title`) never matches because a space is not a tag character. `\p{M}` is added to
the owner's class so Devanagari/Thai words are not cut at a vowel sign; NFKC has already composed every mark that
can be. A run longer than 40 is not truncated into a tag - it is not a tag.

- **Written by:** `PostService.CreateAsync/UpdateAsync` (title + content) and the projector
  (`FeedProjectionRules.HashtagsOf(source)` over `FeedSource.Copy`: project title/summary/description, update
  title/content, comment content, opportunity/event/guild descriptions, milestone/task title+description). `Judge`
  re-tags a stored card when its source's copy changed. Copy edits to sources without `updatedAt`
  (ProjectUpdate, GuildUpdate, comments) are seen by the hourly sweep, not the 5 s window.
- **Canonical layer, not a taxonomy** (`FeedTopicCanon`): a tag resolves to an existing `TaxonomyTerm` by slug or
  label in any of `_`/`-`/joined spellings (`#food-and-drink`, `#foodbeverage` -> SECTOR FOOD_BEVERAGE); on a shared
  spelling SECTOR > SKILL > PROJECT_TYPE (RESEARCH is both a sector and a project type). A SKILL also stands for its
  parent SECTOR. Anything else stays a free-form topic (`tag:x`) and still matches itself. **No aliases table** -
  revisit when two spellings of one subject both trend (e.g. `#permaculture` and `#permaculture-design` each in the
  top tags of a week); then add `TaxonomyTerm` rows or an alias column, not code.
- **Known follow-up, no change yet: confusable scripts.** NFKC does not fold look-alikes across scripts, so `#сafe`
  with a Cyrillic `с` is a different tag from `#cafe`. Harmless for privacy (a tag scope still re-applies visibility)
  but it can split or spoof a topic. Trigger: a spoofed tag reported, or a mixed-script tag in the weekly top tags;
  then reject mixed-script tags (Unicode TR39 "single script" check) at extraction.

## Quality score

Stored in `Post.trendingScore` (`trendingAt` = when), viewer-independent, recomputed by `FeedQualityPass` every
`ScoreIntervalMinutes` under the projector's runner lock (same advisory-lock pattern; the hosted loop runs it after
the cycle in its own scope) and after an executed backfill.

**Capped-reply log-engagement score:** `Q = 1 + ln(1 + A + 0.5*min(R, A) + 2*S + 1.5*B)`, where every signal
counts **distinct accounts other than the post's author**, read from the rows (`FeedReadStore.ReadQualitySignalsAsync`):
A = people who liked it, R = people who replied (on the card, or in a DISCUSSION card's native thread - one person
counts once across both), S = people who shared it, B = people who bookmarked it. Why each term:

- **Distinct non-author people, never counters** (review HIGH-1). `ShareAsync` adds a row and bumps `sharesCount` on
  every call (no unique index), and an author can like, reply to and bookmark their own post. Counting rows or counters
  would let one account share its own post fifty times and climb; counting distinct others means the most one person
  can contribute to anyone's post is one of each signal, and nothing to their own. In particular the author's
  self-like never unlocks `min(R, A)` for their own replies. A unique `(postId, userId)` on `PostShare` is NOT added:
  sharing to two different projects is a legitimate second row, and live data may already hold duplicates (a cleanup
  step this additive migration does not take); the distinct count makes it unnecessary for ranking.
- **Replies capped by approvals.** Reply volume is what a flame war produces; approvals are what a good thread
  earns. `min(R, A)` means a reply only counts while someone has vouched for the post, and at half weight.
- **Shares (2) > bookmarks (1.5) > likes (1).** Passing a card on and saving it for later are deliberate, costly
  endorsements; a like is cheap. All are explicit, positive, viewer-initiated acts.
- **ln damping + a floor of 1.** A runaway card cannot bury everything else for days, and a fresh card with no
  engagement still ranks on recency and affinity (an unscored row - stored 0 - also ranks at 1).
- **FORBIDDEN, by construction:** views, dwell time and negative reactions. `FeedQualitySignals` has no field for
  them, so no caller can feed them in (`viewsCount` is still counted by `PostService`; nothing ranks on it).

- **Time-free** (review L2). Novelty fades with time, so it lives in the ranker (read time), not in Q: a stored
  score with a time term would change for every recent post every pass, rewriting rows on an indexed column
  (`trendingScore`), which defeats Postgres HOT updates. Q changes only when engagement does.

The pass reads every live post id, then signals in `SweepBatchSize` batches, and writes only rows whose score moved.

## Ranking (`FeedRanking`, pure; table-driven `FeedRankingTests`)

`score = max(Q, 1) x decay x affinity x novelty`:

- **decay** = `0.5^(age / 48h)` against the cursor's pinned `asOf` (a card dated after asOf counts as new).
- **affinity** = 1 + project link (team or follow, +2) + guild link (+1.5) + followed author (+1) + one interest
  match, only when there is no project/guild link: a specific tag topic (skill / project type / free tag, +0.75) >
  a sector reached through a tag (+0.5) > a sector reached through the card's project (+0.5). The viewer's
  interests: tags on the posts behind their latest 200 engagements - written, liked or replied to, tagged or not
  (three index range reads on `Post(authorId, createdAt)`, `PostLike(userId, createdAt)`,
  `PostComment(authorId, createdAt)`, merged by when they engaged; review MED-2/MED-B, see Infrastructure/Feed/AGENTS.md);
  sectors of projects they follow, created or work on; their `UserSkill` names - all through the canon.
  Own posts carry "Your activity", no boost.
- **novelty** = 1 + 0.5 x freshness for every reader when the card's project is new, + 0.5 x freshness more when it
  is in one of the viewer's sectors ("New in Construction" for people in construction); freshness = 1 the day the
  project opened, fading linearly to 0 over 14 days (`FeedRanking.Freshness`).
- **Diversity per page** (`SelectPage`, greedy in rank order): home <= 2 per subject (project, else guild,
  opportunity, event, else author) and <= 3 per author; every 5th slot prefers a **discovery** card - no follow,
  team or author link and no interest match (tag or sector). A discovery slot with no discovery card left is filled
  by the next best card, never left empty. **Caps relax only when the feed is exhausted** (nothing visible left below
  the window, review MED-1). A window at its 150-card cap that still cannot fill a page under the caps - one very
  busy subject - returns a SHORT page instead; the next page continues the same window, so the scroll still ends
  only when everything visible has been served.
  - **Client contract (accepted, review MED-C):** keep fetching while `nextCursor` is non-null, even when a page is
    short - or holds 2 cards. A short page means "the caps held", never "the end"; only `nextCursor: null` ends the
    scroll. One busy subject therefore yields short capped pages until its window is spent.
  - **Revisit trigger:** a subject regularly posts more than ~150 cards per 48 h (one window's worth per half-life);
    then add a diversity probe below `Floor` (read a few cards from older, other subjects to fill the short page)
    rather than relaxing the caps.

## Paging (`FeedService`, `FeedCursor`)

Per scope (owner addenda 2-3):

| scope | order | caps | discovery |
|---|---|---|---|
| home | ranked windows | 2/subject, 3/author | every 5th slot |
| `tag:{tag}` | ranked windows | 3/author only | none |
| `project:{id}`, `guild:{id}` | newest first, keyset `(createdAt, id)` | none | none |

**One `PolicyFor` arm per row (Phase 3, Q1).** Phase 2 shipped two arms (home / everything else), so project and
guild shared the tag's author cap; it never bit only because the recency path did not read the policy. Now both
paging modes read it: `RecencyPageAsync` places each batch through `FeedRanking.SelectPage` with its scope's arm. With
no caps that keeps every row in order; a cap added to the project/guild arm would thin a one-writer project AND skip the
thinned rows (the keyset moves past the whole batch) - `AProjectsOwnFeed_ServesAllSixOfOneWritersPosts_...` fails
first.

A tag naming a SECTOR term also matches cards whose project carries that sector (untagged updates on a
CONSTRUCTION project are `#construction`); visibility is re-applied identically in every mode.

**Ranked windows.** Ranking all candidates per request is unbounded; ranking a fixed recency window is not. The
cursor pins `asOf` (so decay cannot reshuffle) and an open window `[Floor, Top)` in `(createdAt desc, id desc)`
keyset order, plus 48-bit fingerprints of the window's cards already served. Each page re-reads the window under the
viewer's visibility, ranks what is unserved, fills a page; a page that cannot fill under the caps opens the next
60-card chunk (window cap 150; a re-read takes at most 210 rows). When a window is spent, `Top` drops to `Floor` and
the served set empties, so the cursor does not grow with scroll depth: at most ~2.5-3.4 KB, bounded by
`FeedCursor.MaxServed` (256 six-byte fingerprints, base64 inside base64url JSON) and the 4096-character decode cap.
An issued cursor holds at most 210 fingerprints; only a hand-made one can exceed 256, and then the served set is cut
back to the fingerprints of rows still in the window (`IntersectWith`), so the next cursor is again decodable and
nothing unserved is skipped (exercised by the forged-cursor case in `FeedReadFlowTests`). **No "caught up" end:** after the followed cards in a
window come its discovery cards, then the next (older) window; `nextCursor` is null only when nothing visible is left.
Trade-off: ranking is exact within a window; across windows recency wins, which a 48 h half-life makes close to
the global order unless more than ~150 cards land per half-life (then a followed card older than the window waits for
its window). Trigger to revisit: a materialised per-viewer candidate list once volume crosses that.

**Two accepted skip cases** (review L1). A card is never shown twice and never shown to someone who may not see it;
in exactly these two cases a visible card can be skipped for the rest of one scroll (a fresh scroll shows it):

1. **More than 210 visible rows inside an open window.** The window's re-read is capped at 210 rows. Rows can join an
   open window after it was sized - the viewer joins a busy project mid-scroll (its members-only cards become
   visible), or the projector inserts envelopes dated inside the window (a backfill). Rows past the 210th are not
   read, so when the window is spent they fall behind `Top` unserved.
2. **A transient hydration failure.** A card whose source kind cannot be read for one page (`CurrentSourcesAsync`
   logs and fails closed) is treated like a vanished card: fingerprinted as served so the page can refill and the
   cursor move on. If the read recovers, that card is not offered again in this scroll.

Both trade a rare skip for never stalling or repeating; neither can reveal anything.

**A cursor can only skip, never reveal.** It is unsigned on purpose: every read composes `PostReadFilters.ReadableBy`
(audience + draft project + retraction) and the scope filter; a forged window just selects a range of what the viewer
may see, a forged served set hides cards, a future `asOf` is clamped to now, and a cursor whose scope or mode differs
from the request is refused (400). A scoped project the viewer cannot see answers 404 exactly like a missing one.

## Cards (`FeedCardDto`) and defence in depth

Hydration re-reads the page's posts through the read gate, then for every envelope reads its source
(`IFeedSourceReader.ReadByIdsAsync`) and drops the card if the source is gone, `Retain(source)` differs from the
stored audience (fail closed: the next cycle will move it), or `TargetsOf(source)` differs - the second of two checks
on the projector's invariant, so a reader never sees a card the sweep has not reached yet. Dropped cards are skipped
and the page refilled; the scroll does not end. A source kind whose read throws drops its cards for that page only.

- **Body**: a person's post shows its content; an envelope shows its source's live `Body` (updates, comments) when a
  person wrote it, else the envelope's system sentence (`BodyIsUserWritten` says which).
- **Funding**: `Funding.Percent` from the envelope's `{"percent":N}` - never an amount, never a backer; the project
  summary on a card carries no funding fields.
- **Opportunity** (`FeedLinkDto`): `id`, `title` and `slug` - the opportunity page resolves by slug, so an id-only
  Apply link 404s (Phase 3). Every card with an opportunity carries it (OPPORTUNITY_* and an opportunity's DISCUSSION).
- **Viewer** (`isMember`, `isFollowing`, `canClaim`): computed server-side; Phase 3 picks the primary action from
  it, never from reason text. `canClaim` is true only on a HELP_WANTED card the viewer may pick up in one tap
  (§Help wanted).
- **Counters** (`Counters.Likes/Replies/Shares`) are the RAW post counters, for display only. Ranking ignores them
  (distinct non-author people, §Quality score), but a card its author shared 50 times DISPLAYS 50 shares. Phase 3
  must not present them as a quality signal; see §Deferred for the fix trigger.
- **Reasons**: stable `kind` + rendered `text` with live names (`FeedReasonText`).

## Help wanted (Phase 3, lane K2)

A HELP_WANTED card is a **person's post**, not an envelope: `sourceType` NULL, `taskId` set (unique, CHECK HELP_WANTED
only; written by `PostService`, lane K1). Every read path takes it exactly like a POST - candidates, ranking, caps,
scopes, the tag and sector match, the quality signals (type-agnostic) and the body (the asker's own words). Hydration
adds one batched read, `IFeedReadStore.ReadHelpTasksAsync` (task row + assignee name + `EXISTS TaskCommerceAgreement`),
and the card's `task` and `viewer.canClaim` come from it.

**Dropped at hydration, page refilled** (the same path as a vanished envelope; the scroll goes on):

- `taskId` is NULL - the task was deleted (`Post_taskId_fkey` is ON DELETE SET NULL, so the asker's words survive);
- the task row is missing, or its read threw (fail closed, this page only);
- the task is not on the card's own project. The read gate has already checked the post's audience and THAT project's
  visibility, so requiring the task there is what keeps a task's assignee from reaching anyone who may not see its
  project: the card is never more visible than its source.

The whole rule lives in one place, `FeedService.HelpTaskOf`, at hydration. Hydration must check anyway (a task can be
deleted between the window read and the hydrate), so a second, SQL copy of the orphan filter in the candidate query
would be a second statement of the rule to drift. Cost: an orphaned ask is ranked and dropped once per scroll (it is
fingerprinted as served, so never twice in one). Trigger to revisit: orphaned asks in the hundreds - then add
`type <> HELP_WANTED OR taskId IS NOT NULL` to `Visible`, or have the task delete path remove its ask.

| task facts | `task.state` | `assigneeName` |
|---|---|---|
| `status = COMPLETED` | `DONE` | assignee's name, if any |
| assigned, not COMPLETED | `PICKED_UP` | assignee's name |
| unassigned, not COMPLETED | `OPEN` | null |

`task.paid` = `equityReward > 0`.

**`canClaim` = `TaskClaimRule.CanClaimInPlace(facts, viewer ∈ MemberProjects(task.projectId))`** - the one rule the
claim write (K1's conditional UPDATE) also enforces, so a card never offers "Pick up task" on a task the claim then
refuses (see `Application/Tasks/AGENTS.md`). Membership is the TASK's project (creator or ProjectMember, as
`TasksController.Assign` checks it), never a guild. A paid task is never claimable in place: the card routes to the
task page, whose readiness check runs before the claim. Reasons still tell a teammate (`ProjectTeam`, "Because you're
on the P team") from a follower (`FollowedProject`, "Because you follow P"); the client renders its own fragment
from the kind.

## Deferred (with the trigger to revisit)

- **Project follow has no write path** (only guild and user follows have services). The feed reads `ProjectFollow`;
  until an endpoint writes it, project affinity comes from team membership. Trigger: Phase 3 "Follow" button.
- **Full-pass quality scoring** reads every live post each interval (writes only scores that moved). Trigger:
  > ~50k posts or a pass > 10 s in the logs - then score only posts with engagement rows since the last pass.
- **Card counters are raw** (`likesCount`, `sharesCount` shown on the card). Ranking ignores them (distinct people
  only), but a self-shared card still displays an inflated share count. Trigger: Phase 3 shows share counts; then
  show distinct sharers or dedupe `ShareAsync` per (post, person, destination).
- **Tag scope with a sector match** ORs a join condition with the GIN containment, so Postgres cannot use the index
  alone. Trigger: tag-scope latency in the logs; then denormalise the project's sectors onto envelopes' hashtags.

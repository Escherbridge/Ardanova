# Feed projector persistence

The rules and the cycle live in `Application/Feed` (see its AGENTS.md). This directory is mechanics only.

- **`FeedSourceReader`** projects each source table into its `FeedSource` record, AsNoTracking, filtered
  either by a "changed since" window or by row ids (the sweep). It reads ONE kind per call, so the cycle can
  isolate a kind whose read fails, and it decides nothing about eligibility or credit (it hands the rules the
  task's `assignedToId` / milestone's `assigneeId` and the project creator; `FeedProjectionRules` picks).
- **`FeedEnvelopeStore`**
  - `TryAcquireRunnerLockAsync`: `pg_try_advisory_lock(RunnerLockKey)` is a SESSION lock, so the store
    opens the DbContext connection itself and keeps it open until the handle is disposed; the cycle's
    reads and writes in the same scope ride that connection. Disposal unlocks, then closes. A crashed
    process drops its connection and Postgres releases the lock.
  - **Disposal never throws.** It runs while an exception may be unwinding - typically the host's shutdown
    cancellation, during which Npgsql may break the connection to cancel the in-flight command. A throwing
    `pg_advisory_unlock` would then REPLACE the `OperationCanceledException` and the hosted service would log a
    spurious failure instead of stopping cleanly. So an unlock (or close) failure is logged at Warning and
    swallowed: the server frees a session lock when its connection dies. If the unlock failed on a connection
    that is still alive, the connection goes back to Npgsql's pool and the lock lingers until that connector is
    reused (Npgsql resets a pooled connection with `DISCARD ALL`, which includes `pg_advisory_unlock_all()`) or
    pruned; meanwhile other runners just see `RunnerBusy` - delayed, never doubled.
  - **Requires a SESSION-mode connection.** The lock lives on one server session for the length of a pass. Behind
    a transaction-pooling proxy (PgBouncer `pool_mode=transaction`, Supabase's 6543 pooler, ...) consecutive
    statements can land on different server sessions: the lock would be taken on one and the unlock sent to
    another, so a second runner could take the "same" lock while the first still runs, and the unlock silently
    does nothing. This assumes the deployed `DATABASE_URL` reaches Postgres directly (Railway's Postgres service,
    no pooler in front), i.e. session semantics. If
    `DATABASE_URL` ever moves behind a transaction pooler, point the API at a session-mode port or replace the
    session lock with a transaction-scoped `pg_try_advisory_xact_lock` per write batch.
  - `InsertAsync`: EF insert of one envelope; 23505 on `(sourceType, sourceId)` -> `AlreadyProjected`,
    23503 -> `TargetGone`. The entity is detached either way so a failure cannot poison the next insert
    (pinned by `FeedProjectorFlowTests.TheEnvelopeStore_StaysUsable_AfterAnInsertThatCollidesOnTheUniqueIndex`).
    Any other failure propagates to the cycle, which counts it as one failed card and carries on.
  - `RemoveAsync`: one transaction - null out reply parents, then delete the envelope's comments, likes,
    bookmarks, shares and media (all RESTRICT FKs to Post), then the envelope. Every statement is scoped
    by `sourceType IS NOT NULL`, so no id can make it delete a person's post. Since Phase 2 it runs only for an
    envelope whose source row is gone.
  - `RetractAsync` / `RestoreAsync` / `SetHashtagsAsync`: single `ExecuteUpdate` statements, each scoped by
    `sourceType IS NOT NULL` like `RemoveAsync`; retract/restore also guard on the current `retractedAt` so a
    replayed plan writes nothing. None touches `createdAt`. `SetQualityAsync` writes `trendingScore`/`trendingAt`
    on any post (person posts are ranked too) and leaves `updatedAt` alone: a score is derived state, not an edit.
- **`FeedSourceReader`** also fills `FeedSource.Copy` (what hashtags are read from) and `Body` (a person's words the
  card shows: update/comment content). Nullable columns are coalesced in the projection (`summary ?? ""`) because SQL
  `NULL || text` is NULL.
- **`FeedReadStore`** (`IFeedReadStore`): the feed read side's non-post reads - the viewer's follows, memberships,
  engaged tags, project sectors and skills; names for cards and reasons; active taxonomy terms; native-thread reply
  counts; and the quality pass's signal batches. Data access only: every visibility rule is composed in
  `Application/Feed/FeedService`.
  - Engaged tags: three index range reads - `Post(authorId, createdAt)`, `PostLike(userId, createdAt)`,
    `PostComment(authorId, createdAt)` (the last two come with migration `20261031000000`) - each the viewer's latest
    200 engagements, merged in memory by when they engaged, top 200, then one read of those posts' tags. The seeks
    carry NO hashtag filter: a join to `Post` in the seek makes Postgres fetch and sort the viewer's whole history
    before the LIMIT. So interests come from the latest 200 engagements, tagged or not (untagged ones add nothing).
    Never one `authorId = v OR EXISTS(like) OR EXISTS(reply)` query: that cannot seek and scans `Post` per page.
  - Quality signals: `COUNT(DISTINCT userId)` per post over the like / share / bookmark ROWS, joined to `Post` to drop
    the author's own; repliers as distinct (post, person) pairs across the card and a DISCUSSION card's native
    thread, author excluded in memory. The raw counters are never read (Application/Feed/AGENTS.md §Quality score).
    Every grouping is by `postId`, served by `PostLike(postId, userId)` / `PostBookmark(postId, userId)` (unique) and
    `PostShare(postId)` / `PostComment(postId)` (migration `20261031000000`) - without the last two each 500-post
    batch of every pass scanned both tables.
- **`FeedProjectorHostedService`** runs one cycle per fresh scope on `Feed:Projector:IntervalSeconds`;
  the first cycle after start sweeps, then hourly. After the cycle, when `ScoreIntervalMinutes` has elapsed, it runs
  `FeedQualityPass` in its own scope (its own connection, so it takes the runner lock the cycle just released). It owns the in-memory `FeedProjectorWatermark` (the cycle is
  Scoped and cannot), which is why a restart forgets it - see Application/Feed/AGENTS.md §Windows.
  `AddFeedProjector` registers the timer only when `Feed:Projector:Enabled` is explicitly true; a malformed
  setting registers `FeedProjectorMisconfiguredAlarm` instead (Critical log at host start, timer off, API up).

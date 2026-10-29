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
    by `sourceType IS NOT NULL`, so no id can make it delete a person's post.
- **`FeedProjectorHostedService`** runs one cycle per fresh scope on `Feed:Projector:IntervalSeconds`;
  the first cycle after start sweeps, then hourly. It owns the in-memory `FeedProjectorWatermark` (the cycle is
  Scoped and cannot), which is why a restart forgets it - see Application/Feed/AGENTS.md §Windows.
  `AddFeedProjector` registers the timer only when `Feed:Projector:Enabled` is explicitly true; a malformed
  setting registers `FeedProjectorMisconfiguredAlarm` instead (Critical log at host start, timer off, API up).

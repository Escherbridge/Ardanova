# Feed projector persistence

The rules and the cycle live in `Application/Feed` (see its AGENTS.md). This directory is mechanics only.

- **`FeedSourceReader`** projects each source table into its `FeedSource` record, AsNoTracking, filtered
  either by a "changed since" window or by row ids (the sweep). It decides nothing about eligibility.
- **`FeedEnvelopeStore`**
  - `TryAcquireRunnerLockAsync`: `pg_try_advisory_lock(RunnerLockKey)` is a SESSION lock, so the store
    opens the DbContext connection itself and keeps it open until the handle is disposed; the cycle's
    reads and writes in the same scope ride that connection. Disposal unlocks, then closes. A crashed
    process drops its connection and Postgres releases the lock.
  - `InsertAsync`: EF insert of one envelope; 23505 on `(sourceType, sourceId)` -> `AlreadyProjected`,
    23503 -> `TargetGone`. The entity is detached either way so a failure cannot poison the next insert.
  - `RemoveAsync`: one transaction - null out reply parents, then delete the envelope's comments, likes,
    bookmarks, shares and media (all RESTRICT FKs to Post), then the envelope. Every statement is scoped
    by `sourceType IS NOT NULL`, so no id can make it delete a person's post.
- **`FeedProjectorHostedService`** runs one cycle per fresh scope on `Feed:Projector:IntervalSeconds`;
  the first cycle after start sweeps, then hourly. `AddFeedProjector` registers the timer only when the
  options resolve Enabled.

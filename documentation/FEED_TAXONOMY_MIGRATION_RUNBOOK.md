# Feed envelope and taxonomy migration runbook (SR-03 phase 0, 2026-10-27)

Four Prisma migrations under `ardanova-client/prisma/migrations`, landing after
`20261018120000_guild_review_one_per_reviewer` (see
`GOVERNANCE_GUILD_FINANCE_MIGRATION_RUNBOOK.md` for that batch and its
preconditions, which apply here unchanged: non-production rehearsal first,
read-only preflight identity, `migrate deploy` only, never `db push` or
`migrate dev` against shared data). Each file's header is the source of truth.

**The operator runs every command here. The authoring lane only rehearsed on a
throwaway local PostgreSQL 16 database; it did not touch any hosted database.**

| # | Migration | Kind | What it does |
| - | --- | --- | --- |
| 1 | `20261027000000_feed_enum_values` | Additive enum labels | `ALTER TYPE ... ADD VALUE IF NOT EXISTS`: 9 `PostType` envelope kinds, 6 `ProjectType` kinds. Labels only, because a new label cannot be used in the transaction that adds it. |
| 2 | `20261027001000_feed_envelope_columns` | Additive schema | Creates `FeedSourceType`; adds nullable `Post.sourceType/sourceId/opportunityId/eventId`, a `CHECK` that the pair is both-or-neither, two `ON DELETE SET NULL` foreign keys, `UNIQUE (sourceType, sourceId)` and seven read indexes. Existing rows read as authored posts (NULL source). |
| 3 | `20261027002000_post_engagement_uniqueness` | **Destructive** + constraint | Deletes duplicate `PostLike`/`PostBookmark` rows per `(postId, userId)`, keeping the earliest; recounts `Post.likesCount` from the surviving `PostLike` rows for each affected post (not a rows-removed decrement -- that undercounts when two different people's likes raced); adds the two unique indexes. |
| 4 | `20261027003000_taxonomy_term` | Additive schema + seed | Creates `TaxonomyKind` and `TaxonomyTerm`, seeds 26 sectors (the 10 legacy category codes verbatim), 13 project types and 54 skills, `ON CONFLICT (kind, slug) DO NOTHING`. |

## Deploy order

0. **Read-only preflight on live**, before touching anything:
   ```sql
   SELECT migration_name, finished_at, rolled_back_at FROM _prisma_migrations ORDER BY started_at;
   -- Expect 20 rows, none with a NULL finished_at or a non-NULL rolled_back_at, the last
   -- migration_name = '20261018120000_guild_review_one_per_reviewer'.
   SELECT count(*) FROM "Post";          -- migration 2 locks "Post" to build indexes
   SELECT count(*) FROM "PostLike";      -- migration 3 removes duplicate rows from both
   SELECT count(*) FROM "PostBookmark";  -- if any of the three is non-zero, note it below
   SELECT count(*) FROM pg_stat_activity
   WHERE state = 'idle in transaction' AND datname = current_database();  -- expect 0
   ```
   Take a fresh `pg_dump` of the database after this query, before step 2.
1. **Do not push the new API/BFF code to GitHub main yet.** Railway builds the
   BFF from main on push, so pushing before the migration lands starts a new
   build that can connect to Postgres before `ALTER TYPE ... ADD VALUE` has
   run.
2. `npx prisma migrate deploy` (all four migrations, one invocation) while the
   OLD BFF and client keep running and serving traffic.
3. **Readback**, immediately after step 2 (see the full queries below):
   4 new `_prisma_migrations` rows, all finished; `TaxonomyTerm` counts
   SECTOR 26 / SKILL 54 (all 54 with a non-null `parentId`) / PROJECT_TYPE 13;
   `npx prisma migrate diff --from-url $DATABASE_URL --to-schema-datamodel prisma/schema.prisma --exit-code`
   exits 0.
4. **Now** push and deploy the new BFF as a **fresh process**, and restart any
   new-build process that happened to start before step 2 (step 1 forbids the
   push, but if one slipped through anyway, restart it) -- Npgsql loads each
   Postgres enum's label set once per data source at first use, so a process
   that connected before the migration keeps the stale label set and cannot
   write a post with any new `PostType`/`ProjectType` (nor, for the envelope
   columns, even a NULL `sourceType`, since the type itself did not exist
   until the migration created it) until it is restarted, even though
   `/health` stays green throughout. Smoke test on the fresh process: create
   and read a post on a project; list projects; the `ProjectType` enum
   endpoint returns 13 values.
5. Deploy the new client. It tolerates an unknown `ProjectType` on read
   (`src/lib/contracts/project-contract.ts`), so it is safe last or, for that
   same reason, safe to ship early if ever needed -- just never before step 2
   for the BFF.

**Do not roll the BFF back to the pre-migration build once any project has
taken on a new `ProjectType` or any post has taken on a new `PostType`/source
pair** -- the old build's Prisma/EF client does not know those labels exist
and will fail to read the rows that use them.

## Preflight (read-only)

```sql
-- Migration 3 removes these rows; record them with the backup reference.
SELECT "postId", "userId", count(*) FROM "PostLike"     GROUP BY 1, 2 HAVING count(*) > 1;
SELECT "postId", "userId", count(*) FROM "PostBookmark" GROUP BY 1, 2 HAVING count(*) > 1;
-- Migration 2 builds indexes under a write lock on "Post"; size the table first.
SELECT count(*) FROM "Post";
```

Migration 3 deletes and then recounts inside one transaction, behind a
`LOCK TABLE ... IN SHARE ROW EXCLUSIVE MODE` that makes a like or bookmark
arriving mid-migration wait rather than fail the `CREATE UNIQUE INDEX` with
P3009; a `lock_timeout` of 5s bounds that wait. Apply in a write-quiet window
regardless.

## Readback

```sql
SELECT migration_name, finished_at FROM _prisma_migrations
WHERE migration_name >= '20261027000000' ORDER BY 1;          -- 4 rows, all finished
SELECT kind, count(*), count("parentId") FROM "TaxonomyTerm"
GROUP BY kind ORDER BY kind;    -- SECTOR 26/0, SKILL 54/54, PROJECT_TYPE 13/0 on a fresh seed
```

```bash
npx prisma migrate diff --from-url "$DATABASE_URL" \
  --to-schema-datamodel prisma/schema.prisma --exit-code   # exit 0
```

## Rehearsal evidence (local PostgreSQL 16, throwaway database, 2026-10-27, fix round)

- Applied migrations 1-22 (through `20261027001000_feed_envelope_columns`), seeded the
  cross-user race `migrate deploy` is meant to clean up -- `u1` liking `p1` twice and `u2` liking
  it once with `likesCount` stored at 2 (the racing-absolute-write value), `u3` liking `p2` three
  times with `likesCount` stored at 1 -- then applied the remaining two migrations.
- Readback: `p1` has 2 `PostLike` rows and `likesCount = 2`; `p2` has 1 row and `likesCount = 1`
  (the recount, not a rows-removed decrement, which would have under- or over-counted both).
- `INSERT INTO "Post" (..., "sourceType", "sourceId") VALUES (..., 'PROJECT', NULL)` -- a
  half-filled envelope pair -- was refused by `Post_sourceType_sourceId_check` (not the unique
  index, which cannot see two NULLs as equal).
- A second `migrate deploy` reported "No pending migrations to apply."
- `prisma migrate diff --from-url <scratch> --to-schema-datamodel prisma/schema.prisma --exit-code` exited 0.
- `TaxonomyTerm` counts: SECTOR 26/0, SKILL 54/54 (all parented), PROJECT_TYPE 13/0.
- Scratch database dropped after verification.

## Phase 2 addendum (2026-10-31): `20261031000000_feed_retraction_and_hashtags`

One more additive migration: nullable `Post.retractedAt TIMESTAMP(3)`, `Post.hashtags TEXT[] NOT NULL DEFAULT
ARRAY[]::TEXT[]` (catalog default, no rewrite), the GIN index `Post_hashtags_idx`, and four engagement indexes
`PostLike_userId_createdAt_idx`, `PostComment_authorId_createdAt_idx`, `PostComment_postId_idx`, `PostShare_postId_idx`.
It commutes with the money branch's `20261028000000_funding_intent_void` (different tables). The file header is the
source of truth.

**Order:** `migrate deploy` -> restart the API (its EF model selects both columns on every Post read; an API on the
new code against the old schema fails with 42703) -> BFF. No enum or type changed, so the Npgsql per-data-source type
cache does not force a BFF restart for this migration. Then, with `Feed:Projector:Enabled=true` on the API, run the
admin backfill (`POST /api/admin/feed/backfill`, dry run first): it re-tags existing envelopes from their sources'
copy and re-scores every post.

**If `migrate deploy` fails on `lock_timeout` (5 s; a long transaction held `Post`, `PostLike`, `PostComment` or
`PostShare`):**
Prisma records the migration as failed and refuses every later deploy with P3009. The migration runs in one
transaction, so nothing was applied. Recover with
```bash
npx prisma migrate resolve --rolled-back 20261031000000_feed_retraction_and_hashtags
npx prisma migrate deploy
```
at a quieter moment. The retry is safe even after a partial manual apply: every statement is `IF NOT EXISTS`.

**Readback:**
```sql
SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns
WHERE table_name = 'Post' AND column_name IN ('retractedAt', 'hashtags');   -- 2 rows, hashtags NOT NULL
SELECT indexname, indexdef FROM pg_indexes
WHERE indexname IN ('Post_hashtags_idx', 'PostLike_userId_createdAt_idx', 'PostComment_authorId_createdAt_idx',
                    'PostComment_postId_idx', 'PostShare_postId_idx');  -- 5 rows, Post_hashtags_idx USING gin
```

**Rehearsal (local PostgreSQL 16 on 5433, throwaway database, 2026-10-31):** `prisma migrate deploy` applied all 25
migrations to an empty scratch database; a second `migrate deploy` reported no pending migrations; the new migration
re-run by hand with `psql` was a no-op (every statement `IF NOT EXISTS`); readback as above; a row inserted without
naming `hashtags` read `{}`; and `prisma migrate diff --from-url <scratch> --to-schema-datamodel prisma/schema.prisma
--exit-code` exited 0 (no drift); database dropped. The URL was passed explicitly to the local scratch database; no
hosted database was touched.

## Phase 3 addendum (2026-11-01): `20261101000000_feed_help_wanted` (SR-03 phase 3, lane S3)

One more additive migration: the `HELP_WANTED` `PostType` label; nullable, unique `Post.taskId`
(the `ProjectTask` a HELP_WANTED post asks for, `ON DELETE SET NULL`); a `CHECK` pairing them
(`Post_taskId_help_wanted_only`: `"taskId" IS NULL OR "type"::text = 'HELP_WANTED'`) so "one
HELP_WANTED per task" holds without a partial index (Prisma 6.19.3 has no partial-index model, so
one would never clear `migrate diff --exit-code`); nullable `Notification.postId` (the post a
reply/claim notification is about, `ON DELETE CASCADE`) and the `(userId, postId)` index that
serves the UI's per-post notification collapse. The file header is the source of truth, including
why the CHECK casts `"type"` to text (`"type"::text = 'HELP_WANTED'`) instead of comparing the
enum directly: PostgreSQL will not let a transaction use an enum value an `ALTER TYPE ... ADD
VALUE` added earlier IN THAT SAME TRANSACTION when the value is parsed AS the enum (`enum_in`);
casting the *column* to text (`enum_out`) instead never resolves the literal as a `PostType`
value, so the restriction never fires and the add-value and its first consumer can share one
migration file (unlike `20261027000000_feed_enum_values` and
`20260909000000_economic_settlement_kind_values`, which both had to defer use to a later file).

**Order:** `migrate deploy` -> restart the API (Npgsql caches each Postgres enum's label set per
data source at first use, `ArdaNova.Infrastructure/DependencyInjection.cs` `MapEnum`; an API
process that connected before this migration cannot write or read a `HELP_WANTED` post until
restarted) -> client. **Before rolling the API back past this migration**, run
```sql
UPDATE "Post" SET "type" = 'POST', "taskId" = NULL WHERE "type" = 'HELP_WANTED';
```
An old API build does not know the `HELP_WANTED` label and fails to read any row that carries it
(22P02).

**If `migrate deploy` fails on `lock_timeout` (5 s):** same recovery as the phase 2 addendum above
-- the migration is one transaction, so a timeout applies nothing:
```bash
npx prisma migrate resolve --rolled-back 20261101000000_feed_help_wanted
npx prisma migrate deploy
```
The retry is safe even after a partial manual apply: every statement is guarded (`IF NOT EXISTS`,
or `DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = ...) THEN ... END IF; END $$;`
for the two foreign keys and the CHECK, which PostgreSQL has no `ADD CONSTRAINT IF NOT EXISTS` for).

**Readback:**
```sql
SELECT conname, contype FROM pg_constraint
WHERE conname IN ('Post_taskId_fkey', 'Post_taskId_help_wanted_only', 'Notification_postId_fkey');
                                                      -- 3 rows: contype 'f', 'c', 'f'
SELECT indexname FROM pg_indexes
WHERE indexname IN ('Post_taskId_key', 'Notification_userId_postId_idx');  -- 2 rows
SELECT enumlabel FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid
WHERE t.typname = 'PostType' AND enumlabel = 'HELP_WANTED';                -- 1 row
```

**Rehearsal (local PostgreSQL 16 on 5433, throwaway database `ardanova_s3_scratch`, 2026-11-01):**
`prisma migrate deploy` applied all 27 migrations to an empty scratch database, including this one,
in a single transaction (the enum-value-plus-CHECK trick above did not raise "unsafe use of new
value"); a second `migrate deploy` reported no pending migrations; the migration file re-run
directly with `prisma db execute --file` was a no-op (every statement guarded); `prisma migrate
diff --from-url <scratch> --to-schema-datamodel prisma/schema.prisma --exit-code` exited 0 (the
CHECK is invisible to Prisma's schema model and caused no drift). Seeded a user, project and two
tasks: a `POST`-type row carrying a `taskId` was refused by `Post_taskId_help_wanted_only` (23514);
a `HELP_WANTED` row on task one succeeded; a second `HELP_WANTED` on the SAME task was refused by
`Post_taskId_key` (23505 / Prisma P2002); deleting task one set the surviving post's `taskId` to
NULL (not an error, since the CHECK only constrains a non-null `taskId`); deleting that post
cascade-deleted its notification row. Database dropped after verification. The URL was passed
explicitly to the local scratch database; no hosted database was touched.

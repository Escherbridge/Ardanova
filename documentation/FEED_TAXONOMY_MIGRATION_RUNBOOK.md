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
ARRAY[]::TEXT[]` (catalog default, no rewrite), the GIN index `Post_hashtags_idx`, and two lookup indexes
`PostLike_userId_idx` / `PostComment_authorId_idx`. It commutes with the money branch's
`20261028000000_funding_intent_void` (different tables). The file header is the source of truth.

**Order:** `migrate deploy` -> restart the API (its EF model selects both columns on every Post read; an API on the
new code against the old schema fails with 42703) -> BFF. No enum or type changed, so the Npgsql per-data-source type
cache does not force a BFF restart for this migration. Then, with `Feed:Projector:Enabled=true` on the API, run the
admin backfill (`POST /api/admin/feed/backfill`, dry run first): it re-tags existing envelopes from their sources'
copy and re-scores every post.

**If `migrate deploy` fails on `lock_timeout` (5 s; a long transaction held `Post`, `PostLike` or `PostComment`):**
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
WHERE indexname IN ('Post_hashtags_idx', 'PostLike_userId_idx', 'PostComment_authorId_idx');  -- 3 rows, the first USING gin
```

**Rehearsal (local PostgreSQL 16 on 5433, throwaway database, 2026-10-31):** all 25 migrations applied in order with
`psql` (one transaction per file, except the one `CREATE INDEX CONCURRENTLY` file), the new migration re-applied as a
no-op, readback as above, a row inserted without naming `hashtags` read `{}`, and `prisma migrate diff --from-url
<scratch> --to-schema-datamodel prisma/schema.prisma --exit-code` exited 0 (no drift); database dropped. No hosted
database was touched.

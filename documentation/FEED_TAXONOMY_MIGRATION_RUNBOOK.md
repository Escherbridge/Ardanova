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
| 2 | `20261027001000_feed_envelope_columns` | Additive schema | Creates `FeedSourceType`; adds nullable `Post.sourceType/sourceId/opportunityId/eventId`, two `ON DELETE SET NULL` foreign keys, `UNIQUE (sourceType, sourceId)` and seven read indexes. Existing rows read as authored posts (NULL source). |
| 3 | `20261027002000_post_engagement_uniqueness` | **Destructive** + constraint | Deletes duplicate `PostLike`/`PostBookmark` rows per `(postId, userId)`, keeping the earliest; lowers `Post.likesCount` by exactly the likes removed; adds the two unique indexes. |
| 4 | `20261027003000_taxonomy_term` | Additive schema + seed | Creates `TaxonomyKind` and `TaxonomyTerm`, seeds 26 sectors (the 10 legacy category codes verbatim), 13 project types and 54 skills, `ON CONFLICT (kind, slug) DO NOTHING`. |

## Deploy order

1. Migrations (all four, one `npx prisma migrate deploy`).
2. The API build that carries the regenerated enums. An API that can write a
   new `PostType`/`ProjectType` before step 1 fails that write with 22P02.
3. The client can ship before or after either: it reads an unknown project type
   without failing and only offers types the API reports.

## Preflight (read-only)

```sql
-- Migration 3 removes these rows; record them with the backup reference.
SELECT "postId", "userId", count(*) FROM "PostLike"     GROUP BY 1, 2 HAVING count(*) > 1;
SELECT "postId", "userId", count(*) FROM "PostBookmark" GROUP BY 1, 2 HAVING count(*) > 1;
-- Migration 2 builds indexes under a write lock on "Post"; size the table first.
SELECT count(*) FROM "Post";
```

Migration 3 deletes and then indexes inside one transaction; a like written in
between fails the `CREATE UNIQUE INDEX`. Apply in a write-quiet window.

## Readback

```sql
SELECT migration_name, finished_at FROM _prisma_migrations
WHERE migration_name >= '20261027000000' ORDER BY 1;          -- 4 rows, all finished
SELECT kind, count(*), count("parentId") FROM "TaxonomyTerm"
GROUP BY kind ORDER BY kind;    -- SECTOR 26/0, SKILL 54/54, PROJECT_TYPE 13/0 on a fresh seed
```

## Rehearsal evidence (local PostgreSQL 16, throwaway database, 2026-10-26)

- All 24 migrations applied by `migrate deploy`; a second run reported "No pending migrations".
- `prisma migrate diff --from-url <scratch> --to-schema-datamodel prisma/schema.prisma --exit-code` exited 0.
- Each of the four files re-executed directly on the migrated database changed nothing.
- Seeded duplicates (one person liking a post three times, a doubled bookmark) left the
  earliest row each, took that post's counter from 4 to 2 and left another post's counter
  untouched; a repeat like and a repeat envelope `(PROJECT, proj-1)` were then refused (23505).

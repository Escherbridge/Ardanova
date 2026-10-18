# Governance / guild / finance migration runbook (migrations since 2026-10-17)

This covers the four Prisma migrations added during the loop 1-4 QA cycle on
`ardanova-client/prisma/migrations`, timestamped `20261017000000` through
`20261018120000`. They land **after** the baseline and release-migration
chain in `GATED_COMMERCE_MIGRATION_RUNBOOK.md` (through
`20260718003000_actor_assertion_replay_cleanup_index_postcondition`) and after
the eleven project/commerce/settlement migrations from
`20260908000000_project_presentation_fields` through
`20260909100000_retire_card_rail_and_guild_split` already on `main`. No
runbook in `documentation/` covers those eleven yet (loop 4 integration
checked); if production has not applied them, `migrate deploy` will apply
them first, so review them before this batch. This document does not repeat that
runbook's preconditions; it assumes them (non-production clone first, a
dedicated read-only role for every preflight, `migrate deploy` only, never
`db push`/`migrate dev` against shared data, exact project/environment/service
confirmed before any write).

**The operator runs every command in this document. QA (this loop) only
verified the preflight/readback SQL against the local demo database — it did
not run, and must not run, any step against production.**

## Scope: what ships, in order

| # | Migration | Kind | What it does |
| - | --- | --- | --- |
| 1 | `20261017000000_financial_engine_domain` | Additive schema | Adds 5 enums, 7 tables, and nullable link columns (`assetDefinitionId`, `eligibilityDecisionId`, `equityOrRedemptionRightPolicyId`, `projectTokenPolicyId`, plus two award/payment columns on `FundingIntent`) to `ProjectTokenConfig`, `EconomicSettlement`, `FundingIntent`, `TaskCommerceAgreement`. No column is altered or dropped; every new link is `NULL` until a reviewed backfill. |
| 2 | `20261017120000_vote_one_ballot_per_member` | Destructive + constraint | Deletes duplicate `Vote` rows for the same `(proposalId, voterId)`, keeping each voter's earliest ballot, then adds `UNIQUE ("proposalId", "voterId")`. Closes a concurrent-double-vote race in `GovernanceService.CastVoteAsync`. |
| 3 | `20261017130000_guild_owner_member_backfill` | Additive data only | Inserts a `GuildMember` row with `role = 'OWNER'` for every guild whose owner has no membership row yet (guilds created before `GuildService.CreateAsync` started writing it, or seeded directly). No `UPDATE`/`DELETE`/DDL. |
| 4 | `20261018120000_guild_review_one_per_reviewer` | Destructive + constraint + recompute | Deletes duplicate `GuildReview` rows for the same `(guildId, userId)`, keeping each reviewer's earliest review, adds `UNIQUE ("guildId", "userId")`, then recomputes `Guild.reviewsCount`/`Guild.rating` from the surviving rows. Closes the same class of concurrent-double-write race as #2, reproduced live in the loop-3 review. |

Each migration file carries this same ordering and rationale as a header
comment (`-- Lands AFTER <previous>`); this table is a consolidated index, not
a replacement for reading the SQL itself before applying it; the comments
there are the source of truth if this document and the file ever disagree.

**#1 is explicitly gated separately**: `conductor/tracks/financial-engine-domain-hardening/PASSOFF.md`
names "a reviewed additive migration generated and rehearsed against a
disposable or non-production PostgreSQL clone" as stopping-gate condition 2
of 5, and records it as **not yet met** (no baseline/additive preflight,
backfill, readback, reconciliation, or rollback evidence has been retained
against a clone of production's actual shape). QA's local verification
(below) confirms the SQL is syntactically correct and idempotent against a
fresh copy of the current schema; it is **not** a substitute for that
disposable-clone rehearsal, because the local demo database never went
through `migrate deploy` from production's pre-migration shape (see
"What QA actually verified," below).

## Apply order

```powershell
# 1. Confirm target: project, environment, service, database -- read it back before proceeding.
railway status
railway variables --service <api-service> --environment <env> | Select-String DATABASE_URL

# 2. Point the CLI at a READ-ONLY connection string for every preflight query below.
$env:DATABASE_URL = '<read-only connection supplied by the operator>'

# 3. Run every preflight query in the next section FIRST and retain the output.

# 4. Only after preflight is reviewed and a backup/restore point is recorded,
#    switch to the write/deploy identity and apply in order:
$env:DATABASE_URL = '<write/deploy connection supplied by the operator>'
cd ardanova-client
npx prisma migrate deploy
```

`prisma migrate deploy` applies pending migrations in filename (timestamp)
order automatically; there is no manual per-file step. The table above is for
review, not for invoking each file by hand. If `migrate deploy` reports a
failed/partial migration from a prior incident, resolve that first (`prisma
migrate resolve`) — do not layer these four on top of an unresolved history.

## Preflight (read-only; run against a read-only connection, record every result)

**All migrations — confirm what is already applied, so you know which of the four will run:**

```sql
SELECT migration_name, finished_at, rolled_back_at
FROM _prisma_migrations
WHERE migration_name >= '20260908000000'
ORDER BY migration_name;
-- Any row with finished_at IS NULL and rolled_back_at IS NULL is a failed migration: resolve it first.
```

**Migration 1 (financial-engine domain) — confirm no name collision before the additive DDL:**

```sql
SELECT typname FROM pg_type
WHERE typname IN ('AssetDefinitionKind','EquityOrRedemptionRightKind',
                   'EligibilityDecisionStatus','EconomicSettlementLegKind','SwapOrderStatus');
-- Expect 0 rows on a database that has not already run this migration.
```

**Migration 2 (vote de-duplication) — count and record what the DELETE will remove, BEFORE applying:**

```sql
SELECT "proposalId", "voterId", count(*) AS dup_count
FROM "Vote" GROUP BY 1, 2 HAVING count(*) > 1;
-- Record every row. The migration keeps each voter's EARLIEST (createdAt, id) ballot and
-- deletes the rest; that deletion is not reversible without this record plus a backup.
```

Then capture the exact rows the `DELETE` will remove (the migration's own
predicate as a `SELECT`), and keep the output with the backup reference:

```sql
SELECT later.*
FROM "Vote" AS later
JOIN "Vote" AS earlier
  ON later."proposalId" = earlier."proposalId"
 AND later."voterId" = earlier."voterId"
 AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");
```

Local demo result (not production, for SQL validation only): `0 rows` — the
seeded local database has no duplicate ballots to lose, so this query is
confirmed syntactically correct but the destructive path was not exercised.

Migrations 2 and 4 delete duplicates and then build a unique index in the
same file. A duplicate written between the two (an API still serving votes or
reviews during deploy) fails the `CREATE UNIQUE INDEX` and leaves the
migration failed. Apply during a write-quiet window, or with the API scaled
to zero, and re-run the duplicate counts right before `migrate deploy`.

**Migration 3 (guild-owner backfill) — preview exactly which rows will be inserted:**

```sql
SELECT g."id" AS guild_id, g."ownerId"
FROM "Guild" AS g
WHERE EXISTS (SELECT 1 FROM "User" AS u WHERE u."id" = g."ownerId")
  AND NOT EXISTS (
    SELECT 1 FROM "GuildMember" AS m
    WHERE m."guildId" = g."id" AND m."userId" = g."ownerId"
  );
-- This is the SELECT half of the migration's INSERT ... SELECT; it inserts nothing by itself.

SELECT indexname FROM pg_indexes
WHERE tablename = 'GuildMember' AND indexname = 'GuildMember_guildId_userId_key';
-- Expect 1 row: the migration's ON CONFLICT ("guildId", "userId") needs this unique index (baseline).

SELECT count(*) FROM "GuildMember" WHERE "id" LIKE 'gm_owner_%';
-- Expect 0: the rollback below deletes by this prefix, so pre-existing ids with it must be known first.
```

Local demo result: 1 row (`cmvdl4snr0007pe4wbyetertr`, the seeded
"Neighborhood Infrastructure Guild") — confirms the query correctly finds a
real owner-without-membership gap on a database that was seeded directly
rather than through `GuildService.CreateAsync`.

**Migration 4 (guild-review de-duplication) — same shape as migration 2:**

```sql
SELECT "guildId", "userId", count(*) AS dup_count
FROM "GuildReview" GROUP BY 1, 2 HAVING count(*) > 1;
-- Record every row for the same reason as migration 2's preflight.
```

Capture the exact rows the `DELETE` will remove:

```sql
SELECT later.*
FROM "GuildReview" AS later
JOIN "GuildReview" AS earlier
  ON later."guildId" = earlier."guildId"
 AND later."userId" = earlier."userId"
 AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");
```

Also snapshot the aggregate fields this migration recomputes, so a rollback
has something to restore to if the recompute is ever found wrong:

```sql
SELECT "id", "reviewsCount", "rating" FROM "Guild";
```

Local demo result: `0 rows` of duplicates; aggregate snapshot recorded has a
single guild with whatever review count the seed produced (no reviews
seeded), so the recompute path itself was not exercised against real
duplicate data either.

## Rollback notes (per migration)

1. **`financial_engine_domain`** — additive only (new enums/tables, nullable
   columns). Safe to drop cleanly **as long as nothing has written into the
   new nullable link columns yet** (true immediately after this migration,
   since every write path into them belongs to the still-gated financial
   stopping-gate work). If rollback is needed, go in reverse dependency order: drop
   the new link columns on `ProjectTokenConfig`, `EconomicSettlement`,
   `FundingIntent` and `TaskCommerceAgreement` first (their foreign keys point
   at the new tables), then the seven new tables (`SwapOrder`, `SwapQuote`,
   `EconomicSettlementLeg`, `EligibilityDecision`,
   `EquityOrRedemptionRightPolicy`, `ProjectTokenPolicy`, `AssetDefinition`),
   then the five enum types, then delete the migration's `_prisma_migrations`
   row so the history matches. Once
   any row populates `assetDefinitionId`/`eligibilityDecisionId`/etc., treat
   this as a one-way migration and plan a forward fix instead.
2. **`vote_one_ballot_per_member`** — the `DROP INDEX "Vote_proposalId_voterId_key"`
   half is reversible. **The `DELETE` half is not**: rows removed because a
   voter had two ballots cannot be restored except from a pre-migration
   backup or the preflight's recorded row list. This is why the preflight
   step is mandatory, not optional, before `migrate deploy` runs this file.
3. **`guild_owner_member_backfill`** — reversible. The inserted rows are
   identifiable by their deterministic id prefix (`'gm_owner_' || guildId`):
   `DELETE FROM "GuildMember" WHERE "id" LIKE 'gm_owner_%' AND "id" = 'gm_owner_' || "guildId";`
   restores the pre-migration state exactly, because the migration never
   touches a pre-existing membership row (`ON CONFLICT DO NOTHING`).
4. **`guild_review_one_per_reviewer`** — same two-part shape as migration 2:
   `DROP INDEX "GuildReview_guildId_userId_key"` is reversible; the `DELETE`
   of duplicate reviews is not, without a backup or the preflight's row list.
   The subsequent `rating`/`reviewsCount` recompute is also only reversible
   via the aggregate snapshot taken in the preflight step above — restore
   those two columns per guild from the snapshot if the recompute must be
   undone.

## What QA actually verified (this loop, local only)

- Ran every preflight SELECT above against the local dev database
  (`127.0.0.1:5433`, `ardanova_local_demo_loop`, via `psql`) and confirmed
  each is syntactically valid against the live schema and returns the
  expected shape.
- Confirmed via `\d "Vote"` that `Vote_proposalId_voterId_key` exists as a
  genuine `UNIQUE` btree index (not just a non-unique index) on the local
  schema, matching what the migration creates.
- Did **not** run `prisma migrate deploy` itself, anywhere, against anything.
  The local demo database has no `_prisma_migrations` table at all (it was
  provisioned by schema push for QA seeding, not by the migration chain), so
  it cannot stand in for the disposable-clone rehearsal that stopping-gate
  condition 2 requires — that rehearsal must start from an actual copy of
  production's current (pre-this-batch) schema and data shape, which only the
  operator can provision.
- Did not touch, and this document does not reference, any `DATABASE_URL`
  from a repository `.env` file; those point at the remote Railway database
  per this project's standing instruction and were never read this loop.

## Cross-references

- `documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md` — the baseline +
  release-migration chain this batch lands after, and the house conventions
  (read-only preflight role, fingerprinting, Path A/B) this document follows.
- `conductor/tracks/financial-engine-domain-hardening/PASSOFF.md` — the
  5-condition stopping gate; condition 2 is the disposable-clone rehearsal
  this batch's migration 1 still needs before any production apply.
- `conductor/tracks/governance-reachability_20261018/spec.md`,
  `conductor/tracks/guild-review-integrity_20261018/spec.md` — the feature
  tracks that introduced migrations 2 and 4 respectively.

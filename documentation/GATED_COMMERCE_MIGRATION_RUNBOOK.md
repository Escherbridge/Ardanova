# Gated-commerce additive migration runbook

Use this runbook before the first production deployment of the gated-commerce
schema. The preflight is read-only: it issues only catalog and data `SELECT`
queries and must use a dedicated PostgreSQL role that has `CONNECT`, `USAGE`,
and `SELECT` only. It never prints `DATABASE_URL` or other connection details.

## Preconditions

- The operator has a non-production clone or staging database that represents
  the approved production baseline.
- `DATABASE_URL` is supplied only through the operator environment and points
  to a read-only role for preflight.
- The database is not managed with `prisma db push`. This repository has no
  reviewed migration history, so a reviewed, additive Prisma migration is
  required before production deployment.
- No funding allocation, payout, task reward, swap, escrow release, or AZOA
  outbox dispatcher is enabled during this procedure.

## 1. Pin the approved baseline

Run the preflight against the approved non-production clone with a deliberately
nonmatching placeholder fingerprint. It exits nonzero but prints the inspected
fingerprint without exposing database credentials. A reviewer records that
value only after inspecting the schema and data report.

```powershell
$env:DATABASE_URL = '<read-only connection supplied by the operator>'
$env:MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT = ('0' * 64)
npm run db:preflight-commerce -- --phase baseline
```

Do not treat the displayed value as approved merely because the command ran.
Store the reviewed value in the release environment as
`MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT`, then re-run the baseline preflight:

```powershell
$env:MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT = '<reviewed 64-character SHA-256>'
npm run db:preflight-commerce -- --phase baseline
```

The baseline phase requires `ProjectInvestment`, `ProjectTokenConfig`, and
`Wallet`. `ProjectInvestment.stripePaymentIntentId` is an existing baseline
column: the additive change is its exact, valid, ready, non-partial unique
index, not a new column. The baseline requires every gated-commerce table,
wallet-verification column, and `assetScale` column to be absent. It also
reports project-token configurations with no authoritative chain asset id; that
is a future settlement blocker, not a reason to infer an id during the schema
migration. Any nonzero exit is a stop condition; do not create or apply a
migration from that database.

## 2. Create and review an additive migration off production

Against a disposable database cloned from the approved baseline, create a
Prisma migration that only adds the DBML-declared columns, tables, constraints,
and indexes. The reviewed SQL must:

1. add nullable/backfillable fields first where existing data requires it;
2. reconcile duplicate non-null `ProjectInvestment.stripePaymentIntentId`
   values before the unique constraint is created;
3. add `ProjectTokenConfig.assetScale` nullable **without a database default**,
   backfill every existing row from the authoritative token configuration, and
   only then review a separate no-default `NOT NULL` hardening step. `DEFAULT 6`
   is prohibited: it fabricates a historical fact and can silently mis-scale a
   token allocation;
4. record missing `assetId` values as a separate authoritative-chain backfill
   obligation, not an inferred default;
5. build and validate unique indexes and foreign keys with the production
   locking/rollback plan reviewed by an operator.

`assetScale` is a four-release evidence migration, not an additive-column
shortcut: nullable/no-default introduction; authoritative chain-source capture;
validated, read-back reconciliation; then a separately approved `NOT NULL`
no-default hardening migration. The current DBML `DEFAULT 6` declaration is not
valid historic evidence and must not be used to generate the first migration.
The range is 0--18 (inclusive), matching fixed-scale settlement precision. See
`ASSET_SCALE_MIGRATION_CONTRACT.md` for the required manifest, source checks,
implementation cutover points, and operator proof.

`prisma db push`, `prisma migrate dev`, and any schema write are prohibited on
the shared or production database. The migration artifact and its rollback
decision are review inputs; this script never creates or applies either.

## 3. Verify the deployed additive state

After the reviewed migration is deployed by the approved release path, run:

```powershell
$env:DATABASE_URL = '<read-only connection supplied by the operator>'
$env:MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT = '<reviewed post-migration fingerprint>'
npm run db:preflight-commerce -- --phase additive
```

The additive phase fails closed if any target table/column has the wrong
PostgreSQL type, nullability, or default; if any required index is missing,
partial, invalid, unready, non-unique where uniqueness is required, or has the
wrong key order; or if the approved fingerprint differs. It explicitly rejects
any `assetScale` database default. It reports masked duplicate payment-intent
ids and up to 100 project-token configurations whose `assetScale` is null or
`assetId` is absent. A null scale blocks the release. A missing asset id is an
explicit settlement/launch blocker until its authoritative on-chain record is
backfilled; do not guess an asset id from a project name, wallet, or UI data.

Only a zero-exit preflight plus separately reviewed deployment, concurrency,
webhook, outbox, and reconciliation evidence permits progressing toward value
flow activation. It does not itself authorize value-moving flows.

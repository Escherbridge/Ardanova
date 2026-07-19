# Gated-commerce migration runbook

This repository now carries an immutable Prisma baseline plus versioned,
idempotent release migrations. The preflight remains read-only: it issues only
catalog and data `SELECT` queries and never prints a database URL.

## Migration history

- `20260718000000_schema_baseline` is the complete schema snapshot for a new
  database and the baseline marker for an existing database.
- `20260718000500_asset_scale_evidence_boundary` removes the historical scale
  default and permits a nullable reconciliation window.
- `20260718001000_actor_assertion_replay_cleanup_index` adds the ordered online
  replay-cleanup index.
- `20260718002000_task_escrow_dispute_context` adds nullable dispute evidence
  fields.
- `20260718003000_actor_assertion_replay_cleanup_index_postcondition` rejects a
  partial, unique, invalid, unready, or wrong-key same-named cleanup index that
  PostgreSQL's `IF NOT EXISTS` would otherwise retain.

Do not edit a migration after it has been applied. Create a new timestamped
migration for every later change. The baseline was generated with:

```powershell
npx prisma migrate diff --from-empty --to-schema-datamodel prisma/schema.prisma --script --output prisma/migrations/20260718000000_schema_baseline/migration.sql
```

## Preconditions

- Confirm the exact project, environment, service, and database before any
  write. Never infer them from a local Railway link alone.
- Use a non-production clone for rehearsal and a dedicated read-only role for
  every preflight.
- Disable funding allocation, payout, task reward, swap, escrow release, and
  the Azoa outbox dispatcher throughout the procedure.
- Back up the database and record restore evidence before migration.
- Never use `prisma db push` or `prisma migrate dev` against shared or hosted
  data.

## Path A: new, empty database

Run `npm run db:migrate`. Prisma applies the complete baseline, then the four
idempotent release migrations. Afterward, run the `additive` preflight described
below and retain its reviewed fingerprint.

## Path B: established schema without Prisma history

Do not resolve the baseline until a full catalog diff classifies the source.
There are two supported cases:

- The schema already matches the complete baseline and differs only by the
  four versioned release migrations. Review that complete diff, resolve only
  the baseline, then run `migrate deploy`.
- The schema matches the captured 2026-07-18 production catalog. Use the
  separately reviewed, one-time adoption SQL below before resolving the
  baseline. This is the current production path.

Any third shape is unsupported. Generate a new source-specific migration and
rehearse it from a restored clone; do not adapt commands interactively against
production.

### B1. Inspect and classify the source

A deliberately wrong fingerprint prints the observed value while still
exiting nonzero. Use `baseline` for the captured pre-adoption production shape:

```powershell
$env:DATABASE_URL = '<read-only connection supplied by the operator>'
$env:MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT = ('0' * 64)
npm run db:preflight-commerce -- --phase baseline
```

Review the catalog and data findings independently. In particular, duplicate
non-null Stripe payment-intent IDs must be reconciled before the adoption SQL
creates their unique index. Record the observed fingerprint only after that
review, then rerun the same phase with the approved value.

That preflight is deliberately scoped to commerce and is **not** proof that an
established database matches the complete Prisma baseline. Before marking the
full baseline applied, generate and retain a full schema-to-schema diff from
the same read-only database connection:

```powershell
New-Item -ItemType Directory -Force .release-evidence | Out-Null
npx prisma migrate diff --from-url $env:DATABASE_URL --to-schema-datamodel prisma/schema.prisma --script --output .release-evidence/baseline-adoption.sql
Get-FileHash -Algorithm SHA256 .release-evidence/baseline-adoption.sql
```

Two reviewers must inspect the entire diff. If the database already matches the
baseline, it may contain only the three schema-changing release effects listed above:
the asset-scale evidence boundary, replay-cleanup index, and four correctly
typed nullable dispute columns. A commerce fingerprint alone never authorizes
`migrate resolve`.

For the captured production shape, the regenerated diff must instead match
`prisma/adoption/20260718_production_to_baseline.sql` byte-for-byte. Verify both
the comparison and the checked-in checksum:

```powershell
git diff --no-index -- `
  prisma/adoption/20260718_production_to_baseline.sql `
  .release-evidence/baseline-adoption.sql
Get-FileHash -Algorithm SHA256 `
  prisma/adoption/20260718_production_to_baseline.sql
```

The reviewed checksum is recorded in `prisma/adoption/CHECKSUMS.sha256`. Any
difference, destructive statement, type change, new non-null requirement, or
unexpected constraint is a stop condition.

### B2. Apply the captured production adoption

First prove a current backup can be restored, disable all value-moving paths,
and enter the approved maintenance window. Apply the reviewed SQL atomically;
do not use `prisma db push`:

```powershell
psql $env:DATABASE_URL `
  --file prisma/adoption/apply-20260718-production-adoption.psql
```

Do not execute the generated SQL file directly. The wrapper owns the atomic
transaction, stops on the first error, and pins unqualified generated objects
to the `public` schema with a transaction-local `search_path`.

Immediately generate the full diff again. It must be empty before migration
history is written:

```powershell
npx prisma migrate diff `
  --from-url $env:DATABASE_URL `
  --to-schema-datamodel prisma/schema.prisma `
  --script --exit-code
```

Only after the empty-diff check, mark the baseline snapshot as applied. The
four later migrations are idempotent and record their own history when
deployed:

```powershell
npx prisma migrate resolve --applied 20260718000000_schema_baseline
npm run db:migrate
npm run db:migrate
npx prisma migrate status
```

The second deploy must report no pending migrations. `migrate resolve` records
history; it does not execute baseline SQL. Never mark a later migration applied
manually. If any step fails, stop traffic promotion and restore the verified
backup rather than attempting reverse DDL.

## Source data obligations

Before the unique payment-intent index is relied on, reconcile duplicate
non-null `ProjectInvestment.stripePaymentIntentId` values. The preflight reports
only masked identifiers.

`ProjectTokenConfig.assetScale` is evidence, not a convenient default. Existing
rows must be reconciled from the authoritative chain record, with a 0–18 scale,
and `assetId` must also come from that record. A database default must never be
used to fabricate historical scale. Missing scale or asset IDs block live-value
activation even if the schema migration itself succeeds. The nullable storage
shape is intentional; every new configuration still requires an explicit,
validated 0--18 scale at the API boundary.

## Verify the deployed state

Obtain and independently review the post-migration fingerprint, then run:

```powershell
$env:DATABASE_URL = '<read-only connection supplied by the operator>'
$env:MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT = '<reviewed post-migration SHA-256>'
npm run db:preflight-commerce -- --phase additive
npx prisma migrate status
```

The final `additive` phase requires the exact valid, ready, non-partial index on
`ActorAssertionReplay(expiresAt, jti)` and the complete established commerce
surface. API readiness also verifies the exact nullable text/timestamp(3)
contract of the four dispute columns. A zero-exit preflight does
not authorize value movement; provider, custody, KYC provenance, concurrency,
webhook, outbox, and reconciliation evidence remain independent gates.

# Established-schema adoption

This directory contains reviewed, one-time SQL for databases that predate the
Prisma migration ledger. These files are not normal Prisma migrations and are
never applied automatically by `prisma migrate deploy`.

See `documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md` for the required
backup, read-only diff, data preflight, two-person review, application,
baseline-resolution, and rollback procedure.

`20260718_production_to_baseline.sql` was generated from the read-only
production catalog captured on 2026-07-18 and the checked-in Prisma schema. It
is additive by construction, but must be regenerated and compared against the
live catalog immediately before use. Any destructive statement or unexpected
drift is a stop condition.

The checked-in SQL has SHA-256
`8aeee5101fa04bda659cfd94886871a521e5f9c7053e4004cd8c3263b443028b`.
The digest is an integrity check, not authorization to apply the file. Apply
only through `apply-20260718-production-adoption.psql`; its transaction-local
`search_path` pins all generated unqualified objects to `public`, and its error
mode rolls the whole transaction back on the first failure. Never run the raw
SQL directly or try to reverse it statement-by-statement. Recovery is a restore
from the verified pre-adoption backup.

# Prisma migration contract

`20260718000000_schema_baseline` is immutable after its first application. It
creates the complete schema for a new database and can be marked applied only
when an existing database has independently passed the matching catalog review.

Every later change belongs in a new timestamped directory. Production uses
`prisma migrate deploy`; `db push` and `migrate dev` are local-only tools. See
`documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md` for the two supported
bootstrap paths and the required before/after evidence. The commerce preflight
does not prove full-baseline equivalence; established databases also require a
reviewed complete Prisma diff before `migrate resolve`.

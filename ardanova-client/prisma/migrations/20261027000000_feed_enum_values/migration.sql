-- Feed envelope and domain-neutral project kinds: enum labels only (SR-03 phase 0).
-- Lands AFTER 20261018120000_guild_review_one_per_reviewer.
--
-- Every feedable event (a project launch, an opportunity opening, a guild update, a scheduled
-- event...) becomes one envelope row in "Post", typed by PostType. The nine PostType labels below
-- are the envelope kinds the feed projector will write. The six ProjectType labels let a project be
-- a service, a physical build, a venue, a program, research or a cooperative, not only a tech
-- product. Both enums are ADDITIVE ONLY: no label is renamed or dropped, because stored rows and
-- older clients still hold every existing one.
--
-- This migration adds ONLY labels. `ALTER TYPE ... ADD VALUE` cannot be followed in the same
-- transaction by a statement that USES the new value, and Prisma runs one migration directory per
-- transaction, so anything that uses these labels lives in a later migration (same rule as
-- 20260909000000_economic_settlement_kind_values). Labels are appended in DBML order; IF NOT EXISTS
-- makes a re-run harmless.
--
-- Deploy order: apply all four SR-03 phase 0 migrations (through 20261027003000) BEFORE any API
-- build that can write the new labels; a write that reaches PostgreSQL first raises 22P02
-- (invalid_text_representation). Do not push the new API code to GitHub main first -- Railway
-- builds the BFF from main on push, so pushing starts a new build racing the migration. Apply the
-- migrations while the OLD BFF keeps running, THEN deploy the new BFF as a fresh process, and
-- restart any new-build process that happened to start before the migration landed: Npgsql loads
-- each Postgres enum's labels once per data source at first use, so a process that connected
-- before `ADD VALUE` ran keeps the old label set and raises 22P02 on the new labels even after the
-- migration is in the database, until it is restarted. The client tolerates an unknown ProjectType
-- on read (src/lib/contracts/project-contract.ts), so it can ship last.
-- See documentation/FEED_TAXONOMY_MIGRATION_RUNBOOK.md for the full deploy sequence.
--
-- Requires PostgreSQL 12 or newer (ADD VALUE inside a transaction block).
-- Generated with `prisma migrate diff` from main's schema to the DBML-regenerated schema, then
-- guarded and split by hand.

ALTER TYPE "ProjectType" ADD VALUE IF NOT EXISTS 'SERVICE';

ALTER TYPE "ProjectType" ADD VALUE IF NOT EXISTS 'PHYSICAL_BUILD';

ALTER TYPE "ProjectType" ADD VALUE IF NOT EXISTS 'VENUE';

ALTER TYPE "ProjectType" ADD VALUE IF NOT EXISTS 'PROGRAM';

ALTER TYPE "ProjectType" ADD VALUE IF NOT EXISTS 'RESEARCH';

ALTER TYPE "ProjectType" ADD VALUE IF NOT EXISTS 'COOPERATIVE';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'PROJECT_LAUNCHED';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'FUNDING_PROGRESS';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'OPPORTUNITY_OPENED';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'OPPORTUNITY_UPDATE';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'GUILD_FOUNDED';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'GUILD_UPDATE';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'EVENT_SCHEDULED';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'DISCUSSION';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'MEMBER_JOINED';

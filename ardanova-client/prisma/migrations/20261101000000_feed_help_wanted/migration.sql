-- HELP_WANTED post type, Post.taskId (the ask-for-help's task) and Notification.postId (SR-03 phase 3, S3).
-- Lands AFTER 20261031000000_feed_retraction_and_hashtags. It touches "Post", "Notification" and
-- "PostType", none of which that migration's own columns depend on, so deploy order only matters
-- relative to the generator (S0, merged b73a509) and the backend/client that read these columns (K1/K2).
--
-- 1. "HELP_WANTED" (PostType). The composer's "Who can help?" prompt and the task board's "Ask for
--    help" both write this type. See point 3 below for why the enum label and everything that reads
--    it cannot share this migration's transaction.
--
-- 2. "Post.taskId" (nullable, unique). The ProjectTask a HELP_WANTED post asks for; NULL on every
--    other type. "One HELP_WANTED per task" is a plain unique index on "taskId" (several NULLs are
--    allowed in a unique index) PAIRED WITH the CHECK below that confines a non-null "taskId" to
--    HELP_WANTED rows - together they give "one per task" without a partial index. Prisma 6.19.3 has
--    no partial-index model and the repo has none in any migration; a partial unique index would be
--    invisible to `schema.prisma` and `migrate diff --exit-code` would never be 0 (SR-03-PHASE3-PLAN.md
--    §8-8). ON DELETE SET NULL: deleting the task un-asks it instead of deleting someone's words.
--
-- 3. The enum-in-one-transaction problem. Prisma runs one migration directory per transaction (same
--    rule the repo already follows: 20260909000000_economic_settlement_kind_values,
--    20261027000000_feed_enum_values). PostgreSQL will not let the CURRENT transaction use a value an
--    `ALTER TYPE ... ADD VALUE` just added in that SAME transaction - but only when the value is
--    PARSED AS the enum (`enum_in`, e.g. a bare `'HELP_WANTED'` compared or cast against the `"type"`
--    column), which is what every prior PostType addition in this repo avoided by using a value in a
--    LATER migration file. The CHECK below instead casts the COLUMN to text (`"type"::text`, i.e.
--    `enum_out`) and compares it to a plain text literal, so PostgreSQL never has to resolve
--    'HELP_WANTED' as a "PostType" value at all - `enum_in` is never called and the restriction never
--    fires. This keeps the add-value and its first consumer in ONE migration folder, which is what the
--    S3 lane brief asks for. Proven on a scratch DB (ardanova_s3_scratch, 2026-11-01): this migration
--    applies cleanly in one transaction, a second apply is a no-op, and the CHECK correctly rejects a
--    "taskId" on a POST row (23514) and a unique index collision on a repeated "taskId" (23505).
--
-- 4. "Notification.postId" (nullable). The Post a reply/claim notification is about, so the UI can
--    collapse several notifications about the same post into one line. ON DELETE CASCADE: a
--    notification about a deleted post is meaningless on its own. The (userId, postId) index serves
--    that per-post collapse read ("how many unread notifications does this user have about this
--    post").
--
-- Rollback: an old backend cannot read an enum label it does not know (22P02 on any Post row whose
-- "type" is HELP_WANTED), so before rolling the backend back past this migration, run
--   UPDATE "Post" SET "type" = 'POST', "taskId" = NULL WHERE "type" = 'HELP_WANTED';
-- (documentation/FEED_TAXONOMY_MIGRATION_RUNBOOK.md has the full rollback runbook entry.) Separately,
-- Npgsql caches each Postgres enum's labels per data source at first use (ArdaNova.Infrastructure/
-- DependencyInjection.cs `MapEnum`), so the backend must boot AFTER this migration lands, not before -
-- deploy order is migrate -> backend -> client (SR-03-PHASE3-PLAN.md §9).
--
-- Strictly additive: one enum value, two nullable columns, two indexes, two foreign keys, one CHECK.
-- No row is rewritten (both new columns are nullable with no default) and no existing row can violate
-- the CHECK (no row has "type" = 'HELP_WANTED' yet). Every statement is guarded, so a partial re-run
-- (e.g. after a lock_timeout failure - see the runbook, `migrate resolve --rolled-back`, retry) creates
-- or rejects nothing twice. Hand-written against `prisma migrate diff`'s suggested script (it has no
-- opinion on the CHECK, since Prisma does not model CHECK constraints), then guarded and annotated.
--
-- Requires PostgreSQL 12 or newer (ADD VALUE inside a transaction block).

SET LOCAL lock_timeout = '5s';

ALTER TYPE "PostType" ADD VALUE IF NOT EXISTS 'HELP_WANTED';

ALTER TABLE "Post" ADD COLUMN IF NOT EXISTS "taskId" TEXT;

ALTER TABLE "Notification" ADD COLUMN IF NOT EXISTS "postId" TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS "Post_taskId_key" ON "Post"("taskId");

CREATE INDEX IF NOT EXISTS "Notification_userId_postId_idx" ON "Notification"("userId", "postId");

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'Post_taskId_fkey') THEN
    ALTER TABLE "Post" ADD CONSTRAINT "Post_taskId_fkey" FOREIGN KEY ("taskId") REFERENCES "ProjectTask"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'Notification_postId_fkey') THEN
    ALTER TABLE "Notification" ADD CONSTRAINT "Notification_postId_fkey" FOREIGN KEY ("postId") REFERENCES "Post"("id") ON DELETE CASCADE ON UPDATE CASCADE;
  END IF;
END $$;

-- "type"::text, not "type" = 'HELP_WANTED'::"PostType" - see point 3 above. The literal stays plain
-- text on both sides, so PostgreSQL never resolves 'HELP_WANTED' as a "PostType" value in this
-- transaction.
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'Post_taskId_help_wanted_only') THEN
    ALTER TABLE "Post" ADD CONSTRAINT "Post_taskId_help_wanted_only" CHECK ("taskId" IS NULL OR "type"::text = 'HELP_WANTED');
  END IF;
END $$;

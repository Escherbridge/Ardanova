-- Feed envelope columns on "Post" (SR-03 phase 0).
-- Lands AFTER 20261027000000_feed_enum_values.
--
-- A feed envelope is a "Post" row the feed projector writes for something that happened elsewhere.
-- ("sourceType", "sourceId") names the row it was projected from, and its UNIQUE index makes the
-- projector idempotent: replaying a source upserts the same envelope instead of adding a second.
-- A post a person wrote keeps both columns NULL; PostgreSQL treats NULLs as distinct in a unique
-- index, so authored posts never collide with each other. "opportunityId" and "eventId" join the
-- existing nullable "projectId"/"guildId" target links, with the same ON DELETE SET NULL.
--
-- Strictly additive: one enum, four nullable columns with no default (no table rewrite, every
-- existing row reads as an authored post), two foreign keys and eight indexes. Post had no index
-- beyond its primary key; these serve the feed's newest-first, trending and per-project /
-- per-guild / per-author reads.
--
-- CREATE INDEX (not CONCURRENTLY: Prisma runs each migration in a transaction) blocks writes to
-- "Post" while it builds. The released database holds 0 posts, so the lock is momentary; on a
-- populated copy, measure it before deploy.
--
-- Generated with `prisma migrate diff`, then guarded by hand so a partial re-run is harmless.

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'FeedSourceType') THEN
    CREATE TYPE "FeedSourceType" AS ENUM ('PROJECT', 'PROJECT_UPDATE', 'PROJECT_MILESTONE', 'PROJECT_TASK', 'PROJECT_TOKEN_CONFIG', 'PROJECT_MEMBER', 'PROJECT_COMMENT', 'GUILD', 'GUILD_UPDATE', 'GUILD_MEMBER', 'OPPORTUNITY', 'OPPORTUNITY_UPDATE', 'OPPORTUNITY_COMMENT', 'EVENT');
  END IF;
END $$;

ALTER TABLE "Post" ADD COLUMN IF NOT EXISTS "sourceType" "FeedSourceType",
ADD COLUMN IF NOT EXISTS "sourceId" TEXT,
ADD COLUMN IF NOT EXISTS "opportunityId" TEXT,
ADD COLUMN IF NOT EXISTS "eventId" TEXT;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'Post_opportunityId_fkey') THEN
    ALTER TABLE "Post" ADD CONSTRAINT "Post_opportunityId_fkey" FOREIGN KEY ("opportunityId") REFERENCES "Opportunity"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'Post_eventId_fkey') THEN
    ALTER TABLE "Post" ADD CONSTRAINT "Post_eventId_fkey" FOREIGN KEY ("eventId") REFERENCES "Event"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "Post_sourceType_sourceId_key" ON "Post"("sourceType", "sourceId");

CREATE INDEX IF NOT EXISTS "Post_createdAt_idx" ON "Post"("createdAt");

CREATE INDEX IF NOT EXISTS "Post_trendingScore_idx" ON "Post"("trendingScore");

CREATE INDEX IF NOT EXISTS "Post_projectId_createdAt_idx" ON "Post"("projectId", "createdAt");

CREATE INDEX IF NOT EXISTS "Post_guildId_createdAt_idx" ON "Post"("guildId", "createdAt");

CREATE INDEX IF NOT EXISTS "Post_authorId_createdAt_idx" ON "Post"("authorId", "createdAt");

CREATE INDEX IF NOT EXISTS "Post_opportunityId_idx" ON "Post"("opportunityId");

CREATE INDEX IF NOT EXISTS "Post_eventId_idx" ON "Post"("eventId");

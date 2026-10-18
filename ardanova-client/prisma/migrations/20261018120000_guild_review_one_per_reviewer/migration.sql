-- One review per person per guild, enforced by the database.
-- UNREHEARSED: not yet applied to a copy of any populated database. Before `migrate deploy`,
-- count duplicates (SELECT "guildId","userId",count(*) FROM "GuildReview" GROUP BY 1,2 HAVING count(*)>1)
-- and record the rows the DELETE below will remove; it is destructive by design.
-- Lands AFTER 20261017130000_guild_owner_member_backfill.
--
-- GuildReviewService.CreateAsync checks for an existing review and then inserts. Two requests from
-- the same person that arrive together both passed the check and both rows were stored (reproduced
-- live in the loop-3 review), so one reviewer could weigh twice in a guild's rating. The unique index
-- is the only guard that holds under concurrency; the service maps its violation (SQLSTATE 23505)
-- to "You have already reviewed this guild".
--
-- A duplicate review is invalid by definition, so any that already exist are removed first, keeping
-- each reviewer's EARLIEST review (createdAt, then id as a stable tie-break). The stored rating and
-- reviewsCount are then recounted from the surviving rows (GuildService also derives them on read).
-- Statements are guarded so a partial re-run is harmless. Hand-written; regenerate Prisma/EF
-- artifacts from the DBML at merge.

DELETE FROM "GuildReview" AS later
USING "GuildReview" AS earlier
WHERE later."guildId" = earlier."guildId"
  AND later."userId" = earlier."userId"
  AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");

CREATE UNIQUE INDEX IF NOT EXISTS "GuildReview_guildId_userId_key" ON "GuildReview"("guildId", "userId");

UPDATE "Guild" AS g
SET "reviewsCount" = agg.review_count,
    "rating" = agg.average_rating
FROM (
  SELECT g2."id" AS guild_id,
         count(r."id")::int AS review_count,
         avg(r."rating")::decimal(18, 8) AS average_rating
  FROM "Guild" AS g2
  LEFT JOIN "GuildReview" AS r ON r."guildId" = g2."id"
  GROUP BY g2."id"
) AS agg
WHERE g."id" = agg.guild_id;

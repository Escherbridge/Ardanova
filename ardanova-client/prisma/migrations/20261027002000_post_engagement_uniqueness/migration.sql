-- One like and one bookmark per person per post, enforced by the database (SR-03 phase 0).
-- DESTRUCTIVE: deletes duplicate "PostLike" and "PostBookmark" rows before adding the unique
-- indexes. The released database holds 0 posts (so 0 likes and 0 bookmarks) as of 2026-10-26.
-- UNREHEARSED on any populated database. Before `migrate deploy` elsewhere, count duplicates
--   SELECT "postId","userId",count(*) FROM "PostLike"     GROUP BY 1,2 HAVING count(*)>1;
--   SELECT "postId","userId",count(*) FROM "PostBookmark" GROUP BY 1,2 HAVING count(*)>1;
-- and record the rows the DELETEs below will remove.
-- Lands AFTER 20261027001000_feed_envelope_columns.
--
-- PostService.LikeAsync (PostService.cs:266-289) checks for an existing row and then writes
-- "likesCount" as an ABSOLUTE `+= 1` with no concurrency token -- not a relative increment the
-- database could serialize. Two likes from DIFFERENT people racing on the same post both read the
-- pre-race count N, both pass the "no existing row" check (different userId), both insert their
-- row, and both write N+1: two rows land but the counter only ever reaches N+1, not N+2. Reviewer
-- repro: u1 likes p1 twice (collapses to 1 row below) and u2 likes p1 once, "likesCount" stored as
-- 2 -- after removing the one duplicate row, 2 rows remain but a removed-count decrement
-- (2 - 1 = 1) leaves the counter at 1, UNDER the correct value of 2. A recount from the surviving
-- rows is therefore the only correct fix; decrementing by rows-removed is wrong in general because
-- the race is between DIFFERENT users' first likes, not just a duplicate from one user.
--
-- The recount cannot share a statement with the DELETE: a `WITH removed AS (DELETE ... RETURNING
-- ...) UPDATE ...` CTE pipeline (this migration's previous shape) runs its UPDATE against the
-- SAME snapshot the DELETE saw, because every CTE in one statement shares one snapshot -- a
-- recount there would still count the rows the DELETE is in the middle of removing. The recount
-- below is therefore a SEPARATE statement, after the DELETE has completed earlier in this same
-- transaction, so it reads the post-DELETE row counts.
--
-- LOCK TABLE up front holds a SHARE ROW EXCLUSIVE lock on both tables for the rest of this
-- transaction: concurrent reads still work, but a like or bookmark arriving between the DELETE and
-- the CREATE UNIQUE INDEX below WAITS for this transaction instead of racing it. Without the lock,
-- a write landing in that window can insert a row the new unique index then finds a duplicate for,
-- which fails `migrate deploy` with P3009 (and leaves the environment on this partially-applied
-- migration until an operator resolves it) rather than simply waiting the extra moment.
--
-- A duplicate like or bookmark is invalid by definition, so the extras are removed first, keeping
-- each person's EARLIEST row ("createdAt", then "id" as a stable tie-break), as in
-- 20261018120000_guild_review_one_per_reviewer.
-- Bookmarks carry no counter. A re-run removes nothing and changes nothing (the recount below is
-- also idempotent: recounting posts that already match their row count is a no-op).
-- Hand-written; schema.prisma and the EF entities are regenerated from the DBML.

SET LOCAL lock_timeout = '5s';

LOCK TABLE "PostLike", "PostBookmark" IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE "_post_engagement_affected_posts" ON COMMIT DROP AS
SELECT DISTINCT later."postId"
FROM "PostLike" AS later
JOIN "PostLike" AS earlier
  ON later."postId" = earlier."postId"
  AND later."userId" = earlier."userId"
  AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");

DELETE FROM "PostLike" AS later
USING "PostLike" AS earlier
WHERE later."postId" = earlier."postId"
  AND later."userId" = earlier."userId"
  AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");

CREATE UNIQUE INDEX IF NOT EXISTS "PostLike_postId_userId_key" ON "PostLike"("postId", "userId");

-- Separate statement, so it reads the post-DELETE row counts rather than the pre-DELETE snapshot.
UPDATE "Post" AS p
SET "likesCount" = (SELECT count(*) FROM "PostLike" WHERE "postId" = p."id")
FROM "_post_engagement_affected_posts" AS affected
WHERE p."id" = affected."postId";

DELETE FROM "PostBookmark" AS later
USING "PostBookmark" AS earlier
WHERE later."postId" = earlier."postId"
  AND later."userId" = earlier."userId"
  AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");

CREATE UNIQUE INDEX IF NOT EXISTS "PostBookmark_postId_userId_key" ON "PostBookmark"("postId", "userId");

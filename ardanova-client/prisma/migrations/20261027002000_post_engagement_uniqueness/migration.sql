-- One like and one bookmark per person per post, enforced by the database (SR-03 phase 0).
-- DESTRUCTIVE: deletes duplicate "PostLike" and "PostBookmark" rows before adding the unique
-- indexes. The released database holds 0 posts (so 0 likes and 0 bookmarks) as of 2026-10-26.
-- UNREHEARSED on any populated database. Before `migrate deploy` elsewhere, count duplicates
--   SELECT "postId","userId",count(*) FROM "PostLike"     GROUP BY 1,2 HAVING count(*)>1;
--   SELECT "postId","userId",count(*) FROM "PostBookmark" GROUP BY 1,2 HAVING count(*)>1;
-- and record the rows the DELETEs below will remove.
-- Lands AFTER 20261027001000_feed_envelope_columns.
--
-- PostService.LikeAsync and BookmarkAsync check for an existing row and then insert. Two requests
-- from the same person that arrive together both pass the check, so both rows were stored and
-- LikeAsync incremented "Post"."likesCount" twice. The unique index is the only guard that holds
-- under concurrency, and feed ranking will read these counts.
--
-- A duplicate like or bookmark is invalid by definition, so the extras are removed first, keeping
-- each person's EARLIEST row ("createdAt", then "id" as a stable tie-break), as in
-- 20261018120000_guild_review_one_per_reviewer. Each removed like had added exactly one to
-- "likesCount" (LikeAsync writes the row and the increment together), so that post's counter is
-- lowered by exactly the number removed -- not recounted from rows, which would also rewrite the
-- counter of every post that never had a duplicate.
-- Bookmarks carry no counter. A re-run removes nothing and changes nothing.
-- Hand-written; schema.prisma and the EF entities are regenerated from the DBML.

WITH removed AS (
  DELETE FROM "PostLike" AS later
  USING "PostLike" AS earlier
  WHERE later."postId" = earlier."postId"
    AND later."userId" = earlier."userId"
    AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id")
  RETURNING later."postId"
),
removed_per_post AS (
  SELECT "postId", count(*)::int AS removed_count
  FROM removed
  GROUP BY "postId"
)
UPDATE "Post" AS p
SET "likesCount" = GREATEST(p."likesCount" - removed_per_post.removed_count, 0)
FROM removed_per_post
WHERE p."id" = removed_per_post."postId";

CREATE UNIQUE INDEX IF NOT EXISTS "PostLike_postId_userId_key" ON "PostLike"("postId", "userId");

DELETE FROM "PostBookmark" AS later
USING "PostBookmark" AS earlier
WHERE later."postId" = earlier."postId"
  AND later."userId" = earlier."userId"
  AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");

CREATE UNIQUE INDEX IF NOT EXISTS "PostBookmark_postId_userId_key" ON "PostBookmark"("postId", "userId");

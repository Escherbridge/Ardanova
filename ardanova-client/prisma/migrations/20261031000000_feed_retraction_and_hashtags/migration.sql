-- Feed envelope retraction state and hashtags on "Post" (SR-03 phase 2).
-- Lands AFTER 20261027003000_taxonomy_term. It touches only "Post", so it commutes with the money
-- branch's 20261028000000_funding_intent_void whichever of the two deploys first.
--
-- 1. "retractedAt" (precondition SR-03-P2-PRE). Until now a feed card whose source was withdrawn but
--    still exists (an opportunity sent back to DRAFT, an event made DRAFT / CANCELLED / INVITE_ONLY, a
--    task reopened) was HARD-deleted together with everyone's replies, likes, bookmarks and shares, and
--    re-opening the source minted a fresh card with zero engagement that recency ranking then bumped as
--    new. Withdrawal is now a state: the projector sets it, every read path filters it out, and re-entry
--    clears it on the SAME row (same id, same "createdAt", engagement intact). A card is deleted only
--    when its source row is gone. No index: every feed read already narrows by "createdAt" /
--    "projectId" / "guildId" and the retracted share of the table stays tiny.
--
-- 2. "hashtags" (owner direction 2026-10-31: light, copy-driven metadata instead of form fields).
--    Lowercased, NFKC-normalised #tags extracted from the post's own copy (for a projected envelope, from
--    its source's copy), deduped, at most 10, first-seen order - written by the API, never by the BFF.
--    The GIN index serves the feed's tag scope ("hashtags" @> ARRAY[tag]). Existing rows start with '{}':
--    projected envelopes are re-tagged by the projector's next full sweep; a person's post written before
--    this migration gains tags when it is next edited (the released database held 0 posts on 2026-10-26).
--
-- Strictly additive: two columns and one index. "retractedAt" is nullable with no default; "hashtags" is
-- NOT NULL with a constant default, which PostgreSQL 11+ stores as a catalog default (no table rewrite).
-- CREATE INDEX (not CONCURRENTLY: Prisma runs each migration in a transaction) blocks writes to "Post"
-- while it builds; with every row at '{}' the GIN build is momentary - measure it first on a populated copy.
--
-- Deploy order: this migration, THEN the API (its EF model selects both columns on every Post read and
-- fails with 42703 against a database without them), then the BFF. No enum or type changed, so the
-- Npgsql per-data-source type cache does not need a BFF restart for this migration.
--
-- Generated with `prisma migrate diff`, then guarded by hand so a partial re-run is harmless. One deliberate
-- difference: Prisma cannot declare a scalar list NOT NULL ("String[] @default([])" diffs as a nullable
-- column), so a datamodel diff will report "hashtags" NOT NULL as drift. That is expected; the API writes it
-- as a non-null array. Never "fix" it with `migrate dev` / `db push`.

SET LOCAL lock_timeout = '5s';

ALTER TABLE "Post" ADD COLUMN IF NOT EXISTS "retractedAt" TIMESTAMP(3),
ADD COLUMN IF NOT EXISTS "hashtags" TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[];

CREATE INDEX IF NOT EXISTS "Post_hashtags_idx" ON "Post" USING GIN ("hashtags");

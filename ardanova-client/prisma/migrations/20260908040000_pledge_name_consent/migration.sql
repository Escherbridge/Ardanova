-- Backer privacy (Wave 6, W6.6). Lands AFTER 20260908030000_funding_intent_exchange.
--
-- A pledge is anonymous by default. "showBackerName" is the one column a backer's own
-- checkbox writes (W6.4's "show my name to other backers" consent on the invest flow); the
-- GetInvestors action reads it per pledge to decide whether a project member sees the backer's
-- display name or "Anonymous backer" for that row. The internal user id is never returned by
-- that endpoint regardless of this flag - it only ever gates a NAME.
--
-- Defaulting to false, not backfilling true, means every pledge made before this column
-- existed - when there was no checkbox to opt in with - stays anonymous rather than being
-- outed retroactively by a column that didn't exist when they pledged.

ALTER TABLE public."ProjectInvestment"
  ADD COLUMN IF NOT EXISTS "showBackerName" BOOLEAN NOT NULL DEFAULT false;

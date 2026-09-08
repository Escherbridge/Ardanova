-- Fixed-scale money (Wave 5, W5.3). Lands AFTER 20260908010000_project_token_and_treasury.
--
-- Two changes, one idea: a figure that represents money stops being a float or an int, and a
-- row that records a move stops pretending to be the balance.
--
-- 1. TokenBalance holds BASE units. INTEGER capped a holding at ~2.1 billion base units, which
--    for a 2-decimal asset is about 21 million whole units - a cap a single project can pass.
--    BIGINT removes the cap, and the three new provenance columns say whether the figure came
--    from the chain at all: "assetId" names the asset it counts, "walletAddress" the account it
--    was seen at, "observedAt" when. Nothing stores an "is real" flag; IsChainObserved is
--    DERIVED from those three, so it cannot disagree with what was recorded.
--    They are nullable with no backfill because no existing row was ever read from a chain, and
--    inventing an observation timestamp would make internal bookkeeping look like chain truth.
--
-- 2. PlatformTreasuryTransaction becomes a RECEIPT. "amountMinorUnits" is the authoritative
--    figure - exact cents, no binary floating point; the existing "amount" DOUBLE stays as its
--    display projection so the surfaces Wave 8 still types as double keep working, and it is
--    written from the minor units, never read back into money math.
--    "balanceAfterMinorUnits" is NULLABLE on purpose: a receipt for a move only the node can
--    balance must be able to say "I do not know the resulting balance" rather than assert zero.
--    The azoa* columns are what makes a receipt verifiable - a row with no "azoaOperationId"
--    records ArdaNova's own policy split, not a value move on the node.
--
-- Backfill: "amountMinorUnits" is seeded by rounding the existing DOUBLE to cents. That is a
-- best-effort projection of a number that was never exact, which is precisely why the column
-- exists; rows written from here on are exact at the door (TreasuryService refuses an inflow
-- that is not a whole number of cents).
--
-- No index is added. These columns take part in no lookup: receipts are read by
-- "relatedProjectId" and "createdAt", which the baseline already covers.

ALTER TABLE public."TokenBalance"
  ALTER COLUMN "balance" TYPE BIGINT,
  ALTER COLUMN "lockedBalance" TYPE BIGINT;

ALTER TABLE public."TokenBalance"
  ADD COLUMN IF NOT EXISTS "assetId" TEXT,
  ADD COLUMN IF NOT EXISTS "walletAddress" TEXT,
  ADD COLUMN IF NOT EXISTS "observedAt" TIMESTAMP(3);

ALTER TABLE public."PlatformTreasuryTransaction"
  ADD COLUMN IF NOT EXISTS "amountMinorUnits" BIGINT NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "balanceAfterMinorUnits" BIGINT,
  ADD COLUMN IF NOT EXISTS "azoaOperationId" TEXT,
  ADD COLUMN IF NOT EXISTS "azoaIdempotencyKey" TEXT,
  ADD COLUMN IF NOT EXISTS "azoaReceiptReference" TEXT,
  ADD COLUMN IF NOT EXISTS "chainAssetId" TEXT,
  ADD COLUMN IF NOT EXISTS "projectTreasuryAddress" TEXT;

UPDATE public."PlatformTreasuryTransaction"
   SET "amountMinorUnits" = ROUND(("amount" * 100)::numeric)::bigint,
       "balanceAfterMinorUnits" = ROUND(("balanceAfter" * 100)::numeric)::bigint
 WHERE "amountMinorUnits" = 0;

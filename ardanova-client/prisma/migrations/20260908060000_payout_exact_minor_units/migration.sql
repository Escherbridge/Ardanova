-- Cash-out exact figures (Wave 8, W8.3).
--
-- A sale of units for money was recorded only as a rounded Int of units and a Float of dollars.
-- Neither is a figure money can be settled against: a Float cannot hold cents exactly, and the
-- Int loses everything below one whole unit. These two columns carry the EXACT amounts the
-- shared node priced and paid, in minor units, so what a person was quoted and what they were
-- paid can be compared without re-deriving either.
--
-- "ardaMinorUnits" is what the sale produced in platform funds; "usdReferenceMinorUnits" is the
-- dollar reference the person was shown at the time. Both are nullable with no default, and that
-- is the honest state of every row that exists today: no historical payout was priced this way,
-- and inventing a figure for one would make a rounded number look like a settled one.
--
-- Node operation ids are deliberately NOT new columns: they reuse conversionTxHash/payoutTxHash
-- behind honest [NotMapped] aliases on the entity, and the idempotency key is derived from the
-- row id rather than stored, so this migration stays additive and bounded to two columns.

ALTER TABLE "PayoutRequest"
    ADD COLUMN IF NOT EXISTS "ardaMinorUnits" BIGINT,
    ADD COLUMN IF NOT EXISTS "usdReferenceMinorUnits" BIGINT;

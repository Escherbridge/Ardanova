-- The receipt figures a settled sponsorship is judged by (Wave L0, L0.1).
--
-- EconomicSettlement.azoaGrossBaseUnits / azoaFeeBaseUnits / azoaNetBaseUnits are EF-mapped
-- properties written by FundingIntentService and EconomicOutboxLeaseStore, and read by
-- ProjectLedgerService when it decides whether a fee was taken. Without these columns the
-- SaveChanges that completes a dispatched sponsorship raises 42703 (undefined_column) and rolls
-- back the stake, the treasury receipt, the raised total, the gate and the settlement row --
-- while the funds themselves have already moved on the shared node, and the outbox replays the
-- same receipt forever.
--
-- TEXT, not BIGINT: these are the node's own base-unit strings, recorded exactly as the receipt
-- reported them rather than re-parsed into a width this side chose. Nullable with no default,
-- because a receipt that carried no fee figure must stay empty -- a zero would read as "the node
-- charged nothing", which is a claim no existing row can support.

ALTER TABLE public."EconomicSettlement"
  ADD COLUMN IF NOT EXISTS "azoaGrossBaseUnits" TEXT,
  ADD COLUMN IF NOT EXISTS "azoaFeeBaseUnits" TEXT,
  ADD COLUMN IF NOT EXISTS "azoaNetBaseUnits" TEXT;

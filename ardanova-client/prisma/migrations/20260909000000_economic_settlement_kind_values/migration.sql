-- Sponsorship settlement kinds the code already writes (Wave L0, L0.1).
--
-- `EconomicSettlementKind` gained EXCHANGE and HOLD_RELEASE in C# but never in PostgreSQL, so
-- every sponsorship that reached FundingIntentService.BuildExchangeSettlement wrote a label the
-- native enum does not hold: PostgreSQL raised 22P02 (invalid_text_representation), which the
-- service's conflict handler misread as a uniqueness race. The money moved; the record did not.
--
-- This migration adds ONLY the two labels. `ALTER TYPE ... ADD VALUE` cannot be followed in the
-- same transaction by a statement that USES the new value, and Prisma runs one migration
-- directory per transaction, so the columns that go with this change are a separate migration
-- (20260909001000_economic_settlement_azoa_amounts). Splitting them is not tidiness; sharing a
-- transaction is what makes this fail on deploy.
--
-- Requires PostgreSQL 12 or newer: before 12, `ALTER TYPE ... ADD VALUE` cannot run inside a
-- transaction block at all, and Prisma runs every migration file in one.

ALTER TYPE "EconomicSettlementKind" ADD VALUE IF NOT EXISTS 'EXCHANGE';

ALTER TYPE "EconomicSettlementKind" ADD VALUE IF NOT EXISTS 'HOLD_RELEASE';

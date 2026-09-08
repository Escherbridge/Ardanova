-- Sponsorship becomes an exchange (Wave 6, W6.2). Lands AFTER 20260908020000_fixed_scale_money
-- and BEFORE W6.6's *_pledge_name_consent.
--
-- One idea: a sponsorship stops being a card payment measured in cents and becomes an exchange
-- between two token amounts that were both locked before anything moved.
--
-- 1. The card rail leaves the table. "currencyCode"/"amount"/"scale" WERE the "USD"-cents
--    settlement; the four provider columns were Stripe's handles. The node terminates the payment
--    provider now, and ArdaNova never sees a card event, so keeping them would leave four columns
--    nothing can ever write - and a settlement denominated in a currency is refused at the gateway
--    rather than sent. Rows that were mid-checkout when the rail was retired can never complete, so
--    they are closed as FAILED first; nothing is deleted.
--
-- 2. Both legs and the rate are typed columns, not JSON. "platformAmountBaseUnits" is what the
--    sponsor gives up and "projectUnitQuantity" is the whole units they receive - the quantity is
--    the authoritative figure and its base units are derived from it at one place in code, so the
--    two can never drift. "requestedPlatformBaseUnits" is what they OFFERED: the idempotency key
--    covers the request, so a replay is only "different terms" when this differs, and the remainder
--    below one whole unit stays with the sponsor.
--
-- 3. "exchangeIdempotencyKey" is unique because it is the one key the sponsorship is ever settled
--    under, {avatarId}:funding-intent:{intentId}, shared with its EconomicSettlement row. The
--    synchronous attempt and the outbox worker therefore address the same node operation, and a
--    second attempt can only read back the first one's receipt.
--
-- 4. "releaseNotBefore" is the hold window (D14/Q5) surfaced as "yours after N days".
--    "settlementAttemptCount" bounds what a poll may do, and holdingReasonCode/Detail/heldAt record
--    a readiness lapse as a terminal HOLDING rather than an endless retry.
--
-- 5. The enum is recreated rather than extended: AWAITING_PAYMENT and PAYMENT_VERIFIED are card
--    states with no meaning left, and HOLDING is new. Postgres cannot drop an enum value in place.
--
-- Backfill: the new NOT NULL columns are added with placeholder defaults, backfilled, then have
-- their defaults dropped, so the application remains the only writer of a live value.

-- 1. Close every intent the retired card rail can no longer finish.
UPDATE public."FundingIntent"
   SET "status" = 'FAILED'
 WHERE "status" IN ('DRAFT', 'AWAITING_PAYMENT', 'PAYMENT_VERIFIED', 'SETTLEMENT_PENDING');

-- 2. Recreate the status enum without the card states and with HOLDING.
ALTER TYPE "FundingIntentStatus" RENAME TO "FundingIntentStatus_old";

CREATE TYPE "FundingIntentStatus" AS ENUM (
  'DRAFT', 'SETTLEMENT_PENDING', 'SETTLED', 'HOLDING', 'REJECTED', 'CANCELLED', 'FAILED');

ALTER TABLE public."FundingIntent" ALTER COLUMN "status" DROP DEFAULT;

ALTER TABLE public."FundingIntent"
  ALTER COLUMN "status" TYPE "FundingIntentStatus"
  USING "status"::text::"FundingIntentStatus";

ALTER TABLE public."FundingIntent" ALTER COLUMN "status" SET DEFAULT 'DRAFT';

DROP TYPE "FundingIntentStatus_old";

-- 3. The exchange the intent locks.
ALTER TABLE public."FundingIntent"
  ADD COLUMN IF NOT EXISTS "platformAssetCode" TEXT NOT NULL DEFAULT 'ARDA',
  ADD COLUMN IF NOT EXISTS "platformAmountBaseUnits" BIGINT NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "platformScale" INTEGER NOT NULL DEFAULT 2,
  ADD COLUMN IF NOT EXISTS "requestedPlatformBaseUnits" BIGINT NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "projectUnitAssetCode" TEXT NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS "projectUnitQuantity" BIGINT NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "projectUnitScale" INTEGER NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "projectAssetId" TEXT NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS "usdReferenceMinorUnits" BIGINT NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "declaredRateArdaPerUnit" TEXT NOT NULL DEFAULT '0',
  ADD COLUMN IF NOT EXISTS "declaredRateUsdPerUnit" TEXT NOT NULL DEFAULT '0',
  ADD COLUMN IF NOT EXISTS "declaredRateSource" TEXT NOT NULL DEFAULT 'retired:card-rail',
  ADD COLUMN IF NOT EXISTS "declaredRateDeclaredAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ADD COLUMN IF NOT EXISTS "declaredRateVersion" TEXT,
  ADD COLUMN IF NOT EXISTS "exchangeIdempotencyKey" TEXT,
  ADD COLUMN IF NOT EXISTS "azoaOperationId" TEXT,
  ADD COLUMN IF NOT EXISTS "holdReference" TEXT,
  ADD COLUMN IF NOT EXISTS "releaseNotBefore" TIMESTAMP(3),
  ADD COLUMN IF NOT EXISTS "settlementAttemptCount" INTEGER NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "holdingReasonCode" TEXT,
  ADD COLUMN IF NOT EXISTS "holdingReasonDetail" TEXT,
  ADD COLUMN IF NOT EXISTS "heldAt" TIMESTAMP(3);

-- The declared rate of a closed card-rail intent is the moment it was created, not now.
UPDATE public."FundingIntent" SET "declaredRateDeclaredAt" = "createdAt";

ALTER TABLE public."FundingIntent"
  ALTER COLUMN "platformAssetCode" DROP DEFAULT,
  ALTER COLUMN "platformAmountBaseUnits" DROP DEFAULT,
  ALTER COLUMN "platformScale" DROP DEFAULT,
  ALTER COLUMN "requestedPlatformBaseUnits" DROP DEFAULT,
  ALTER COLUMN "projectUnitAssetCode" DROP DEFAULT,
  ALTER COLUMN "projectUnitQuantity" DROP DEFAULT,
  ALTER COLUMN "projectUnitScale" DROP DEFAULT,
  ALTER COLUMN "projectAssetId" DROP DEFAULT,
  ALTER COLUMN "usdReferenceMinorUnits" DROP DEFAULT,
  ALTER COLUMN "declaredRateArdaPerUnit" DROP DEFAULT,
  ALTER COLUMN "declaredRateUsdPerUnit" DROP DEFAULT,
  ALTER COLUMN "declaredRateSource" DROP DEFAULT,
  ALTER COLUMN "declaredRateDeclaredAt" DROP DEFAULT;

-- 4. The card rail's own columns and their unique indexes go with it.
ALTER TABLE public."FundingIntent"
  DROP COLUMN IF EXISTS "currencyCode",
  DROP COLUMN IF EXISTS "amount",
  DROP COLUMN IF EXISTS "scale",
  DROP COLUMN IF EXISTS "paymentProvider",
  DROP COLUMN IF EXISTS "providerCheckoutSessionId",
  DROP COLUMN IF EXISTS "providerPaymentIntentId",
  DROP COLUMN IF EXISTS "verifiedProviderEventId",
  DROP COLUMN IF EXISTS "paymentVerifiedAt";

-- 5. One settlement key per sponsorship, enforced by the database rather than by a convention.
CREATE UNIQUE INDEX IF NOT EXISTS "FundingIntent_exchangeIdempotencyKey_key"
  ON public."FundingIntent"("exchangeIdempotencyKey");

-- Retire the orphaned card rail, and give a bid somewhere to record the guild's share.
--
-- PART 1 - the card rail (OI-58).
--
-- The card rail was retired in code long before this migration: at this HEAD there is no
-- `StripeService`, no `IStripeService`, no `StripeDtos` and no webhook controller. What survived
-- was the SHAPE of a second money path -- a payment-intent gateway registered in the container, a
-- provider-receipt table, and an idempotency column on the pledge row -- with nothing driving it.
-- That is worse than either extreme. A shape with no controls attached is an invitation: one new
-- controller action wired to the surviving gateway re-opens a complete second route into
-- `ProjectInvestment` and the token supply, outside the readiness gate, the reserve draw
-- authority, the settlement outbox and every other control this program built. None of those
-- controls know about it, because it predates them.
--
-- So the shape goes too. `StripeWebhookEvent` held provider delivery receipts for a provider this
-- deployment no longer talks to; its rows can only describe deliveries that were already processed
-- by code that no longer exists. `ProjectInvestment.stripePaymentIntentId` was the database-level
-- idempotency key for a completed card allocation -- the unique index is the whole of its value,
-- and there is no writer left to be idempotent. Dropping the column removes only the PROVIDER
-- REFERENCE: the pledge itself (`usdAmount`, `tokenAmount`, `investedAt`, the trust-protection
-- columns, the backer's name consent) is untouched, so nobody's recorded sponsorship changes.
--
-- `PayoutRequest.stripePayoutId` is deliberately NOT dropped. That is the outbound payout rail,
-- a different path with live code behind it; only the inbound card rail is retired here.
--
-- PART 2 - the guild's share of a bid award (schema half of OI-68).
--
-- A hold has exactly one beneficiary, and a bid records no split, so a paid guild bid is refused
-- at both ends today (`OpportunityBidService.DescribeUnacceptableStakeBid`,
-- `TaskCommerceService` GuildSplitUnsupported). `guildAwardSplit` is the missing fact: the part of
-- `proposedAmount` owed to the guild, in the same units and at the same precision as the award
-- itself, so the two are comparable without a conversion rule that could round value away. The
-- remainder is the individual bidder's.
--
-- Nullable with NO default and NO backfill, deliberately. NULL means "no split was declared",
-- which is exactly what is true of every bid that exists today, and it is the fail-closed reading:
-- the two refusals above still stand until L2.12 writes the code that reads this column and opens
-- the N-hold path behind its own validation (0 < guildAwardSplit < proposedAmount, whole units).
-- Adding the column does not enable anything by itself.

-- Part 1: retire the card rail.
DROP INDEX IF EXISTS "ProjectInvestment_stripePaymentIntentId_key";

ALTER TABLE public."ProjectInvestment"
  DROP COLUMN IF EXISTS "stripePaymentIntentId";

DROP TABLE IF EXISTS public."StripeWebhookEvent";

DROP TYPE IF EXISTS "StripeWebhookEventStatus";

-- Part 2: the guild's share of a bid award.
ALTER TABLE public."OpportunityBid"
  ADD COLUMN IF NOT EXISTS "guildAwardSplit" DECIMAL(18,8);

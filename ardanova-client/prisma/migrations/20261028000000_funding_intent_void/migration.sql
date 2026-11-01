-- Sponsorship request-key tombstones (money idempotency, round 3: H-1 / L-1).
-- Lands AFTER 20261027003000_taxonomy_term on main (and after 20261018120000_guild_review_one_per_reviewer
-- on branches that predate SR-03 phase 0); it touches no table those migrations do.
--
-- A sponsor's browser saves its request key BEFORE sending and resends it after a lost answer, so
-- the saved key is the lock against a second debit. Discarding it needs a server-side answer:
-- POST /api/funding-intents/requests/{key}/void. When no FundingIntent exists under
-- (funderUserId, idempotencyKey) the void writes ONE row here, and FundingIntentService refuses a
-- later create carrying that key with REQUEST_VOIDED instead of admitting it. An intent admitted
-- and then voided is not recorded here - it becomes FundingIntent.status CANCELLED, an existing
-- enum value - so this migration adds no enum value.
--
-- Strictly additive: one table, its unique index and its foreign key. No row is written, so
-- nothing needs a backfill. Every statement is guarded, so a re-run creates nothing and fails
-- nothing. Generated with `prisma migrate diff`, then guarded by hand; schema.prisma and the EF
-- entities are regenerated from the DBML.

CREATE TABLE IF NOT EXISTS "FundingIntentVoid" (
    "id" TEXT NOT NULL,
    "funderUserId" TEXT NOT NULL,
    "idempotencyKey" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "FundingIntentVoid_pkey" PRIMARY KEY ("id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "FundingIntentVoid_funderUserId_idempotencyKey_key" ON "FundingIntentVoid"("funderUserId", "idempotencyKey");

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntentVoid_funderUserId_fkey') THEN
    ALTER TABLE "FundingIntentVoid" ADD CONSTRAINT "FundingIntentVoid_funderUserId_fkey" FOREIGN KEY ("funderUserId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

-- Worker award holds (Wave 7, W7.2).
--
-- A TokenAllocation stops being only a promise in this database and becomes the READ-MODEL of
-- a hold the shared node is actually keeping. Accepting a bid opens the first hold, drawn from
-- the project's declared contributor allowance; approving the completed work continues that
-- hold into a second one — the worker's own window — so the units are theirs and restricted at
-- the same time. The node's reserve ledger, not these columns, decides whether an allowance
-- can still afford an award; these columns exist so a person can be shown their own award
-- without asking the node on every page load.
--
-- "workerHoldReleasesAt" is the end of the worker's window. That window exists so an award is
-- restricted until it is liquid, which defers the taxable event to the moment the worker can
-- realise it. It is NOT a chargeback window: reward units are drawn from the platform reserve
-- and were never bought with a card, so there is nothing to reverse.
--
-- Every column is nullable with no default, and that is the honest state of every row that
-- exists today: no allocation on record was ever committed on the node. A NULL "holdId" is
-- exactly what the payout paths refuse on (TokenAllocation.IsNodeHeld,
-- ProjectTokenService.ApproveWorkCompletionAsync), so backfilling anything here would turn
-- historical reservations into payable awards nothing backs.
ALTER TABLE public."TokenAllocation"
  ADD COLUMN IF NOT EXISTS "taskCommerceAgreementId" TEXT,
  ADD COLUMN IF NOT EXISTS "holdChain" TEXT,
  ADD COLUMN IF NOT EXISTS "reservePartition" TEXT,
  ADD COLUMN IF NOT EXISTS "holdId" TEXT,
  ADD COLUMN IF NOT EXISTS "holdState" TEXT,
  ADD COLUMN IF NOT EXISTS "workerHoldId" TEXT,
  ADD COLUMN IF NOT EXISTS "workerHoldState" TEXT,
  ADD COLUMN IF NOT EXISTS "workerHoldReleasesAt" TIMESTAMP(3),
  ADD COLUMN IF NOT EXISTS "holdSyncedAt" TIMESTAMP(3);

-- The hold id IS the idempotency anchor for a whole award: it is derived from the accepted bid,
-- so a redelivered acceptance addresses the hold it already opened. Uniqueness is what makes
-- that true under concurrency — without it two simultaneous acceptances of the same bid could
-- each write their own allocation row against one node commitment. Postgres allows many NULLs
-- in a unique index, so reservations that carry no hold are unaffected.
CREATE UNIQUE INDEX IF NOT EXISTS "TokenAllocation_holdId_key"
  ON public."TokenAllocation" ("holdId");

CREATE UNIQUE INDEX IF NOT EXISTS "TokenAllocation_workerHoldId_key"
  ON public."TokenAllocation" ("workerHoldId");

-- Reading an award back from its agreement is how the commerce surface answers "what is this
-- assignment worth and where has it got to". No foreign key is declared here on purpose: the
-- relation is not modelled in schema.prisma yet, and a database-only constraint would drift
-- from the declarative schema every later migration is diffed against.
CREATE INDEX IF NOT EXISTS "TokenAllocation_taskCommerceAgreementId_idx"
  ON public."TokenAllocation" ("taskCommerceAgreementId");

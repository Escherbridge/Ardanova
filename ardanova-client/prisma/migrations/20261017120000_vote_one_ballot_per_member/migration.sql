-- One member, one ballot, enforced by the database.
-- UNREHEARSED: not yet applied to a copy of any populated database. Before `migrate deploy`,
-- count duplicates (SELECT "proposalId","voterId",count(*) FROM "Vote" GROUP BY 1,2 HAVING count(*)>1)
-- and record the rows the DELETE below will remove; it is destructive by design.
-- Lands AFTER 20261017000000_financial_engine_domain.
--
-- GovernanceService.CastVoteAsync checks for an existing ballot and then inserts. Two requests
-- from the same member that arrive together both pass the check, and both rows were stored, so a
-- member could be counted twice in a tally that decides PASSED or REJECTED. The unique index is
-- the only guard that holds under concurrency; the service maps its violation (SQLSTATE 23505)
-- to "You have already voted on this proposal".
--
-- A duplicate ballot is invalid by definition, so any that already exist are removed first,
-- keeping each member's EARLIEST ballot (createdAt, then id as a stable tie-break). Statements
-- are guarded so a partial re-run is harmless. Hand-written; regenerate Prisma/EF artifacts from
-- the DBML at merge.

DELETE FROM "Vote" AS later
USING "Vote" AS earlier
WHERE later."proposalId" = earlier."proposalId"
  AND later."voterId" = earlier."voterId"
  AND (later."createdAt", later."id") > (earlier."createdAt", earlier."id");

CREATE UNIQUE INDEX IF NOT EXISTS "Vote_proposalId_voterId_key" ON "Vote"("proposalId", "voterId");

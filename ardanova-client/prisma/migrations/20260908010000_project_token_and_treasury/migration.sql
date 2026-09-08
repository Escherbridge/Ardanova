-- Project token and treasury (Wave 5, W5.2).
-- A project owner declares their stake at publish and a server-side job creates it on the
-- shared node, resolves the account the project's funds land in, and registers the worker
-- and sponsor allowances the node draws against. These columns hold the three facts that
-- job produces. Nothing here stores a "provisioned" flag: fundability is DERIVED from
-- assetId + treasuryAddress + partitionsDeclaredAt (ProjectTokenConfig.IsNodeProvisioned),
-- so a flag can never disagree with what was actually recorded.
--
-- Defaults make every pre-existing row correct with no backfill pass. No project token
-- config that exists today was ever created on the node, so a zero allowance and a null
-- treasury state exactly what is true of them: they are not fundable, and the allocation
-- paths refuse them rather than promising stake nothing can issue. Backfilling a non-zero
-- allowance would be inventing a node commitment that does not exist.
--
-- "unitsDisclosure" is nullable rather than defaulted: there is no honest disclosure to
-- invent on a project whose owner never wrote one. New declarations require it in the
-- service (ProjectTokenService.ValidateDeclaration), which is where a person can be told
-- what to write.
--
-- No index is added. The only lookup these columns take part in is by "projectId", which
-- already carries a unique index; an index on a provisioning timestamp would look like an
-- optimisation while changing no plan.
ALTER TABLE public."ProjectTokenConfig"
  ADD COLUMN IF NOT EXISTS "assetChain" TEXT,
  ADD COLUMN IF NOT EXISTS "treasuryChain" TEXT,
  ADD COLUMN IF NOT EXISTS "treasuryReference" TEXT,
  ADD COLUMN IF NOT EXISTS "treasuryAddress" TEXT,
  ADD COLUMN IF NOT EXISTS "treasuryResolvedAt" TIMESTAMP(3),
  ADD COLUMN IF NOT EXISTS "contributorPartitionSupply" INTEGER NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "investorPartitionSupply" INTEGER NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "partitionsDeclaredAt" TIMESTAMP(3),
  ADD COLUMN IF NOT EXISTS "unitsDisclosure" TEXT,
  ADD COLUMN IF NOT EXISTS "provisionedAt" TIMESTAMP(3),
  ADD COLUMN IF NOT EXISTS "provisionedByUserId" TEXT,
  ADD COLUMN IF NOT EXISTS "provisioningFailureReason" TEXT;

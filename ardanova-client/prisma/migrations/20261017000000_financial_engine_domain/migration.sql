-- Financial-engine domain vocabulary (merge of codex/financial-engine-domain onto main).
-- Lands AFTER 20260909100000_retire_card_rail_and_guild_split.
--
-- Strictly additive: five enums, seven tables, nullable link columns on ProjectTokenConfig,
-- EconomicSettlement, FundingIntent and TaskCommerceAgreement, their indexes and foreign keys,
-- plus CHECKs the DBML notes require (scale 0..18, leg position >= 0, canonical atom strings).
-- No column is altered or dropped, and every link stays NULL until a reviewed backfill, so
-- existing rows are untouched. Statements are guarded so a partial re-run is harmless.
--
-- Generated with `prisma migrate diff` from main's schema to the merged schema, then guarded by
-- hand. NOT yet rehearsed: see conductor/tracks/financial-engine-domain-hardening/PASSOFF.md
-- (stopping gate 2) before applying to any shared database.

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'AssetDefinitionKind') THEN
    CREATE TYPE "AssetDefinitionKind" AS ENUM ('PROJECT_UTILITY', 'ECOSYSTEM_UTILITY', 'EQUITY_RIGHT', 'REDEMPTION_RIGHT', 'FIAT');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'EquityOrRedemptionRightKind') THEN
    CREATE TYPE "EquityOrRedemptionRightKind" AS ENUM ('EQUITY', 'REDEMPTION');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'EligibilityDecisionStatus') THEN
    CREATE TYPE "EligibilityDecisionStatus" AS ENUM ('APPROVED', 'REJECTED', 'EXPIRED', 'REVOKED');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'EconomicSettlementLegKind') THEN
    CREATE TYPE "EconomicSettlementLegKind" AS ENUM ('PAYMENT', 'AWARD', 'SOURCE', 'INTERMEDIARY', 'TARGET', 'FEE', 'REFUND');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'SwapOrderStatus') THEN
    CREATE TYPE "SwapOrderStatus" AS ENUM ('DRAFT', 'AUTHORIZED', 'PENDING_SETTLEMENT', 'CONFIRMED', 'REJECTED', 'CANCELLED', 'FAILED');
  END IF;
END $$;

ALTER TABLE "ProjectTokenConfig" ADD COLUMN IF NOT EXISTS "assetDefinitionId" TEXT;

ALTER TABLE "EconomicSettlement" ADD COLUMN IF NOT EXISTS "assetDefinitionId" TEXT,
ADD COLUMN IF NOT EXISTS "eligibilityDecisionId" TEXT,
ADD COLUMN IF NOT EXISTS "equityOrRedemptionRightPolicyId" TEXT,
ADD COLUMN IF NOT EXISTS "projectTokenPolicyId" TEXT;

ALTER TABLE "FundingIntent" ADD COLUMN IF NOT EXISTS "awardAmountAtoms" TEXT,
ADD COLUMN IF NOT EXISTS "awardAssetDefinitionId" TEXT,
ADD COLUMN IF NOT EXISTS "eligibilityDecisionId" TEXT,
ADD COLUMN IF NOT EXISTS "equityOrRedemptionRightPolicyId" TEXT,
ADD COLUMN IF NOT EXISTS "paymentAssetDefinitionId" TEXT,
ADD COLUMN IF NOT EXISTS "projectTokenPolicyId" TEXT;

ALTER TABLE "TaskCommerceAgreement" ADD COLUMN IF NOT EXISTS "assetDefinitionId" TEXT,
ADD COLUMN IF NOT EXISTS "eligibilityDecisionId" TEXT,
ADD COLUMN IF NOT EXISTS "equityOrRedemptionRightPolicyId" TEXT,
ADD COLUMN IF NOT EXISTS "projectTokenPolicyId" TEXT;

CREATE TABLE IF NOT EXISTS "AssetDefinition" (
    "id" TEXT NOT NULL,
    "kind" "AssetDefinitionKind" NOT NULL,
    "chainType" TEXT NOT NULL,
    "chainNetwork" TEXT NOT NULL,
    "canonicalAssetId" TEXT NOT NULL,
    "symbol" TEXT NOT NULL,
    "displayName" TEXT NOT NULL,
    "scale" INTEGER NOT NULL,
    "supersedesAssetDefinitionId" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "AssetDefinition_pkey" PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "ProjectTokenPolicy" (
    "id" TEXT NOT NULL,
    "projectId" TEXT NOT NULL,
    "assetDefinitionId" TEXT NOT NULL,
    "version" INTEGER NOT NULL,
    "termsHash" TEXT NOT NULL,
    "allocationRules" JSONB NOT NULL,
    "effectiveFrom" TIMESTAMP(3) NOT NULL,
    "retiredAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "ProjectTokenPolicy_pkey" PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "EquityOrRedemptionRightPolicy" (
    "id" TEXT NOT NULL,
    "projectId" TEXT NOT NULL,
    "kind" "EquityOrRedemptionRightKind" NOT NULL,
    "version" INTEGER NOT NULL,
    "jurisdiction" TEXT NOT NULL,
    "disclosureVersion" TEXT NOT NULL,
    "eligibilityPolicyVersion" TEXT NOT NULL,
    "termsHash" TEXT NOT NULL,
    "termsSnapshot" JSONB NOT NULL,
    "effectiveFrom" TIMESTAMP(3) NOT NULL,
    "retiredAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "EquityOrRedemptionRightPolicy_pkey" PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "EligibilityDecision" (
    "id" TEXT NOT NULL,
    "userId" TEXT NOT NULL,
    "equityOrRedemptionRightPolicyId" TEXT NOT NULL,
    "status" "EligibilityDecisionStatus" NOT NULL,
    "evidenceDigest" TEXT NOT NULL,
    "reasonCode" TEXT NOT NULL,
    "decidedByUserId" TEXT,
    "expiresAt" TIMESTAMP(3),
    "reviewedAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "EligibilityDecision_pkey" PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "EconomicSettlementLeg" (
    "id" TEXT NOT NULL,
    "economicSettlementId" TEXT NOT NULL,
    "position" INTEGER NOT NULL,
    "kind" "EconomicSettlementLegKind" NOT NULL,
    "assetDefinitionId" TEXT NOT NULL,
    "amountAtoms" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "EconomicSettlementLeg_pkey" PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "SwapQuote" (
    "id" TEXT NOT NULL,
    "semanticKey" TEXT NOT NULL,
    "actorUserId" TEXT NOT NULL,
    "sourceAssetDefinitionId" TEXT NOT NULL,
    "ardaAssetDefinitionId" TEXT NOT NULL,
    "targetAssetDefinitionId" TEXT NOT NULL,
    "sourceProjectTokenPolicyId" TEXT NOT NULL,
    "targetProjectTokenPolicyId" TEXT NOT NULL,
    "sourceAmountAtoms" TEXT NOT NULL,
    "ardaAmountAtoms" TEXT NOT NULL,
    "targetAmountAtoms" TEXT NOT NULL,
    "liquiditySnapshot" JSONB NOT NULL,
    "gateDecisionSnapshot" JSONB NOT NULL,
    "termsHash" TEXT NOT NULL,
    "expiresAt" TIMESTAMP(3) NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "SwapQuote_pkey" PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "SwapOrder" (
    "id" TEXT NOT NULL,
    "quoteId" TEXT NOT NULL,
    "actorUserId" TEXT NOT NULL,
    "status" "SwapOrderStatus" NOT NULL DEFAULT 'DRAFT',
    "economicSettlementId" TEXT,
    "acceptedAt" TIMESTAMP(3),
    "confirmedAt" TIMESTAMP(3),
    "failureCode" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "SwapOrder_pkey" PRIMARY KEY ("id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "AssetDefinition_chainType_chainNetwork_canonicalAssetId_key" ON "AssetDefinition"("chainType", "chainNetwork", "canonicalAssetId");

CREATE UNIQUE INDEX IF NOT EXISTS "ProjectTokenPolicy_projectId_version_key" ON "ProjectTokenPolicy"("projectId", "version");

CREATE UNIQUE INDEX IF NOT EXISTS "EquityOrRedemptionRightPolicy_projectId_kind_version_key" ON "EquityOrRedemptionRightPolicy"("projectId", "kind", "version");

CREATE INDEX IF NOT EXISTS "EligibilityDecision_userId_equityOrRedemptionRightPolicyId__idx" ON "EligibilityDecision"("userId", "equityOrRedemptionRightPolicyId", "status");

CREATE INDEX IF NOT EXISTS "EligibilityDecision_expiresAt_idx" ON "EligibilityDecision"("expiresAt");

CREATE UNIQUE INDEX IF NOT EXISTS "EconomicSettlementLeg_economicSettlementId_position_key" ON "EconomicSettlementLeg"("economicSettlementId", "position");

CREATE UNIQUE INDEX IF NOT EXISTS "SwapQuote_semanticKey_key" ON "SwapQuote"("semanticKey");

CREATE INDEX IF NOT EXISTS "SwapQuote_actorUserId_expiresAt_idx" ON "SwapQuote"("actorUserId", "expiresAt");

CREATE UNIQUE INDEX IF NOT EXISTS "SwapOrder_quoteId_key" ON "SwapOrder"("quoteId");

CREATE UNIQUE INDEX IF NOT EXISTS "SwapOrder_economicSettlementId_key" ON "SwapOrder"("economicSettlementId");

CREATE INDEX IF NOT EXISTS "SwapOrder_actorUserId_status_idx" ON "SwapOrder"("actorUserId", "status");

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ProjectTokenConfig_assetDefinitionId_fkey') THEN
    ALTER TABLE "ProjectTokenConfig" ADD CONSTRAINT "ProjectTokenConfig_assetDefinitionId_fkey" FOREIGN KEY ("assetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlement_assetDefinitionId_fkey') THEN
    ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_assetDefinitionId_fkey" FOREIGN KEY ("assetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlement_projectTokenPolicyId_fkey') THEN
    ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_projectTokenPolicyId_fkey" FOREIGN KEY ("projectTokenPolicyId") REFERENCES "ProjectTokenPolicy"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlement_equityOrRedemptionRightPolicyId_fkey') THEN
    ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_equityOrRedemptionRightPolicyId_fkey" FOREIGN KEY ("equityOrRedemptionRightPolicyId") REFERENCES "EquityOrRedemptionRightPolicy"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlement_eligibilityDecisionId_fkey') THEN
    ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_eligibilityDecisionId_fkey" FOREIGN KEY ("eligibilityDecisionId") REFERENCES "EligibilityDecision"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntent_paymentAssetDefinitionId_fkey') THEN
    ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_paymentAssetDefinitionId_fkey" FOREIGN KEY ("paymentAssetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntent_awardAssetDefinitionId_fkey') THEN
    ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_awardAssetDefinitionId_fkey" FOREIGN KEY ("awardAssetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntent_projectTokenPolicyId_fkey') THEN
    ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_projectTokenPolicyId_fkey" FOREIGN KEY ("projectTokenPolicyId") REFERENCES "ProjectTokenPolicy"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntent_equityOrRedemptionRightPolicyId_fkey') THEN
    ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_equityOrRedemptionRightPolicyId_fkey" FOREIGN KEY ("equityOrRedemptionRightPolicyId") REFERENCES "EquityOrRedemptionRightPolicy"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntent_eligibilityDecisionId_fkey') THEN
    ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_eligibilityDecisionId_fkey" FOREIGN KEY ("eligibilityDecisionId") REFERENCES "EligibilityDecision"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'TaskCommerceAgreement_assetDefinitionId_fkey') THEN
    ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_assetDefinitionId_fkey" FOREIGN KEY ("assetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'TaskCommerceAgreement_projectTokenPolicyId_fkey') THEN
    ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_projectTokenPolicyId_fkey" FOREIGN KEY ("projectTokenPolicyId") REFERENCES "ProjectTokenPolicy"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'TaskCommerceAgreement_equityOrRedemptionRightPolicyId_fkey') THEN
    ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_equityOrRedemptionRightPolicyId_fkey" FOREIGN KEY ("equityOrRedemptionRightPolicyId") REFERENCES "EquityOrRedemptionRightPolicy"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'TaskCommerceAgreement_eligibilityDecisionId_fkey') THEN
    ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_eligibilityDecisionId_fkey" FOREIGN KEY ("eligibilityDecisionId") REFERENCES "EligibilityDecision"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'AssetDefinition_supersedesAssetDefinitionId_fkey') THEN
    ALTER TABLE "AssetDefinition" ADD CONSTRAINT "AssetDefinition_supersedesAssetDefinitionId_fkey" FOREIGN KEY ("supersedesAssetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ProjectTokenPolicy_projectId_fkey') THEN
    ALTER TABLE "ProjectTokenPolicy" ADD CONSTRAINT "ProjectTokenPolicy_projectId_fkey" FOREIGN KEY ("projectId") REFERENCES "Project"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ProjectTokenPolicy_assetDefinitionId_fkey') THEN
    ALTER TABLE "ProjectTokenPolicy" ADD CONSTRAINT "ProjectTokenPolicy_assetDefinitionId_fkey" FOREIGN KEY ("assetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EquityOrRedemptionRightPolicy_projectId_fkey') THEN
    ALTER TABLE "EquityOrRedemptionRightPolicy" ADD CONSTRAINT "EquityOrRedemptionRightPolicy_projectId_fkey" FOREIGN KEY ("projectId") REFERENCES "Project"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EligibilityDecision_userId_fkey') THEN
    ALTER TABLE "EligibilityDecision" ADD CONSTRAINT "EligibilityDecision_userId_fkey" FOREIGN KEY ("userId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EligibilityDecision_equityOrRedemptionRightPolicyId_fkey') THEN
    ALTER TABLE "EligibilityDecision" ADD CONSTRAINT "EligibilityDecision_equityOrRedemptionRightPolicyId_fkey" FOREIGN KEY ("equityOrRedemptionRightPolicyId") REFERENCES "EquityOrRedemptionRightPolicy"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EligibilityDecision_decidedByUserId_fkey') THEN
    ALTER TABLE "EligibilityDecision" ADD CONSTRAINT "EligibilityDecision_decidedByUserId_fkey" FOREIGN KEY ("decidedByUserId") REFERENCES "User"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlementLeg_economicSettlementId_fkey') THEN
    ALTER TABLE "EconomicSettlementLeg" ADD CONSTRAINT "EconomicSettlementLeg_economicSettlementId_fkey" FOREIGN KEY ("economicSettlementId") REFERENCES "EconomicSettlement"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlementLeg_assetDefinitionId_fkey') THEN
    ALTER TABLE "EconomicSettlementLeg" ADD CONSTRAINT "EconomicSettlementLeg_assetDefinitionId_fkey" FOREIGN KEY ("assetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_actorUserId_fkey') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_actorUserId_fkey" FOREIGN KEY ("actorUserId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_sourceAssetDefinitionId_fkey') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_sourceAssetDefinitionId_fkey" FOREIGN KEY ("sourceAssetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_ardaAssetDefinitionId_fkey') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_ardaAssetDefinitionId_fkey" FOREIGN KEY ("ardaAssetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_targetAssetDefinitionId_fkey') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_targetAssetDefinitionId_fkey" FOREIGN KEY ("targetAssetDefinitionId") REFERENCES "AssetDefinition"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_sourceProjectTokenPolicyId_fkey') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_sourceProjectTokenPolicyId_fkey" FOREIGN KEY ("sourceProjectTokenPolicyId") REFERENCES "ProjectTokenPolicy"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_targetProjectTokenPolicyId_fkey') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_targetProjectTokenPolicyId_fkey" FOREIGN KEY ("targetProjectTokenPolicyId") REFERENCES "ProjectTokenPolicy"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapOrder_quoteId_fkey') THEN
    ALTER TABLE "SwapOrder" ADD CONSTRAINT "SwapOrder_quoteId_fkey" FOREIGN KEY ("quoteId") REFERENCES "SwapQuote"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapOrder_actorUserId_fkey') THEN
    ALTER TABLE "SwapOrder" ADD CONSTRAINT "SwapOrder_actorUserId_fkey" FOREIGN KEY ("actorUserId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapOrder_economicSettlementId_fkey') THEN
    ALTER TABLE "SwapOrder" ADD CONSTRAINT "SwapOrder_economicSettlementId_fkey" FOREIGN KEY ("economicSettlementId") REFERENCES "EconomicSettlement"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

-- Invariants the DBML notes assign to the migration.
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'AssetDefinition_scale_range_check') THEN
    ALTER TABLE "AssetDefinition" ADD CONSTRAINT "AssetDefinition_scale_range_check" CHECK ("scale" BETWEEN 0 AND 18);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlementLeg_position_nonnegative_check') THEN
    ALTER TABLE "EconomicSettlementLeg" ADD CONSTRAINT "EconomicSettlementLeg_position_nonnegative_check" CHECK ("position" >= 0);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EconomicSettlementLeg_amountAtoms_canonical_check') THEN
    ALTER TABLE "EconomicSettlementLeg" ADD CONSTRAINT "EconomicSettlementLeg_amountAtoms_canonical_check" CHECK ("amountAtoms" ~ '^[1-9][0-9]*$');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_sourceAmountAtoms_canonical_check') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_sourceAmountAtoms_canonical_check" CHECK ("sourceAmountAtoms" ~ '^[1-9][0-9]*$');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_ardaAmountAtoms_canonical_check') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_ardaAmountAtoms_canonical_check" CHECK ("ardaAmountAtoms" ~ '^[1-9][0-9]*$');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'SwapQuote_targetAmountAtoms_canonical_check') THEN
    ALTER TABLE "SwapQuote" ADD CONSTRAINT "SwapQuote_targetAmountAtoms_canonical_check" CHECK ("targetAmountAtoms" ~ '^[1-9][0-9]*$');
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FundingIntent_awardAmountAtoms_canonical_check') THEN
    ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_awardAmountAtoms_canonical_check" CHECK ("awardAmountAtoms" IS NULL OR "awardAmountAtoms" ~ '^(0|[1-9][0-9]*)$');
  END IF;
END $$;

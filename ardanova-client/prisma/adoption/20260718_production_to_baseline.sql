-- CreateEnum
CREATE TYPE "EconomicSettlementKind" AS ENUM ('TASK_REWARD', 'FUNDING_ALLOCATION', 'ESCROW_REFUND', 'SWAP_SETTLEMENT');

-- CreateEnum
CREATE TYPE "EconomicSettlementStatus" AS ENUM ('DRAFT', 'AUTHORIZED', 'PENDING_DISPATCH', 'SUBMITTED', 'AWAITING_RECONCILIATION', 'CONFIRMED', 'REJECTED', 'CANCELLED', 'FAILED');

-- CreateEnum
CREATE TYPE "EconomicOutboxStatus" AS ENUM ('PENDING', 'LEASED', 'SUBMITTED', 'AWAITING_RECONCILIATION', 'COMPLETED', 'FAILED', 'CANCELLED');

-- CreateEnum
CREATE TYPE "FundingIntentStatus" AS ENUM ('DRAFT', 'AWAITING_PAYMENT', 'PAYMENT_VERIFIED', 'SETTLEMENT_PENDING', 'SETTLED', 'REJECTED', 'CANCELLED', 'FAILED');

-- CreateEnum
CREATE TYPE "TaskCommerceAgreementStatus" AS ENUM ('DRAFT', 'ACCEPTED', 'ESCROW_FUNDED', 'QUEST_LINKED', 'RELEASE_AUTHORIZED', 'SETTLEMENT_PENDING', 'SETTLED', 'CANCELLED', 'FAILED');

-- CreateEnum
CREATE TYPE "StripeWebhookEventStatus" AS ENUM ('PROCESSING', 'COMPLETED', 'FAILED');

-- AlterTable
ALTER TABLE "ProjectTokenConfig" ADD COLUMN     "assetScale" INTEGER;

-- AlterTable
ALTER TABLE "TaskEscrow" ADD COLUMN     "disputeDescription" TEXT,
ADD COLUMN     "disputeReason" TEXT,
ADD COLUMN     "disputedAt" TIMESTAMP(3),
ADD COLUMN     "disputedByUserId" TEXT;

-- AlterTable
ALTER TABLE "User" ADD COLUMN     "azoaAvatarId" TEXT,
ADD COLUMN     "azoaWalletAddress" TEXT,
ADD COLUMN     "azoaWalletId" TEXT;

-- AlterTable
ALTER TABLE "Wallet" ADD COLUMN     "verificationChain" TEXT,
ADD COLUMN     "verificationChallengeId" TEXT,
ADD COLUMN     "verificationNetwork" TEXT,
ADD COLUMN     "verifiedAt" TIMESTAMP(3);

-- CreateTable
CREATE TABLE "WalletVerificationChallenge" (
    "id" TEXT NOT NULL,
    "userId" TEXT NOT NULL,
    "walletId" TEXT NOT NULL,
    "address" TEXT NOT NULL,
    "chain" TEXT NOT NULL,
    "network" TEXT NOT NULL,
    "nonceHash" TEXT NOT NULL,
    "issuedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "expiresAt" TIMESTAMP(3) NOT NULL,
    "consumedAt" TIMESTAMP(3),
    "proofVerified" BOOLEAN,
    "signatureHash" TEXT,
    "failureCode" TEXT,

    CONSTRAINT "WalletVerificationChallenge_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "ActorAssertionReplay" (
    "jti" TEXT NOT NULL,
    "expiresAt" TIMESTAMP(3) NOT NULL,
    "consumedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "subject" TEXT NOT NULL,
    "requestTarget" TEXT NOT NULL,
    "bodySha256" TEXT NOT NULL,

    CONSTRAINT "ActorAssertionReplay_pkey" PRIMARY KEY ("jti")
);

-- CreateTable
CREATE TABLE "StripeWebhookEvent" (
    "id" TEXT NOT NULL,
    "eventType" TEXT NOT NULL,
    "status" "StripeWebhookEventStatus" NOT NULL DEFAULT 'PROCESSING',
    "attemptCount" INTEGER NOT NULL DEFAULT 1,
    "receivedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "processingLeaseExpiresAt" TIMESTAMP(3) NOT NULL,
    "completedAt" TIMESTAMP(3),
    "lastFailedAt" TIMESTAMP(3),

    CONSTRAINT "StripeWebhookEvent_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "EconomicSettlement" (
    "id" TEXT NOT NULL,
    "kind" "EconomicSettlementKind" NOT NULL,
    "status" "EconomicSettlementStatus" NOT NULL DEFAULT 'DRAFT',
    "idempotencyKey" TEXT NOT NULL,
    "externalEventId" TEXT,
    "beneficiaryUserId" TEXT NOT NULL,
    "authorizedByUserId" TEXT,
    "projectId" TEXT,
    "taskId" TEXT,
    "escrowId" TEXT,
    "assetCode" TEXT NOT NULL,
    "amount" DECIMAL(38,18) NOT NULL,
    "scale" INTEGER NOT NULL DEFAULT 18,
    "termsSnapshot" JSONB,
    "azoaOperationId" TEXT,
    "azoaReceipt" JSONB,
    "azoaReplayed" BOOLEAN,
    "failureCode" TEXT,
    "failureDetail" TEXT,
    "version" INTEGER NOT NULL DEFAULT 0,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "authorizedAt" TIMESTAMP(3),
    "submittedAt" TIMESTAMP(3),
    "confirmedAt" TIMESTAMP(3),
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "EconomicSettlement_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "EconomicOutbox" (
    "id" TEXT NOT NULL,
    "settlementId" TEXT NOT NULL,
    "status" "EconomicOutboxStatus" NOT NULL DEFAULT 'PENDING',
    "payloadVersion" INTEGER NOT NULL DEFAULT 1,
    "attemptCount" INTEGER NOT NULL DEFAULT 0,
    "availableAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "leaseToken" TEXT,
    "leaseExpiresAt" TIMESTAMP(3),
    "lastAttemptAt" TIMESTAMP(3),
    "dispatchedAt" TIMESTAMP(3),
    "reconciliationRequiredAt" TIMESTAMP(3),
    "failureCode" TEXT,
    "failureDetail" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "EconomicOutbox_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "FundingIntent" (
    "id" TEXT NOT NULL,
    "semanticKey" TEXT NOT NULL,
    "idempotencyKey" TEXT NOT NULL,
    "status" "FundingIntentStatus" NOT NULL DEFAULT 'DRAFT',
    "funderUserId" TEXT NOT NULL,
    "projectId" TEXT NOT NULL,
    "projectTokenConfigId" TEXT NOT NULL,
    "currencyCode" TEXT NOT NULL,
    "amount" DECIMAL(38,18) NOT NULL,
    "scale" INTEGER NOT NULL DEFAULT 2,
    "disclosureVersion" TEXT NOT NULL,
    "eligibilitySnapshot" JSONB NOT NULL,
    "termsSnapshot" JSONB NOT NULL,
    "termsHash" TEXT NOT NULL,
    "paymentProvider" TEXT,
    "providerCheckoutSessionId" TEXT,
    "providerPaymentIntentId" TEXT,
    "verifiedProviderEventId" TEXT,
    "settlementId" TEXT,
    "expiresAt" TIMESTAMP(3),
    "paymentVerifiedAt" TIMESTAMP(3),
    "settledAt" TIMESTAMP(3),
    "cancelledAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "FundingIntent_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "TaskCommerceAgreement" (
    "id" TEXT NOT NULL,
    "semanticKey" TEXT NOT NULL,
    "status" "TaskCommerceAgreementStatus" NOT NULL DEFAULT 'DRAFT',
    "projectId" TEXT NOT NULL,
    "taskId" TEXT NOT NULL,
    "bidId" TEXT NOT NULL,
    "contributorUserId" TEXT NOT NULL,
    "projectTokenConfigId" TEXT,
    "assetCode" TEXT NOT NULL,
    "awardAmount" DECIMAL(38,18) NOT NULL,
    "scale" INTEGER NOT NULL DEFAULT 18,
    "acceptedTermsSnapshot" JSONB NOT NULL,
    "termsHash" TEXT NOT NULL,
    "escrowId" TEXT,
    "questRunId" TEXT,
    "settlementId" TEXT,
    "acceptedAt" TIMESTAMP(3),
    "releaseAuthorizedAt" TIMESTAMP(3),
    "settledAt" TIMESTAMP(3),
    "cancelledAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "TaskCommerceAgreement_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "WalletVerificationChallenge_walletId_consumedAt_idx" ON "WalletVerificationChallenge"("walletId", "consumedAt");

-- CreateIndex
CREATE INDEX "WalletVerificationChallenge_userId_expiresAt_idx" ON "WalletVerificationChallenge"("userId", "expiresAt");

-- CreateIndex
CREATE INDEX "ActorAssertionReplay_expiresAt_jti_idx" ON "ActorAssertionReplay"("expiresAt", "jti");

-- CreateIndex
CREATE INDEX "StripeWebhookEvent_status_processingLeaseExpiresAt_idx" ON "StripeWebhookEvent"("status", "processingLeaseExpiresAt");

-- CreateIndex
CREATE UNIQUE INDEX "EconomicSettlement_idempotencyKey_key" ON "EconomicSettlement"("idempotencyKey");

-- CreateIndex
CREATE UNIQUE INDEX "EconomicSettlement_externalEventId_key" ON "EconomicSettlement"("externalEventId");

-- CreateIndex
CREATE INDEX "EconomicSettlement_beneficiaryUserId_status_idx" ON "EconomicSettlement"("beneficiaryUserId", "status");

-- CreateIndex
CREATE INDEX "EconomicSettlement_projectId_status_idx" ON "EconomicSettlement"("projectId", "status");

-- CreateIndex
CREATE INDEX "EconomicSettlement_taskId_kind_idx" ON "EconomicSettlement"("taskId", "kind");

-- CreateIndex
CREATE UNIQUE INDEX "EconomicOutbox_settlementId_key" ON "EconomicOutbox"("settlementId");

-- CreateIndex
CREATE INDEX "EconomicOutbox_status_availableAt_idx" ON "EconomicOutbox"("status", "availableAt");

-- CreateIndex
CREATE INDEX "EconomicOutbox_leaseExpiresAt_idx" ON "EconomicOutbox"("leaseExpiresAt");

-- CreateIndex
CREATE UNIQUE INDEX "FundingIntent_semanticKey_key" ON "FundingIntent"("semanticKey");

-- CreateIndex
CREATE UNIQUE INDEX "FundingIntent_providerCheckoutSessionId_key" ON "FundingIntent"("providerCheckoutSessionId");

-- CreateIndex
CREATE UNIQUE INDEX "FundingIntent_providerPaymentIntentId_key" ON "FundingIntent"("providerPaymentIntentId");

-- CreateIndex
CREATE UNIQUE INDEX "FundingIntent_verifiedProviderEventId_key" ON "FundingIntent"("verifiedProviderEventId");

-- CreateIndex
CREATE UNIQUE INDEX "FundingIntent_settlementId_key" ON "FundingIntent"("settlementId");

-- CreateIndex
CREATE INDEX "FundingIntent_funderUserId_status_idx" ON "FundingIntent"("funderUserId", "status");

-- CreateIndex
CREATE INDEX "FundingIntent_projectId_status_idx" ON "FundingIntent"("projectId", "status");

-- CreateIndex
CREATE INDEX "FundingIntent_projectTokenConfigId_status_idx" ON "FundingIntent"("projectTokenConfigId", "status");

-- CreateIndex
CREATE UNIQUE INDEX "FundingIntent_funderUserId_idempotencyKey_key" ON "FundingIntent"("funderUserId", "idempotencyKey");

-- CreateIndex
CREATE UNIQUE INDEX "TaskCommerceAgreement_semanticKey_key" ON "TaskCommerceAgreement"("semanticKey");

-- CreateIndex
CREATE UNIQUE INDEX "TaskCommerceAgreement_taskId_key" ON "TaskCommerceAgreement"("taskId");

-- CreateIndex
CREATE UNIQUE INDEX "TaskCommerceAgreement_bidId_key" ON "TaskCommerceAgreement"("bidId");

-- CreateIndex
CREATE UNIQUE INDEX "TaskCommerceAgreement_escrowId_key" ON "TaskCommerceAgreement"("escrowId");

-- CreateIndex
CREATE UNIQUE INDEX "TaskCommerceAgreement_settlementId_key" ON "TaskCommerceAgreement"("settlementId");

-- CreateIndex
CREATE INDEX "TaskCommerceAgreement_contributorUserId_status_idx" ON "TaskCommerceAgreement"("contributorUserId", "status");

-- CreateIndex
CREATE INDEX "TaskCommerceAgreement_projectId_status_idx" ON "TaskCommerceAgreement"("projectId", "status");

-- CreateIndex
CREATE INDEX "TaskCommerceAgreement_questRunId_idx" ON "TaskCommerceAgreement"("questRunId");

-- CreateIndex
CREATE UNIQUE INDEX "ProjectInvestment_stripePaymentIntentId_key" ON "ProjectInvestment"("stripePaymentIntentId");

-- CreateIndex
CREATE UNIQUE INDEX "User_azoaAvatarId_key" ON "User"("azoaAvatarId");

-- AddForeignKey
ALTER TABLE "WalletVerificationChallenge" ADD CONSTRAINT "WalletVerificationChallenge_userId_fkey" FOREIGN KEY ("userId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "WalletVerificationChallenge" ADD CONSTRAINT "WalletVerificationChallenge_walletId_fkey" FOREIGN KEY ("walletId") REFERENCES "Wallet"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_beneficiaryUserId_fkey" FOREIGN KEY ("beneficiaryUserId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_authorizedByUserId_fkey" FOREIGN KEY ("authorizedByUserId") REFERENCES "User"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_projectId_fkey" FOREIGN KEY ("projectId") REFERENCES "Project"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_taskId_fkey" FOREIGN KEY ("taskId") REFERENCES "ProjectTask"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "EconomicSettlement" ADD CONSTRAINT "EconomicSettlement_escrowId_fkey" FOREIGN KEY ("escrowId") REFERENCES "TaskEscrow"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "EconomicOutbox" ADD CONSTRAINT "EconomicOutbox_settlementId_fkey" FOREIGN KEY ("settlementId") REFERENCES "EconomicSettlement"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_funderUserId_fkey" FOREIGN KEY ("funderUserId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_projectId_fkey" FOREIGN KEY ("projectId") REFERENCES "Project"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_projectTokenConfigId_fkey" FOREIGN KEY ("projectTokenConfigId") REFERENCES "ProjectTokenConfig"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "FundingIntent" ADD CONSTRAINT "FundingIntent_settlementId_fkey" FOREIGN KEY ("settlementId") REFERENCES "EconomicSettlement"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_projectId_fkey" FOREIGN KEY ("projectId") REFERENCES "Project"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_taskId_fkey" FOREIGN KEY ("taskId") REFERENCES "ProjectTask"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_bidId_fkey" FOREIGN KEY ("bidId") REFERENCES "OpportunityBid"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_contributorUserId_fkey" FOREIGN KEY ("contributorUserId") REFERENCES "User"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_projectTokenConfigId_fkey" FOREIGN KEY ("projectTokenConfigId") REFERENCES "ProjectTokenConfig"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_escrowId_fkey" FOREIGN KEY ("escrowId") REFERENCES "TaskEscrow"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "TaskCommerceAgreement" ADD CONSTRAINT "TaskCommerceAgreement_settlementId_fkey" FOREIGN KEY ("settlementId") REFERENCES "EconomicSettlement"("id") ON DELETE SET NULL ON UPDATE CASCADE;

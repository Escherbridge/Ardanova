SET lock_timeout = '5s';
ALTER TABLE "PayoutRequest" ADD COLUMN "requestKey" TEXT, ADD COLUMN "quoteId" TEXT,
 ADD COLUMN "exchangeAdmitBefore" TIMESTAMP(3), ADD COLUMN "exchangeCommandJson" TEXT,
 ADD COLUMN "currencyCode" TEXT NOT NULL DEFAULT 'ARDA', ADD COLUMN "currencyDecimals" INTEGER NOT NULL DEFAULT 2;
CREATE UNIQUE INDEX "PayoutRequest_userId_requestKey_key" ON "PayoutRequest"("userId", "requestKey");
CREATE UNIQUE INDEX "PayoutRequest_quoteId_key" ON "PayoutRequest"("quoteId");
CREATE TABLE "PayoutSaleQuote" (
 "id" TEXT NOT NULL PRIMARY KEY, "currencyCode" TEXT NOT NULL DEFAULT 'ARDA', "currencyDecimals" INTEGER NOT NULL DEFAULT 2,
 "userId" TEXT NOT NULL, "projectTokenConfigId" TEXT NOT NULL,
 "holderClass" "TokenHolderClass" NOT NULL, "sourceUnitAmount" INTEGER NOT NULL,
 "platformMinorUnits" BIGINT NOT NULL, "usdReferenceMinorUnits" BIGINT NOT NULL,
 "commandJson" TEXT NOT NULL, "createdAt" TIMESTAMP(3) NOT NULL, "expiresAt" TIMESTAMP(3) NOT NULL,
 CONSTRAINT "PayoutSaleQuote_amounts_check" CHECK ("sourceUnitAmount" > 0 AND "platformMinorUnits" > 0 AND "expiresAt" > "createdAt")
);

CREATE TABLE "PayoutSaleVoid" (
 "id" TEXT NOT NULL PRIMARY KEY, "userId" TEXT NOT NULL, "requestKey" TEXT NOT NULL,
 "quoteId" TEXT NOT NULL, "createdAt" TIMESTAMP(3) NOT NULL
);
CREATE UNIQUE INDEX "PayoutSaleVoid_userId_requestKey_key" ON "PayoutSaleVoid"("userId", "requestKey");

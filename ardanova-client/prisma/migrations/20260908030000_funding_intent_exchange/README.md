# Owed: the matching `schema.prisma` edit

This migration is written by W6.2, which does not own `prisma/schema.prisma` (W6.6 edits it next,
for `*_pledge_name_consent`). The SQL applies on its own under `prisma migrate deploy`, but until
the model below replaces the current `FundingIntent` model and `FundingIntentStatus` enum, a
`prisma migrate dev` would generate a migration that reverts this one. Apply it verbatim.

```prisma
enum FundingIntentStatus {
  DRAFT
  SETTLEMENT_PENDING
  SETTLED
  HOLDING
  REJECTED
  CANCELLED
  FAILED
}

model FundingIntent {
  id                         String              @id @default(cuid())
  semanticKey                String              @unique
  idempotencyKey             String
  status                     FundingIntentStatus @default(DRAFT)
  funderUserId               String
  projectId                  String
  projectTokenConfigId       String
  disclosureVersion          String
  platformAssetCode          String
  platformAmountBaseUnits    BigInt
  platformScale              Int
  requestedPlatformBaseUnits BigInt
  projectUnitAssetCode       String
  projectUnitQuantity        BigInt
  projectUnitScale           Int
  projectAssetId             String
  usdReferenceMinorUnits     BigInt
  declaredRateArdaPerUnit    String
  declaredRateUsdPerUnit     String
  declaredRateSource         String
  declaredRateDeclaredAt     DateTime
  declaredRateVersion        String?
  eligibilitySnapshot        Json
  termsSnapshot              Json
  termsHash                  String
  exchangeIdempotencyKey     String?             @unique
  azoaOperationId            String?
  holdReference              String?
  releaseNotBefore           DateTime?
  settlementId               String?             @unique
  settlementAttemptCount     Int                 @default(0)
  holdingReasonCode          String?
  holdingReasonDetail        String?
  heldAt                     DateTime?
  expiresAt                  DateTime?
  settledAt                  DateTime?
  cancelledAt                DateTime?
  createdAt                  DateTime            @default(now())
  updatedAt                  DateTime            @updatedAt
  funderUser                 User                @relation("FundingIntent_funderUserId", fields: [funderUserId], references: [id])
  project                    Project             @relation("FundingIntent_projectId", fields: [projectId], references: [id])
  projectTokenConfig         ProjectTokenConfig  @relation("FundingIntent_projectTokenConfigId", fields: [projectTokenConfigId], references: [id])
  settlement                 EconomicSettlement? @relation("FundingIntent_settlementId", fields: [settlementId], references: [id])

  @@unique([funderUserId, idempotencyKey])
  @@index([funderUserId, status])
  @@index([projectId, status])
  @@index([projectTokenConfigId, status])
}
```

Note on `BigInt`: the .NET entity types these as `long`, and the Prisma client is not the writer of
any of them. If the client is ever asked to read one, `BigInt` serialises as a string in JSON, which
is the correct shape for an exact base-unit count anyway.

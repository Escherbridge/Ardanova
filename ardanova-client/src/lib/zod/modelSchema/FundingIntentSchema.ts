import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { FundingIntentStatusSchema } from '../inputTypeSchemas/FundingIntentStatusSchema'

/////////////////////////////////////////
// FUNDING INTENT SCHEMA
/////////////////////////////////////////

export const FundingIntentSchema = z.object({
  status: FundingIntentStatusSchema,
  id: z.string().cuid(),
  semanticKey: z.string(),
  idempotencyKey: z.string(),
  funderUserId: z.string(),
  projectId: z.string(),
  projectTokenConfigId: z.string(),
  disclosureVersion: z.string(),
  platformAssetCode: z.string(),
  platformAmountBaseUnits: z.bigint(),
  platformScale: z.number().int(),
  requestedPlatformBaseUnits: z.bigint(),
  projectUnitAssetCode: z.string(),
  projectUnitQuantity: z.bigint(),
  projectUnitScale: z.number().int(),
  projectAssetId: z.string(),
  usdReferenceMinorUnits: z.bigint(),
  declaredRateArdaPerUnit: z.string(),
  declaredRateUsdPerUnit: z.string(),
  declaredRateSource: z.string(),
  declaredRateDeclaredAt: z.coerce.date(),
  declaredRateVersion: z.string().nullable(),
  eligibilitySnapshot: JsonValueSchema,
  termsSnapshot: JsonValueSchema,
  termsHash: z.string(),
  exchangeIdempotencyKey: z.string().nullable(),
  azoaOperationId: z.string().nullable(),
  holdReference: z.string().nullable(),
  releaseNotBefore: z.coerce.date().nullable(),
  settlementId: z.string().nullable(),
  settlementAttemptCount: z.number().int(),
  holdingReasonCode: z.string().nullable(),
  holdingReasonDetail: z.string().nullable(),
  heldAt: z.coerce.date().nullable(),
  expiresAt: z.coerce.date().nullable(),
  settledAt: z.coerce.date().nullable(),
  cancelledAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type FundingIntent = z.infer<typeof FundingIntentSchema>

export default FundingIntentSchema;

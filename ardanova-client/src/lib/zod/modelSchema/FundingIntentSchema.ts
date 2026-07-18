import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { Prisma } from '@prisma/client'
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
  paymentAssetDefinitionId: z.string().nullable(),
  awardAssetDefinitionId: z.string().nullable(),
  awardAmountAtoms: z.string().nullable(),
  projectTokenPolicyId: z.string().nullable(),
  equityOrRedemptionRightPolicyId: z.string().nullable(),
  eligibilityDecisionId: z.string().nullable(),
  currencyCode: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'FundingIntent']"}),
  scale: z.number().int(),
  disclosureVersion: z.string(),
  eligibilitySnapshot: JsonValueSchema,
  termsSnapshot: JsonValueSchema,
  termsHash: z.string(),
  paymentProvider: z.string().nullable(),
  providerCheckoutSessionId: z.string().nullable(),
  providerPaymentIntentId: z.string().nullable(),
  verifiedProviderEventId: z.string().nullable(),
  settlementId: z.string().nullable(),
  expiresAt: z.coerce.date().nullable(),
  paymentVerifiedAt: z.coerce.date().nullable(),
  settledAt: z.coerce.date().nullable(),
  cancelledAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type FundingIntent = z.infer<typeof FundingIntentSchema>

export default FundingIntentSchema;

import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { Prisma } from '@prisma/client'
import { EconomicSettlementKindSchema } from '../inputTypeSchemas/EconomicSettlementKindSchema'
import { EconomicSettlementStatusSchema } from '../inputTypeSchemas/EconomicSettlementStatusSchema'

/////////////////////////////////////////
// ECONOMIC SETTLEMENT SCHEMA
/////////////////////////////////////////

export const EconomicSettlementSchema = z.object({
  kind: EconomicSettlementKindSchema,
  status: EconomicSettlementStatusSchema,
  id: z.string().cuid(),
  idempotencyKey: z.string(),
  externalEventId: z.string().nullable(),
  beneficiaryUserId: z.string(),
  authorizedByUserId: z.string().nullable(),
  projectId: z.string().nullable(),
  taskId: z.string().nullable(),
  escrowId: z.string().nullable(),
  assetCode: z.string(),
  assetDefinitionId: z.string().nullable(),
  projectTokenPolicyId: z.string().nullable(),
  equityOrRedemptionRightPolicyId: z.string().nullable(),
  eligibilityDecisionId: z.string().nullable(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'EconomicSettlement']"}),
  scale: z.number().int(),
  termsSnapshot: JsonValueSchema.nullable(),
  azoaOperationId: z.string().nullable(),
  azoaReceipt: JsonValueSchema.nullable(),
  azoaReplayed: z.boolean().nullable(),
  failureCode: z.string().nullable(),
  failureDetail: z.string().nullable(),
  version: z.number().int(),
  createdAt: z.coerce.date(),
  authorizedAt: z.coerce.date().nullable(),
  submittedAt: z.coerce.date().nullable(),
  confirmedAt: z.coerce.date().nullable(),
  updatedAt: z.coerce.date(),
})

export type EconomicSettlement = z.infer<typeof EconomicSettlementSchema>

export default EconomicSettlementSchema;

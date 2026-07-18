import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { Prisma } from '@prisma/client'
import { TaskCommerceAgreementStatusSchema } from '../inputTypeSchemas/TaskCommerceAgreementStatusSchema'

/////////////////////////////////////////
// TASK COMMERCE AGREEMENT SCHEMA
/////////////////////////////////////////

export const TaskCommerceAgreementSchema = z.object({
  status: TaskCommerceAgreementStatusSchema,
  id: z.string().cuid(),
  semanticKey: z.string(),
  projectId: z.string(),
  taskId: z.string(),
  bidId: z.string(),
  contributorUserId: z.string(),
  projectTokenConfigId: z.string().nullable(),
  assetDefinitionId: z.string().nullable(),
  projectTokenPolicyId: z.string().nullable(),
  equityOrRedemptionRightPolicyId: z.string().nullable(),
  eligibilityDecisionId: z.string().nullable(),
  assetCode: z.string(),
  awardAmount: z.instanceof(Prisma.Decimal, { message: "Field 'awardAmount' must be a Decimal. Location: ['Models', 'TaskCommerceAgreement']"}),
  scale: z.number().int(),
  acceptedTermsSnapshot: JsonValueSchema,
  termsHash: z.string(),
  escrowId: z.string().nullable(),
  questRunId: z.string().nullable(),
  settlementId: z.string().nullable(),
  acceptedAt: z.coerce.date().nullable(),
  releaseAuthorizedAt: z.coerce.date().nullable(),
  settledAt: z.coerce.date().nullable(),
  cancelledAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type TaskCommerceAgreement = z.infer<typeof TaskCommerceAgreementSchema>

export default TaskCommerceAgreementSchema;

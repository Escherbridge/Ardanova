import { z } from 'zod';
import { EligibilityDecisionStatusSchema } from '../inputTypeSchemas/EligibilityDecisionStatusSchema'

/////////////////////////////////////////
// ELIGIBILITY DECISION SCHEMA
/////////////////////////////////////////

export const EligibilityDecisionSchema = z.object({
  status: EligibilityDecisionStatusSchema,
  id: z.string().cuid(),
  userId: z.string(),
  equityOrRedemptionRightPolicyId: z.string(),
  evidenceDigest: z.string(),
  reasonCode: z.string(),
  decidedByUserId: z.string().nullable(),
  expiresAt: z.coerce.date().nullable(),
  reviewedAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
})

export type EligibilityDecision = z.infer<typeof EligibilityDecisionSchema>

export default EligibilityDecisionSchema;

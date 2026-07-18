import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { EquityOrRedemptionRightKindSchema } from '../inputTypeSchemas/EquityOrRedemptionRightKindSchema'

/////////////////////////////////////////
// EQUITY OR REDEMPTION RIGHT POLICY SCHEMA
/////////////////////////////////////////

export const EquityOrRedemptionRightPolicySchema = z.object({
  kind: EquityOrRedemptionRightKindSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  version: z.number().int(),
  jurisdiction: z.string(),
  disclosureVersion: z.string(),
  eligibilityPolicyVersion: z.string(),
  termsHash: z.string(),
  termsSnapshot: JsonValueSchema,
  effectiveFrom: z.coerce.date(),
  retiredAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
})

export type EquityOrRedemptionRightPolicy = z.infer<typeof EquityOrRedemptionRightPolicySchema>

export default EquityOrRedemptionRightPolicySchema;

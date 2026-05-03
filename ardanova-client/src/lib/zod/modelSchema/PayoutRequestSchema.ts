import { z } from 'zod';
import { PayoutStatusSchema } from '../inputTypeSchemas/PayoutStatusSchema'
import { TokenHolderClassSchema } from '../inputTypeSchemas/TokenHolderClassSchema'
import { ProjectGateStatusSchema } from '../inputTypeSchemas/ProjectGateStatusSchema'

/////////////////////////////////////////
// PAYOUT REQUEST SCHEMA
/////////////////////////////////////////

export const PayoutRequestSchema = z.object({
  status: PayoutStatusSchema,
  holderClass: TokenHolderClassSchema,
  gateStatusAtRequest: ProjectGateStatusSchema,
  id: z.string().cuid(),
  userId: z.string(),
  sourceProjectTokenConfigId: z.string().nullable(),
  sourceTokenAmount: z.number().int(),
  ardaTokenAmount: z.number().int().nullable(),
  usdAmount: z.number().nullable(),
  conversionTxHash: z.string().nullable(),
  payoutTxHash: z.string().nullable(),
  stripePayoutId: z.string().nullable(),
  failureReason: z.string().nullable(),
  requestedAt: z.coerce.date(),
  processedAt: z.coerce.date().nullable(),
  completedAt: z.coerce.date().nullable(),
})

export type PayoutRequest = z.infer<typeof PayoutRequestSchema>

export default PayoutRequestSchema;

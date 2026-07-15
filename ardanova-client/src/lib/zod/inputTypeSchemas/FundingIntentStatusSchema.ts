import { z } from 'zod';

export const FundingIntentStatusSchema = z.enum(['DRAFT','AWAITING_PAYMENT','PAYMENT_VERIFIED','SETTLEMENT_PENDING','SETTLED','REJECTED','CANCELLED','FAILED']);

export type FundingIntentStatusType = `${z.infer<typeof FundingIntentStatusSchema>}`

export default FundingIntentStatusSchema;

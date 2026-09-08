import { z } from 'zod';

export const FundingIntentStatusSchema = z.enum(['DRAFT','SETTLEMENT_PENDING','SETTLED','HOLDING','REJECTED','CANCELLED','FAILED']);

export type FundingIntentStatusType = `${z.infer<typeof FundingIntentStatusSchema>}`

export default FundingIntentStatusSchema;

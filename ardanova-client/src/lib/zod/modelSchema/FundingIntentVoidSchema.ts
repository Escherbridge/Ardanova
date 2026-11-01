import { z } from 'zod';

/////////////////////////////////////////
// FUNDING INTENT VOID SCHEMA
/////////////////////////////////////////

export const FundingIntentVoidSchema = z.object({
  id: z.cuid(),
  funderUserId: z.string(),
  idempotencyKey: z.string(),
  createdAt: z.coerce.date(),
})

export type FundingIntentVoid = z.infer<typeof FundingIntentVoidSchema>

export default FundingIntentVoidSchema;

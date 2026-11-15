import { z } from 'zod';

/////////////////////////////////////////
// PAYOUT SALE VOID SCHEMA
/////////////////////////////////////////

export const PayoutSaleVoidSchema = z.object({
  id: z.string(),
  userId: z.string(),
  requestKey: z.string(),
  quoteId: z.string(),
  createdAt: z.coerce.date(),
})

export type PayoutSaleVoid = z.infer<typeof PayoutSaleVoidSchema>

export default PayoutSaleVoidSchema;

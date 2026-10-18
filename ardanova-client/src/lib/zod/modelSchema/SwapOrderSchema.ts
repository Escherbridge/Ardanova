import { z } from 'zod';
import { SwapOrderStatusSchema } from '../inputTypeSchemas/SwapOrderStatusSchema'

/////////////////////////////////////////
// SWAP ORDER SCHEMA
/////////////////////////////////////////

export const SwapOrderSchema = z.object({
  status: SwapOrderStatusSchema,
  id: z.string().cuid(),
  quoteId: z.string(),
  actorUserId: z.string(),
  economicSettlementId: z.string().nullable(),
  acceptedAt: z.coerce.date().nullable(),
  confirmedAt: z.coerce.date().nullable(),
  failureCode: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type SwapOrder = z.infer<typeof SwapOrderSchema>

export default SwapOrderSchema;

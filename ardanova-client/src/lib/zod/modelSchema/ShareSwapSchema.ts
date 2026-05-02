import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { SwapStatusSchema } from '../inputTypeSchemas/SwapStatusSchema'

/////////////////////////////////////////
// SHARE SWAP SCHEMA
/////////////////////////////////////////

export const ShareSwapSchema = z.object({
  status: SwapStatusSchema,
  id: z.string().cuid(),
  userId: z.string(),
  fromShareId: z.string(),
  toShareId: z.string(),
  fromAmount: z.instanceof(Prisma.Decimal, { message: "Field 'fromAmount' must be a Decimal. Location: ['Models', 'ShareSwap']"}),
  toAmount: z.instanceof(Prisma.Decimal, { message: "Field 'toAmount' must be a Decimal. Location: ['Models', 'ShareSwap']"}),
  exchangeRate: z.instanceof(Prisma.Decimal, { message: "Field 'exchangeRate' must be a Decimal. Location: ['Models', 'ShareSwap']"}),
  fee: z.instanceof(Prisma.Decimal, { message: "Field 'fee' must be a Decimal. Location: ['Models', 'ShareSwap']"}),
  txHash: z.string().nullable(),
  createdAt: z.coerce.date(),
  completedAt: z.coerce.date().nullable(),
})

export type ShareSwap = z.infer<typeof ShareSwapSchema>

export default ShareSwapSchema;

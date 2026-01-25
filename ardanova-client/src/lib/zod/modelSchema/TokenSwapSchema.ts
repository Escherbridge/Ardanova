import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { SwapStatusSchema } from '../inputTypeSchemas/SwapStatusSchema'

/////////////////////////////////////////
// TOKEN SWAP SCHEMA
/////////////////////////////////////////

export const TokenSwapSchema = z.object({
  status: SwapStatusSchema,
  id: z.string().cuid(),
  userId: z.string(),
  fromTokenId: z.string(),
  toTokenId: z.string(),
  fromAmount: z.instanceof(Prisma.Decimal, { message: "Field 'fromAmount' must be a Decimal. Location: ['Models', 'TokenSwap']"}),
  toAmount: z.instanceof(Prisma.Decimal, { message: "Field 'toAmount' must be a Decimal. Location: ['Models', 'TokenSwap']"}),
  exchangeRate: z.instanceof(Prisma.Decimal, { message: "Field 'exchangeRate' must be a Decimal. Location: ['Models', 'TokenSwap']"}),
  fee: z.instanceof(Prisma.Decimal, { message: "Field 'fee' must be a Decimal. Location: ['Models', 'TokenSwap']"}),
  txHash: z.string().nullable(),
  createdAt: z.coerce.date(),
  completedAt: z.coerce.date().nullable(),
})

export type TokenSwap = z.infer<typeof TokenSwapSchema>

export default TokenSwapSchema;

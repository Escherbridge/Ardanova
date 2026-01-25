import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// TOKEN HOLDER SCHEMA
/////////////////////////////////////////

export const TokenHolderSchema = z.object({
  id: z.string().cuid(),
  tokenId: z.string(),
  userId: z.string(),
  balance: z.instanceof(Prisma.Decimal, { message: "Field 'balance' must be a Decimal. Location: ['Models', 'TokenHolder']"}),
  stakedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'stakedAmount' must be a Decimal. Location: ['Models', 'TokenHolder']"}),
  lockedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'lockedAmount' must be a Decimal. Location: ['Models', 'TokenHolder']"}),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type TokenHolder = z.infer<typeof TokenHolderSchema>

export default TokenHolderSchema;

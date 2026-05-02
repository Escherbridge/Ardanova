import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// SHARE HOLDER SCHEMA
/////////////////////////////////////////

export const ShareHolderSchema = z.object({
  id: z.string().cuid(),
  shareId: z.string(),
  userId: z.string(),
  balance: z.instanceof(Prisma.Decimal, { message: "Field 'balance' must be a Decimal. Location: ['Models', 'ShareHolder']"}),
  stakedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'stakedAmount' must be a Decimal. Location: ['Models', 'ShareHolder']"}),
  lockedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'lockedAmount' must be a Decimal. Location: ['Models', 'ShareHolder']"}),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ShareHolder = z.infer<typeof ShareHolderSchema>

export default ShareHolderSchema;

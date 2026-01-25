import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { VestingFrequencySchema } from '../inputTypeSchemas/VestingFrequencySchema'

/////////////////////////////////////////
// TOKEN VESTING SCHEMA
/////////////////////////////////////////

export const TokenVestingSchema = z.object({
  releaseFrequency: VestingFrequencySchema,
  id: z.string().cuid(),
  holderId: z.string(),
  totalAmount: z.instanceof(Prisma.Decimal, { message: "Field 'totalAmount' must be a Decimal. Location: ['Models', 'TokenVesting']"}),
  releasedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'releasedAmount' must be a Decimal. Location: ['Models', 'TokenVesting']"}),
  startDate: z.coerce.date(),
  cliffEnd: z.coerce.date(),
  vestingEnd: z.coerce.date(),
  createdAt: z.coerce.date(),
})

export type TokenVesting = z.infer<typeof TokenVestingSchema>

export default TokenVestingSchema;

import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { VestingFrequencySchema } from '../inputTypeSchemas/VestingFrequencySchema'

/////////////////////////////////////////
// SHARE VESTING SCHEMA
/////////////////////////////////////////

export const ShareVestingSchema = z.object({
  releaseFrequency: VestingFrequencySchema,
  id: z.string().cuid(),
  holderId: z.string(),
  totalAmount: z.instanceof(Prisma.Decimal, { message: "Field 'totalAmount' must be a Decimal. Location: ['Models', 'ShareVesting']"}),
  releasedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'releasedAmount' must be a Decimal. Location: ['Models', 'ShareVesting']"}),
  startDate: z.coerce.date(),
  cliffEnd: z.coerce.date(),
  vestingEnd: z.coerce.date(),
  createdAt: z.coerce.date(),
})

export type ShareVesting = z.infer<typeof ShareVestingSchema>

export default ShareVestingSchema;

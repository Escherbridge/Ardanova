import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { ReferralStatusSchema } from '../inputTypeSchemas/ReferralStatusSchema'

/////////////////////////////////////////
// REFERRAL SCHEMA
/////////////////////////////////////////

export const ReferralSchema = z.object({
  status: ReferralStatusSchema,
  id: z.string().cuid(),
  referrerId: z.string(),
  referredId: z.string(),
  referralCode: z.string().nullable(),
  rewardClaimed: z.boolean(),
  xpRewarded: z.number().int().nullable(),
  equityRewarded: z.instanceof(Prisma.Decimal, { message: "Field 'equityRewarded' must be a Decimal. Location: ['Models', 'Referral']"}).nullable(),
  createdAt: z.coerce.date(),
  completedAt: z.coerce.date().nullable(),
})

export type Referral = z.infer<typeof ReferralSchema>

export default ReferralSchema;

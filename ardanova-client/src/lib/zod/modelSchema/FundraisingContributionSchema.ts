import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { ContributionStatusSchema } from '../inputTypeSchemas/ContributionStatusSchema'

/////////////////////////////////////////
// FUNDRAISING CONTRIBUTION SCHEMA
/////////////////////////////////////////

export const FundraisingContributionSchema = z.object({
  status: ContributionStatusSchema,
  id: z.string().cuid(),
  fundraisingId: z.string(),
  userId: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'FundraisingContribution']"}),
  shareAmount: z.instanceof(Prisma.Decimal, { message: "Field 'shareAmount' must be a Decimal. Location: ['Models', 'FundraisingContribution']"}),
  paymentAsset: z.string(),
  txHash: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type FundraisingContribution = z.infer<typeof FundraisingContributionSchema>

export default FundraisingContributionSchema;

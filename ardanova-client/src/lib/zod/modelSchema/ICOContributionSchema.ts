import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { ContributionStatusSchema } from '../inputTypeSchemas/ContributionStatusSchema'

/////////////////////////////////////////
// ICO CONTRIBUTION SCHEMA
/////////////////////////////////////////

export const ICOContributionSchema = z.object({
  status: ContributionStatusSchema,
  id: z.string().cuid(),
  icoId: z.string(),
  userId: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'ICOContribution']"}),
  tokenAmount: z.instanceof(Prisma.Decimal, { message: "Field 'tokenAmount' must be a Decimal. Location: ['Models', 'ICOContribution']"}),
  paymentAsset: z.string(),
  txHash: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type ICOContribution = z.infer<typeof ICOContributionSchema>

export default ICOContributionSchema;

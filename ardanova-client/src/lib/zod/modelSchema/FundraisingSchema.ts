import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { FundraisingStatusSchema } from '../inputTypeSchemas/FundraisingStatusSchema'

/////////////////////////////////////////
// FUNDRAISING SCHEMA
/////////////////////////////////////////

export const FundraisingSchema = z.object({
  status: FundraisingStatusSchema,
  id: z.string().cuid(),
  shareId: z.string(),
  fundingGoal: z.instanceof(Prisma.Decimal, { message: "Field 'fundingGoal' must be a Decimal. Location: ['Models', 'Fundraising']"}),
  currentFunding: z.instanceof(Prisma.Decimal, { message: "Field 'currentFunding' must be a Decimal. Location: ['Models', 'Fundraising']"}),
  minContribution: z.instanceof(Prisma.Decimal, { message: "Field 'minContribution' must be a Decimal. Location: ['Models', 'Fundraising']"}).nullable(),
  maxContribution: z.instanceof(Prisma.Decimal, { message: "Field 'maxContribution' must be a Decimal. Location: ['Models', 'Fundraising']"}).nullable(),
  sharePrice: z.instanceof(Prisma.Decimal, { message: "Field 'sharePrice' must be a Decimal. Location: ['Models', 'Fundraising']"}),
  startDate: z.coerce.date(),
  endDate: z.coerce.date(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type Fundraising = z.infer<typeof FundraisingSchema>

export default FundraisingSchema;

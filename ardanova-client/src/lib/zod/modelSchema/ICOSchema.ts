import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { ICOStatusSchema } from '../inputTypeSchemas/ICOStatusSchema'

/////////////////////////////////////////
// ICO SCHEMA
/////////////////////////////////////////

export const ICOSchema = z.object({
  status: ICOStatusSchema,
  id: z.string().cuid(),
  tokenId: z.string(),
  fundingGoal: z.instanceof(Prisma.Decimal, { message: "Field 'fundingGoal' must be a Decimal. Location: ['Models', 'ICO']"}),
  currentFunding: z.instanceof(Prisma.Decimal, { message: "Field 'currentFunding' must be a Decimal. Location: ['Models', 'ICO']"}),
  minContribution: z.instanceof(Prisma.Decimal, { message: "Field 'minContribution' must be a Decimal. Location: ['Models', 'ICO']"}).nullable(),
  maxContribution: z.instanceof(Prisma.Decimal, { message: "Field 'maxContribution' must be a Decimal. Location: ['Models', 'ICO']"}).nullable(),
  tokenPrice: z.instanceof(Prisma.Decimal, { message: "Field 'tokenPrice' must be a Decimal. Location: ['Models', 'ICO']"}),
  startDate: z.coerce.date(),
  endDate: z.coerce.date(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ICO = z.infer<typeof ICOSchema>

export default ICOSchema;

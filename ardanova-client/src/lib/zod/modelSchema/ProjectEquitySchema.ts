import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// PROJECT EQUITY SCHEMA
/////////////////////////////////////////

export const ProjectEquitySchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  sharePercent: z.instanceof(Prisma.Decimal, { message: "Field 'sharePercent' must be a Decimal. Location: ['Models', 'ProjectEquity']"}),
  investmentAmount: z.instanceof(Prisma.Decimal, { message: "Field 'investmentAmount' must be a Decimal. Location: ['Models', 'ProjectEquity']"}),
  grantedAt: z.coerce.date(),
})

export type ProjectEquity = z.infer<typeof ProjectEquitySchema>

export default ProjectEquitySchema;

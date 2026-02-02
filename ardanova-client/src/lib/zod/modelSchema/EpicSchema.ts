import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { EpicStatusSchema } from '../inputTypeSchemas/EpicStatusSchema'
import { PrioritySchema } from '../inputTypeSchemas/PrioritySchema'

/////////////////////////////////////////
// EPIC SCHEMA
/////////////////////////////////////////

export const EpicSchema = z.object({
  status: EpicStatusSchema,
  priority: PrioritySchema,
  id: z.string().cuid(),
  milestoneId: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  equityBudget: z.instanceof(Prisma.Decimal, { message: "Field 'equityBudget' must be a Decimal. Location: ['Models', 'Epic']"}).nullable(),
  progress: z.number().int(),
  startDate: z.coerce.date().nullable(),
  targetDate: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type Epic = z.infer<typeof EpicSchema>

export default EpicSchema;

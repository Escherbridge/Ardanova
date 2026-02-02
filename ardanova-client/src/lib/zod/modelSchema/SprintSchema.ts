import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { SprintStatusSchema } from '../inputTypeSchemas/SprintStatusSchema'

/////////////////////////////////////////
// SPRINT SCHEMA
/////////////////////////////////////////

export const SprintSchema = z.object({
  status: SprintStatusSchema,
  id: z.string().cuid(),
  epicId: z.string(),
  name: z.string(),
  goal: z.string().nullable(),
  startDate: z.coerce.date(),
  endDate: z.coerce.date(),
  equityBudget: z.instanceof(Prisma.Decimal, { message: "Field 'equityBudget' must be a Decimal. Location: ['Models', 'Sprint']"}).nullable(),
  velocity: z.number().int().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type Sprint = z.infer<typeof SprintSchema>

export default SprintSchema;

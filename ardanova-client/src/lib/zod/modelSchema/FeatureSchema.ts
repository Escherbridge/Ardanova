import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { FeatureStatusSchema } from '../inputTypeSchemas/FeatureStatusSchema'
import { PrioritySchema } from '../inputTypeSchemas/PrioritySchema'

/////////////////////////////////////////
// FEATURE SCHEMA
/////////////////////////////////////////

export const FeatureSchema = z.object({
  status: FeatureStatusSchema,
  priority: PrioritySchema,
  id: z.string().cuid(),
  projectId: z.string(),
  sprintId: z.string().nullable(),
  epicId: z.string().nullable(),
  milestoneId: z.string().nullable(),
  guildId: z.string().nullable(),
  title: z.string(),
  description: z.string().nullable(),
  equityBudget: z.instanceof(Prisma.Decimal, { message: "Field 'equityBudget' must be a Decimal. Location: ['Models', 'Feature']"}).nullable(),
  order: z.number().int(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type Feature = z.infer<typeof FeatureSchema>

export default FeatureSchema;

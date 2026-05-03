import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { MilestoneStatusSchema } from '../inputTypeSchemas/MilestoneStatusSchema'
import { PrioritySchema } from '../inputTypeSchemas/PrioritySchema'

/////////////////////////////////////////
// PROJECT MILESTONE SCHEMA
/////////////////////////////////////////

export const ProjectMilestoneSchema = z.object({
  status: MilestoneStatusSchema,
  priority: PrioritySchema,
  id: z.string().cuid(),
  projectId: z.string(),
  guildId: z.string().nullable(),
  title: z.string(),
  description: z.string().nullable(),
  targetDate: z.coerce.date().nullable(),
  completedAt: z.coerce.date().nullable(),
  equityBudget: z.instanceof(Prisma.Decimal, { message: "Field 'equityBudget' must be a Decimal. Location: ['Models', 'ProjectMilestone']"}).nullable(),
  order: z.number().int(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type ProjectMilestone = z.infer<typeof ProjectMilestoneSchema>

export default ProjectMilestoneSchema;

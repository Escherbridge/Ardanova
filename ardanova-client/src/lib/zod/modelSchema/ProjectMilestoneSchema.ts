import { z } from 'zod';
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
  title: z.string(),
  description: z.string().nullable(),
  targetDate: z.coerce.date().nullable(),
  completedAt: z.coerce.date().nullable(),
  order: z.number().int(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type ProjectMilestone = z.infer<typeof ProjectMilestoneSchema>

export default ProjectMilestoneSchema;

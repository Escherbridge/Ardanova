import { z } from 'zod';

/////////////////////////////////////////
// PROJECT MILESTONE SCHEMA
/////////////////////////////////////////

export const ProjectMilestoneSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  targetDate: z.coerce.date(),
  completedAt: z.coerce.date().nullable(),
  isCompleted: z.boolean(),
  createdAt: z.coerce.date(),
})

export type ProjectMilestone = z.infer<typeof ProjectMilestoneSchema>

export default ProjectMilestoneSchema;

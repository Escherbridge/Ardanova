import { z } from 'zod';

/////////////////////////////////////////
// PROJECT FOLLOW SCHEMA
/////////////////////////////////////////

export const ProjectFollowSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  projectId: z.string(),
  notifyUpdates: z.boolean(),
  notifyMilestones: z.boolean(),
  notifyProposals: z.boolean(),
  createdAt: z.coerce.date(),
})

export type ProjectFollow = z.infer<typeof ProjectFollowSchema>

export default ProjectFollowSchema;

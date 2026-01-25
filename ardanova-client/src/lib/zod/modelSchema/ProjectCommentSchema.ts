import { z } from 'zod';

/////////////////////////////////////////
// PROJECT COMMENT SCHEMA
/////////////////////////////////////////

export const ProjectCommentSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  content: z.string(),
  parentId: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ProjectComment = z.infer<typeof ProjectCommentSchema>

export default ProjectCommentSchema;

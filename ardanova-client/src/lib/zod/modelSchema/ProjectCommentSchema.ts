import { z } from 'zod';
import { CommentTargetTypeSchema } from '../inputTypeSchemas/CommentTargetTypeSchema'

/////////////////////////////////////////
// PROJECT COMMENT SCHEMA
/////////////////////////////////////////

export const ProjectCommentSchema = z.object({
  targetType: CommentTargetTypeSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  content: z.string(),
  parentId: z.string().nullable(),
  targetId: z.string(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ProjectComment = z.infer<typeof ProjectCommentSchema>

export default ProjectCommentSchema;

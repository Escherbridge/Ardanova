import { z } from 'zod';

export const CommentTargetTypeSchema = z.enum(['PROJECT','MILESTONE','EPIC','SPRINT','FEATURE','PBI','TASK']);

export type CommentTargetTypeType = `${z.infer<typeof CommentTargetTypeSchema>}`

export default CommentTargetTypeSchema;

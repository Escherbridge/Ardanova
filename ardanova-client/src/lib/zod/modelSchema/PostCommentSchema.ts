import { z } from 'zod';

/////////////////////////////////////////
// POST COMMENT SCHEMA
/////////////////////////////////////////

export const PostCommentSchema = z.object({
  id: z.cuid(),
  postId: z.string(),
  authorId: z.string(),
  parentId: z.string().nullable(),
  content: z.string(),
  likesCount: z.number().int(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type PostComment = z.infer<typeof PostCommentSchema>

export default PostCommentSchema;

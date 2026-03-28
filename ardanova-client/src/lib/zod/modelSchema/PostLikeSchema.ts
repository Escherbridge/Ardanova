import { z } from 'zod';

/////////////////////////////////////////
// POST LIKE SCHEMA
/////////////////////////////////////////

export const PostLikeSchema = z.object({
  id: z.string().cuid(),
  postId: z.string(),
  userId: z.string(),
  createdAt: z.coerce.date(),
})

export type PostLike = z.infer<typeof PostLikeSchema>

export default PostLikeSchema;

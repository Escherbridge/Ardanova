import { z } from 'zod';

/////////////////////////////////////////
// POST BOOKMARK SCHEMA
/////////////////////////////////////////

export const PostBookmarkSchema = z.object({
  id: z.cuid(),
  postId: z.string(),
  userId: z.string(),
  createdAt: z.coerce.date(),
})

export type PostBookmark = z.infer<typeof PostBookmarkSchema>

export default PostBookmarkSchema;

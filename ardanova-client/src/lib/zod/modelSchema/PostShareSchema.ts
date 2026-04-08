import { z } from 'zod';

/////////////////////////////////////////
// POST SHARE SCHEMA
/////////////////////////////////////////

export const PostShareSchema = z.object({
  id: z.string().cuid(),
  postId: z.string(),
  userId: z.string(),
  sharedToProjectId: z.string().nullable(),
  sharedToGuildId: z.string().nullable(),
  comment: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type PostShare = z.infer<typeof PostShareSchema>

export default PostShareSchema;

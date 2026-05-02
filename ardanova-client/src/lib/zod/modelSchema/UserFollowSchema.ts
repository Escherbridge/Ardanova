import { z } from 'zod';

/////////////////////////////////////////
// USER FOLLOW SCHEMA
/////////////////////////////////////////

export const UserFollowSchema = z.object({
  id: z.string().cuid(),
  followerId: z.string(),
  followingId: z.string(),
  createdAt: z.coerce.date(),
})

export type UserFollow = z.infer<typeof UserFollowSchema>

export default UserFollowSchema;

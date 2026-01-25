import { z } from 'zod';

/////////////////////////////////////////
// USER ACHIEVEMENT SCHEMA
/////////////////////////////////////////

export const UserAchievementSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  achievementId: z.string(),
  progress: z.number().int(),
  earnedAt: z.coerce.date().nullable(),
})

export type UserAchievement = z.infer<typeof UserAchievementSchema>

export default UserAchievementSchema;

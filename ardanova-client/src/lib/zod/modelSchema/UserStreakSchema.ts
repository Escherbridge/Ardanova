import { z } from 'zod';
import { StreakTypeSchema } from '../inputTypeSchemas/StreakTypeSchema'

/////////////////////////////////////////
// USER STREAK SCHEMA
/////////////////////////////////////////

export const UserStreakSchema = z.object({
  streakType: StreakTypeSchema,
  id: z.string().cuid(),
  userId: z.string(),
  currentStreak: z.number().int(),
  longestStreak: z.number().int(),
  lastActivityDate: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type UserStreak = z.infer<typeof UserStreakSchema>

export default UserStreakSchema;

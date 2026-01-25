import { z } from 'zod';
import { LeaderboardPeriodSchema } from '../inputTypeSchemas/LeaderboardPeriodSchema'
import { LeaderboardCategorySchema } from '../inputTypeSchemas/LeaderboardCategorySchema'

/////////////////////////////////////////
// LEADERBOARD SCHEMA
/////////////////////////////////////////

export const LeaderboardSchema = z.object({
  period: LeaderboardPeriodSchema,
  category: LeaderboardCategorySchema,
  id: z.string().cuid(),
  startDate: z.coerce.date(),
  endDate: z.coerce.date(),
  createdAt: z.coerce.date(),
})

export type Leaderboard = z.infer<typeof LeaderboardSchema>

export default LeaderboardSchema;

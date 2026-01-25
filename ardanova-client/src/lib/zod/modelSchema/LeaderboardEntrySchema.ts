import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'

/////////////////////////////////////////
// LEADERBOARD ENTRY SCHEMA
/////////////////////////////////////////

export const LeaderboardEntrySchema = z.object({
  id: z.string().cuid(),
  leaderboardId: z.string(),
  userId: z.string(),
  rank: z.number().int(),
  score: z.number().int(),
  metadata: JsonValueSchema.nullable(),
})

export type LeaderboardEntry = z.infer<typeof LeaderboardEntrySchema>

export default LeaderboardEntrySchema;

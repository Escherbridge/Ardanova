import { z } from 'zod';

/////////////////////////////////////////
// GUILD REVIEW SCHEMA
/////////////////////////////////////////

export const GuildReviewSchema = z.object({
  id: z.string().cuid(),
  guildId: z.string(),
  projectId: z.string().nullable(),
  userId: z.string(),
  rating: z.number().int(),
  comment: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type GuildReview = z.infer<typeof GuildReviewSchema>

export default GuildReviewSchema;

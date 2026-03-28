import { z } from 'zod';

/////////////////////////////////////////
// GUILD FOLLOW SCHEMA
/////////////////////////////////////////

export const GuildFollowSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  guildId: z.string(),
  notifyUpdates: z.boolean(),
  notifyEvents: z.boolean(),
  notifyProjects: z.boolean(),
  createdAt: z.coerce.date(),
})

export type GuildFollow = z.infer<typeof GuildFollowSchema>

export default GuildFollowSchema;

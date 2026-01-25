import { z } from 'zod';

/////////////////////////////////////////
// GUILD MEMBER SCHEMA
/////////////////////////////////////////

export const GuildMemberSchema = z.object({
  id: z.string().cuid(),
  guildId: z.string(),
  userId: z.string(),
  role: z.string(),
  joinedAt: z.coerce.date(),
})

export type GuildMember = z.infer<typeof GuildMemberSchema>

export default GuildMemberSchema;

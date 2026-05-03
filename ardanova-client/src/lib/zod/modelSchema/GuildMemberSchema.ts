import { z } from 'zod';
import { GuildMemberRoleSchema } from '../inputTypeSchemas/GuildMemberRoleSchema'

/////////////////////////////////////////
// GUILD MEMBER SCHEMA
/////////////////////////////////////////

export const GuildMemberSchema = z.object({
  role: GuildMemberRoleSchema,
  id: z.string().cuid(),
  guildId: z.string(),
  userId: z.string(),
  joinedAt: z.coerce.date(),
})

export type GuildMember = z.infer<typeof GuildMemberSchema>

export default GuildMemberSchema;

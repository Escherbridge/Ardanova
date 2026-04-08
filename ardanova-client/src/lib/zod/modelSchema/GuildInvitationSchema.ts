import { z } from 'zod';
import { GuildMemberRoleSchema } from '../inputTypeSchemas/GuildMemberRoleSchema'
import { InvitationStatusSchema } from '../inputTypeSchemas/InvitationStatusSchema'

/////////////////////////////////////////
// GUILD INVITATION SCHEMA
/////////////////////////////////////////

export const GuildInvitationSchema = z.object({
  role: GuildMemberRoleSchema,
  status: InvitationStatusSchema,
  id: z.string().cuid(),
  guildId: z.string(),
  invitedById: z.string(),
  invitedUserId: z.string().nullable(),
  invitedEmail: z.string().nullable(),
  message: z.string().nullable(),
  token: z.string().nullable(),
  expiresAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  respondedAt: z.coerce.date().nullable(),
})

export type GuildInvitation = z.infer<typeof GuildInvitationSchema>

export default GuildInvitationSchema;

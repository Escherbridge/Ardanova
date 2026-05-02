import { z } from 'zod';
import { GuildMemberRoleSchema } from '../inputTypeSchemas/GuildMemberRoleSchema'
import { MembershipRequestStatusSchema } from '../inputTypeSchemas/MembershipRequestStatusSchema'

/////////////////////////////////////////
// GUILD APPLICATION SCHEMA
/////////////////////////////////////////

export const GuildApplicationSchema = z.object({
  requestedRole: GuildMemberRoleSchema,
  status: MembershipRequestStatusSchema,
  id: z.string().cuid(),
  guildId: z.string(),
  userId: z.string(),
  message: z.string(),
  skills: z.string().nullable(),
  experience: z.string().nullable(),
  portfolio: z.string().nullable(),
  availability: z.string().nullable(),
  reviewedById: z.string().nullable(),
  reviewMessage: z.string().nullable(),
  appliedAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
})

export type GuildApplication = z.infer<typeof GuildApplicationSchema>

export default GuildApplicationSchema;

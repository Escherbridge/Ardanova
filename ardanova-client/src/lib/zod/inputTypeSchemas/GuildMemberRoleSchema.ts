import { z } from 'zod';

export const GuildMemberRoleSchema = z.enum(['OWNER','ADMIN','MANAGER','MEMBER','APPRENTICE']);

export type GuildMemberRoleType = `${z.infer<typeof GuildMemberRoleSchema>}`

export default GuildMemberRoleSchema;

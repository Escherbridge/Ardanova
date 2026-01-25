import { z } from 'zod';

export const UserTypeSchema = z.enum(['INNOVATOR','SUPPORTER','VOLUNTEER','FREELANCER','SME_OWNER','GUILD_MEMBER']);

export type UserTypeType = `${z.infer<typeof UserTypeSchema>}`

export default UserTypeSchema;

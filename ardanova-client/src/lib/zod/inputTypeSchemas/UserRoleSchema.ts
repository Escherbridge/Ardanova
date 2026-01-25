import { z } from 'zod';

export const UserRoleSchema = z.enum(['INDIVIDUAL','GUILD','ADMIN']);

export type UserRoleType = `${z.infer<typeof UserRoleSchema>}`

export default UserRoleSchema;

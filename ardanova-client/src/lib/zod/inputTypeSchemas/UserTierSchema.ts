import { z } from 'zod';

export const UserTierSchema = z.enum(['BRONZE','SILVER','GOLD','PLATINUM','DIAMOND']);

export type UserTierType = `${z.infer<typeof UserTierSchema>}`

export default UserTierSchema;

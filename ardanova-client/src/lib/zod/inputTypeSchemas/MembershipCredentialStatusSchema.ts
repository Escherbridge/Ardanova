import { z } from 'zod';

export const MembershipCredentialStatusSchema = z.enum(['ACTIVE','REVOKED','SUSPENDED']);

export type MembershipCredentialStatusType = `${z.infer<typeof MembershipCredentialStatusSchema>}`

export default MembershipCredentialStatusSchema;

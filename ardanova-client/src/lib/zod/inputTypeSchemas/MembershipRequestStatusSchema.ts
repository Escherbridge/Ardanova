import { z } from 'zod';

export const MembershipRequestStatusSchema = z.enum(['PENDING','APPROVED','REJECTED','WITHDRAWN']);

export type MembershipRequestStatusType = `${z.infer<typeof MembershipRequestStatusSchema>}`

export default MembershipRequestStatusSchema;

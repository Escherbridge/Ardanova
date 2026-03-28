import { z } from 'zod';

export const InvitationStatusSchema = z.enum(['PENDING','ACCEPTED','DECLINED','EXPIRED','CANCELLED']);

export type InvitationStatusType = `${z.infer<typeof InvitationStatusSchema>}`

export default InvitationStatusSchema;

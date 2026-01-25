import { z } from 'zod';

export const ReferralStatusSchema = z.enum(['PENDING','COMPLETED','EXPIRED','CANCELLED']);

export type ReferralStatusType = `${z.infer<typeof ReferralStatusSchema>}`

export default ReferralStatusSchema;

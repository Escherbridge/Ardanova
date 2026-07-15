import { z } from 'zod';

export const EconomicOutboxStatusSchema = z.enum(['PENDING','LEASED','SUBMITTED','AWAITING_RECONCILIATION','COMPLETED','FAILED','CANCELLED']);

export type EconomicOutboxStatusType = `${z.infer<typeof EconomicOutboxStatusSchema>}`

export default EconomicOutboxStatusSchema;

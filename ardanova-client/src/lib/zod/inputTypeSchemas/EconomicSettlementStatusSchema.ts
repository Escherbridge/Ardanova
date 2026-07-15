import { z } from 'zod';

export const EconomicSettlementStatusSchema = z.enum(['DRAFT','AUTHORIZED','PENDING_DISPATCH','SUBMITTED','AWAITING_RECONCILIATION','CONFIRMED','REJECTED','CANCELLED','FAILED']);

export type EconomicSettlementStatusType = `${z.infer<typeof EconomicSettlementStatusSchema>}`

export default EconomicSettlementStatusSchema;

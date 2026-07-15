import { z } from 'zod';

export const EconomicSettlementKindSchema = z.enum(['TASK_REWARD','FUNDING_ALLOCATION','ESCROW_REFUND','SWAP_SETTLEMENT']);

export type EconomicSettlementKindType = `${z.infer<typeof EconomicSettlementKindSchema>}`

export default EconomicSettlementKindSchema;

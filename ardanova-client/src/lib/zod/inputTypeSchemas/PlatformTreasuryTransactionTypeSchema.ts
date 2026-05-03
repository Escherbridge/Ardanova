import { z } from 'zod';

export const PlatformTreasuryTransactionTypeSchema = z.enum(['FUNDING_INFLOW','ALLOCATION_INDEX','ALLOCATION_LIQUID','ALLOCATION_OPS','PAYOUT_DEBIT','INDEX_RETURN','PROFIT_SHARE','REBALANCE','TRUST_PROTECTION','FOUNDER_BURN']);

export type PlatformTreasuryTransactionTypeType = `${z.infer<typeof PlatformTreasuryTransactionTypeSchema>}`

export default PlatformTreasuryTransactionTypeSchema;

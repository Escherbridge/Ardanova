import { z } from 'zod';

export const EconomicSettlementLegKindSchema = z.enum(['PAYMENT','AWARD','SOURCE','INTERMEDIARY','TARGET','FEE','REFUND']);

export type EconomicSettlementLegKindType = `${z.infer<typeof EconomicSettlementLegKindSchema>}`

export default EconomicSettlementLegKindSchema;

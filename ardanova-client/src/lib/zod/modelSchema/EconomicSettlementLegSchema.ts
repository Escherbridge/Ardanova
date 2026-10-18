import { z } from 'zod';
import { EconomicSettlementLegKindSchema } from '../inputTypeSchemas/EconomicSettlementLegKindSchema'

/////////////////////////////////////////
// ECONOMIC SETTLEMENT LEG SCHEMA
/////////////////////////////////////////

export const EconomicSettlementLegSchema = z.object({
  kind: EconomicSettlementLegKindSchema,
  id: z.string().cuid(),
  economicSettlementId: z.string(),
  position: z.number().int(),
  assetDefinitionId: z.string(),
  amountAtoms: z.string(),
  createdAt: z.coerce.date(),
})

export type EconomicSettlementLeg = z.infer<typeof EconomicSettlementLegSchema>

export default EconomicSettlementLegSchema;

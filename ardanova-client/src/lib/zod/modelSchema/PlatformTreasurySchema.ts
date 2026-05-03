import { z } from 'zod';

/////////////////////////////////////////
// PLATFORM TREASURY SCHEMA
/////////////////////////////////////////

export const PlatformTreasurySchema = z.object({
  id: z.string().cuid(),
  singletonKey: z.string(),
  ardaTotalSupply: z.bigint(),
  ardaCirculatingSupply: z.bigint(),
  ardaAssetId: z.string().nullable(),
  ardaMintTxHash: z.string().nullable(),
  indexFundBalance: z.number(),
  liquidReserveBalance: z.number(),
  operationsBalance: z.number(),
  indexFundAllocationPct: z.number(),
  liquidReserveAllocationPct: z.number(),
  operationsAllocationPct: z.number(),
  indexFundAnnualReturn: z.number(),
  platformProfitSharePct: z.number(),
  trustProtectionRate: z.number(),
  totalInflows: z.number(),
  totalPayouts: z.number(),
  totalRebalanceTransfers: z.number(),
  lastRebalanceAt: z.coerce.date().nullable(),
  lastReconciliationAt: z.coerce.date().nullable(),
  updatedAt: z.coerce.date(),
})

export type PlatformTreasury = z.infer<typeof PlatformTreasurySchema>

export default PlatformTreasurySchema;

import { z } from 'zod';

export const PlatformTreasuryScalarFieldEnumSchema = z.enum(['id','singletonKey','ardaTotalSupply','ardaCirculatingSupply','ardaAssetId','ardaMintTxHash','indexFundBalance','liquidReserveBalance','operationsBalance','indexFundAllocationPct','liquidReserveAllocationPct','operationsAllocationPct','indexFundAnnualReturn','platformProfitSharePct','trustProtectionRate','totalInflows','totalPayouts','totalRebalanceTransfers','lastRebalanceAt','lastReconciliationAt','updatedAt']);

export default PlatformTreasuryScalarFieldEnumSchema;

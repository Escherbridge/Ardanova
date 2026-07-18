import { z } from 'zod';

export const EconomicSettlementLegScalarFieldEnumSchema = z.enum(['id','economicSettlementId','position','kind','assetDefinitionId','amountAtoms','createdAt']);

export default EconomicSettlementLegScalarFieldEnumSchema;

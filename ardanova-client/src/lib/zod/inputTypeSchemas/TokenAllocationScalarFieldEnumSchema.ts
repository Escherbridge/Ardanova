import { z } from 'zod';

export const TokenAllocationScalarFieldEnumSchema = z.enum(['id','projectTokenConfigId','pbiId','recipientUserId','equityPercentage','tokenAmount','status','holderClass','isLiquid','distributedAt','distributionTxHash','burnedAt','createdAt','updatedAt']);

export default TokenAllocationScalarFieldEnumSchema;

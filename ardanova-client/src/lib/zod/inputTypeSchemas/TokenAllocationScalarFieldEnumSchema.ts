import { z } from 'zod';

export const TokenAllocationScalarFieldEnumSchema = z.enum(['id','projectTokenConfigId','pbiId','recipientUserId','equityPercentage','tokenAmount','status','holderClass','isLiquid','distributedAt','distributionTxHash','burnedAt','taskCommerceAgreementId','holdChain','reservePartition','holdId','holdState','workerHoldId','workerHoldState','workerHoldReleasesAt','holdSyncedAt','createdAt','updatedAt']);

export default TokenAllocationScalarFieldEnumSchema;

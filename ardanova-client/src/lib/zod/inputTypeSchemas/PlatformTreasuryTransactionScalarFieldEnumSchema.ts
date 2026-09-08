import { z } from 'zod';

export const PlatformTreasuryTransactionScalarFieldEnumSchema = z.enum(['id','type','amount','fromBucket','toBucket','relatedProjectId','relatedPayoutRequestId','description','balanceAfter','amountMinorUnits','balanceAfterMinorUnits','azoaOperationId','azoaIdempotencyKey','azoaReceiptReference','chainAssetId','projectTreasuryAddress','createdAt']);

export default PlatformTreasuryTransactionScalarFieldEnumSchema;

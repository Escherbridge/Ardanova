import { z } from 'zod';

export const TokenAllocationScalarFieldEnumSchema = z.enum([
  'id',
  'projectTokenConfigId',
  'taskId',
  'recipientUserId',
  'equityPercentage',
  'tokenAmount',
  'status',
  'holderClass',
  'isLiquid',
  'distributedAt',
  'distributionTxHash',
  'burnedAt',
  'createdAt',
  'updatedAt',
]);

export default TokenAllocationScalarFieldEnumSchema;

import { z } from 'zod';

export const PlatformTreasuryTransactionScalarFieldEnumSchema = z.enum([
  'id',
  'type',
  'amount',
  'fromBucket',
  'toBucket',
  'relatedProjectId',
  'relatedPayoutRequestId',
  'description',
  'balanceAfter',
  'createdAt',
]);

export default PlatformTreasuryTransactionScalarFieldEnumSchema;

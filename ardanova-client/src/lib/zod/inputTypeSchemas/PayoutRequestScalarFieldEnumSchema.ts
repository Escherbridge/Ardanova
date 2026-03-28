import { z } from 'zod';

export const PayoutRequestScalarFieldEnumSchema = z.enum([
  'id',
  'userId',
  'sourceProjectTokenConfigId',
  'sourceTokenAmount',
  'ardaTokenAmount',
  'usdAmount',
  'status',
  'holderClass',
  'gateStatusAtRequest',
  'conversionTxHash',
  'payoutTxHash',
  'stripePayoutId',
  'failureReason',
  'requestedAt',
  'processedAt',
  'completedAt',
]);

export default PayoutRequestScalarFieldEnumSchema;

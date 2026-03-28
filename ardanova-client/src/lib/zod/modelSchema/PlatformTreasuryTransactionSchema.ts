import { z } from 'zod';
import { PlatformTreasuryTransactionTypeSchema } from '../inputTypeSchemas/PlatformTreasuryTransactionTypeSchema';

/////////////////////////////////////////
// PLATFORM TREASURY TRANSACTION SCHEMA
/////////////////////////////////////////

export const PlatformTreasuryTransactionSchema = z.object({
  id: z.string().cuid(),
  type: PlatformTreasuryTransactionTypeSchema,
  amount: z.number(),
  fromBucket: z.string().nullable(),
  toBucket: z.string().nullable(),
  relatedProjectId: z.string().nullable(),
  relatedPayoutRequestId: z.string().nullable(),
  description: z.string().nullable(),
  balanceAfter: z.number(),
  createdAt: z.coerce.date(),
});

export type PlatformTreasuryTransaction = z.infer<typeof PlatformTreasuryTransactionSchema>;

export default PlatformTreasuryTransactionSchema;

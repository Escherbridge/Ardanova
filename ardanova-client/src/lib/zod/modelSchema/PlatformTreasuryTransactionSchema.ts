import { z } from 'zod';
import { PlatformTreasuryTransactionTypeSchema } from '../inputTypeSchemas/PlatformTreasuryTransactionTypeSchema'

/////////////////////////////////////////
// PLATFORM TREASURY TRANSACTION SCHEMA
/////////////////////////////////////////

export const PlatformTreasuryTransactionSchema = z.object({
  type: PlatformTreasuryTransactionTypeSchema,
  id: z.string().cuid(),
  amount: z.number(),
  fromBucket: z.string().nullable(),
  toBucket: z.string().nullable(),
  relatedProjectId: z.string().nullable(),
  relatedPayoutRequestId: z.string().nullable(),
  description: z.string().nullable(),
  balanceAfter: z.number(),
  amountMinorUnits: z.bigint(),
  balanceAfterMinorUnits: z.bigint().nullable(),
  azoaOperationId: z.string().nullable(),
  azoaIdempotencyKey: z.string().nullable(),
  azoaReceiptReference: z.string().nullable(),
  chainAssetId: z.string().nullable(),
  projectTreasuryAddress: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type PlatformTreasuryTransaction = z.infer<typeof PlatformTreasuryTransactionSchema>

export default PlatformTreasuryTransactionSchema;

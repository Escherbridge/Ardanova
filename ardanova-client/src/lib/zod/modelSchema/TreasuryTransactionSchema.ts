import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { TransactionTypeSchema } from '../inputTypeSchemas/TransactionTypeSchema'

/////////////////////////////////////////
// TREASURY TRANSACTION SCHEMA
/////////////////////////////////////////

export const TreasuryTransactionSchema = z.object({
  type: TransactionTypeSchema,
  id: z.string().cuid(),
  treasuryId: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'TreasuryTransaction']"}),
  description: z.string().nullable(),
  txHash: z.string().nullable(),
  proposalId: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type TreasuryTransaction = z.infer<typeof TreasuryTransactionSchema>

export default TreasuryTransactionSchema;

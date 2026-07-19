import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { EscrowStatusSchema } from '../inputTypeSchemas/EscrowStatusSchema'

/////////////////////////////////////////
// TASK ESCROW SCHEMA
/////////////////////////////////////////

export const TaskEscrowSchema = z.object({
  status: EscrowStatusSchema,
  id: z.string().cuid(),
  taskId: z.string(),
  funderId: z.string(),
  shareId: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'TaskEscrow']"}),
  txHashFund: z.string().nullable(),
  txHashRelease: z.string().nullable(),
  txHashRefund: z.string().nullable(),
  disputeReason: z.string().nullable(),
  disputeDescription: z.string().nullable(),
  disputedByUserId: z.string().nullable(),
  createdAt: z.coerce.date(),
  fundedAt: z.coerce.date().nullable(),
  releasedAt: z.coerce.date().nullable(),
  refundedAt: z.coerce.date().nullable(),
  disputedAt: z.coerce.date().nullable(),
})

export type TaskEscrow = z.infer<typeof TaskEscrowSchema>

export default TaskEscrowSchema;

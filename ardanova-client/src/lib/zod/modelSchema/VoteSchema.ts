import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// VOTE SCHEMA
/////////////////////////////////////////

export const VoteSchema = z.object({
  id: z.string().cuid(),
  proposalId: z.string(),
  voterId: z.string(),
  choice: z.number().int(),
  weight: z.instanceof(Prisma.Decimal, { message: "Field 'weight' must be a Decimal. Location: ['Models', 'Vote']"}),
  reason: z.string().nullable(),
  txHash: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type Vote = z.infer<typeof VoteSchema>

export default VoteSchema;

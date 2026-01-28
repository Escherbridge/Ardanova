import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// DELEGATED VOTE SCHEMA
/////////////////////////////////////////

export const DelegatedVoteSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  delegatorId: z.string(),
  delegateeId: z.string(),
  shareId: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'DelegatedVote']"}),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
  expiresAt: z.coerce.date().nullable(),
  revokedAt: z.coerce.date().nullable(),
})

export type DelegatedVote = z.infer<typeof DelegatedVoteSchema>

export default DelegatedVoteSchema;

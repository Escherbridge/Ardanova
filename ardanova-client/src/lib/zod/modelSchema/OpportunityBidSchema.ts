import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { BidStatusSchema } from '../inputTypeSchemas/BidStatusSchema'

/////////////////////////////////////////
// OPPORTUNITY BID SCHEMA
/////////////////////////////////////////

export const OpportunityBidSchema = z.object({
  status: BidStatusSchema,
  id: z.string().cuid(),
  opportunityId: z.string(),
  bidderId: z.string(),
  guildId: z.string().nullable(),
  proposedAmount: z.instanceof(Prisma.Decimal, { message: "Field 'proposedAmount' must be a Decimal. Location: ['Models', 'OpportunityBid']"}).nullable(),
  proposal: z.string(),
  estimatedHours: z.number().int().nullable(),
  timeline: z.string().nullable(),
  deliverables: z.string().nullable(),
  createdAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
})

export type OpportunityBid = z.infer<typeof OpportunityBidSchema>

export default OpportunityBidSchema;

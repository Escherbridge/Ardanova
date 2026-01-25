import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { BidStatusSchema } from '../inputTypeSchemas/BidStatusSchema'

/////////////////////////////////////////
// PROJECT BID SCHEMA
/////////////////////////////////////////

export const ProjectBidSchema = z.object({
  status: BidStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  guildId: z.string(),
  userId: z.string(),
  proposal: z.string(),
  timeline: z.string().nullable(),
  budget: z.instanceof(Prisma.Decimal, { message: "Field 'budget' must be a Decimal. Location: ['Models', 'ProjectBid']"}),
  deliverables: z.string().nullable(),
  submittedAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
})

export type ProjectBid = z.infer<typeof ProjectBidSchema>

export default ProjectBidSchema;

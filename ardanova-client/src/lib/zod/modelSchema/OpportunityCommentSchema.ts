import { z } from 'zod';

/////////////////////////////////////////
// OPPORTUNITY COMMENT SCHEMA
/////////////////////////////////////////

export const OpportunityCommentSchema = z.object({
  id: z.string().cuid(),
  opportunityId: z.string(),
  userId: z.string(),
  content: z.string(),
  parentId: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type OpportunityComment = z.infer<typeof OpportunityCommentSchema>

export default OpportunityCommentSchema;

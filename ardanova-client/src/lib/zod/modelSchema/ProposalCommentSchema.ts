import { z } from 'zod';

/////////////////////////////////////////
// PROPOSAL COMMENT SCHEMA
/////////////////////////////////////////

export const ProposalCommentSchema = z.object({
  id: z.string().cuid(),
  proposalId: z.string(),
  userId: z.string(),
  content: z.string(),
  parentId: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
});

export type ProposalComment = z.infer<typeof ProposalCommentSchema>;

export default ProposalCommentSchema;

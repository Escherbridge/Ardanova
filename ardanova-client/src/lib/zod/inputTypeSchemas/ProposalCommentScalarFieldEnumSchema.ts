import { z } from 'zod';

export const ProposalCommentScalarFieldEnumSchema = z.enum(['id','proposalId','userId','content','parentId','createdAt','updatedAt']);

export default ProposalCommentScalarFieldEnumSchema;

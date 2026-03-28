import { z } from 'zod';

export const OpportunityCommentScalarFieldEnumSchema = z.enum(['id','opportunityId','userId','content','parentId','createdAt','updatedAt']);

export default OpportunityCommentScalarFieldEnumSchema;

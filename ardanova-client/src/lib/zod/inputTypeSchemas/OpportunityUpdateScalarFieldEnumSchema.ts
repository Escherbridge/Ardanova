import { z } from 'zod';

export const OpportunityUpdateScalarFieldEnumSchema = z.enum(['id','opportunityId','userId','title','content','images','createdAt']);

export default OpportunityUpdateScalarFieldEnumSchema;

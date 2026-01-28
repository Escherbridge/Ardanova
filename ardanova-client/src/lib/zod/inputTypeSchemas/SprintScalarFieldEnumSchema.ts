import { z } from 'zod';

export const SprintScalarFieldEnumSchema = z.enum(['id','projectId','name','goal','startDate','endDate','tokenBudget','velocity','status','createdAt','updatedAt','assigneeId']);

export default SprintScalarFieldEnumSchema;

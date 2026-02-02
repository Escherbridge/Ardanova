import { z } from 'zod';

export const SprintScalarFieldEnumSchema = z.enum(['id','epicId','name','goal','startDate','endDate','equityBudget','velocity','status','createdAt','updatedAt','assigneeId']);

export default SprintScalarFieldEnumSchema;

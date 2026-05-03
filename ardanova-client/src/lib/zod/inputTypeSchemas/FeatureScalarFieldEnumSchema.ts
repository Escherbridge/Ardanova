import { z } from 'zod';

export const FeatureScalarFieldEnumSchema = z.enum(['id','projectId','sprintId','epicId','milestoneId','guildId','title','description','status','priority','equityBudget','order','createdAt','updatedAt','assigneeId']);

export default FeatureScalarFieldEnumSchema;

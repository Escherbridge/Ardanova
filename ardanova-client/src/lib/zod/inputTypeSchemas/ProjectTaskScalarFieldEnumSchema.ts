import { z } from 'zod';

export const ProjectTaskScalarFieldEnumSchema = z.enum(['id','projectId','pbiId','title','description','status','priority','taskType','estimatedHours','actualHours','equityReward','escrowStatus','dueDate','completedAt','createdAt','updatedAt','assignedToId','opportunityId']);

export default ProjectTaskScalarFieldEnumSchema;

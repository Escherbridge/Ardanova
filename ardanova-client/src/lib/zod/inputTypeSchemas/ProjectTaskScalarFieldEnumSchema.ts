import { z } from 'zod';

export const ProjectTaskScalarFieldEnumSchema = z.enum(['id','projectId','backlogItemId','title','description','status','priority','taskType','estimatedHours','actualHours','equityReward','escrowStatus','dueDate','completedAt','createdAt','updatedAt','assignedToId']);

export default ProjectTaskScalarFieldEnumSchema;

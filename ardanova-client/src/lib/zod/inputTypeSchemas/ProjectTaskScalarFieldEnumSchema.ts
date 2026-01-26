import { z } from 'zod';

export const ProjectTaskScalarFieldEnumSchema = z.enum(['id','projectId','backlogItemId','title','description','status','priority','taskType','estimatedHours','actualHours','tokenReward','escrowStatus','dueDate','completedAt','createdAt','updatedAt','assignedToId']);

export default ProjectTaskScalarFieldEnumSchema;

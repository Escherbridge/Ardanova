import { z } from 'zod';

export const ProjectTaskScalarFieldEnumSchema = z.enum(['id','projectId','pbiId','featureId','sprintId','epicId','milestoneId','guildId','title','description','status','priority','taskType','effortEstimate','estimatedHours','actualHours','equityReward','escrowStatus','dueDate','completedAt','createdAt','updatedAt','assignedToId','opportunityId']);

export default ProjectTaskScalarFieldEnumSchema;

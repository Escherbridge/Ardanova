import { z } from 'zod';

export const ProjectMilestoneScalarFieldEnumSchema = z.enum(['id','projectId','guildId','title','description','targetDate','completedAt','status','priority','equityBudget','order','createdAt','updatedAt','assigneeId']);

export default ProjectMilestoneScalarFieldEnumSchema;

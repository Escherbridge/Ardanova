import { z } from 'zod';

export const ProjectMilestoneScalarFieldEnumSchema = z.enum(['id','projectId','title','description','targetDate','completedAt','status','priority','order','createdAt','updatedAt','assigneeId']);

export default ProjectMilestoneScalarFieldEnumSchema;

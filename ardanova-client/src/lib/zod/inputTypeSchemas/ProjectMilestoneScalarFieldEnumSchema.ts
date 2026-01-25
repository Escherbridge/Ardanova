import { z } from 'zod';

export const ProjectMilestoneScalarFieldEnumSchema = z.enum(['id','projectId','title','description','targetDate','completedAt','isCompleted','createdAt']);

export default ProjectMilestoneScalarFieldEnumSchema;

import { z } from 'zod';

export const ProjectTaskDependencyScalarFieldEnumSchema = z.enum(['id','taskId','dependsOnId']);

export default ProjectTaskDependencyScalarFieldEnumSchema;

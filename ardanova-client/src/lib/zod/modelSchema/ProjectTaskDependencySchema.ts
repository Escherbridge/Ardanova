import { z } from 'zod';

/////////////////////////////////////////
// PROJECT TASK DEPENDENCY SCHEMA
/////////////////////////////////////////

export const ProjectTaskDependencySchema = z.object({
  id: z.string().cuid(),
  taskId: z.string(),
  dependsOnId: z.string(),
})

export type ProjectTaskDependency = z.infer<typeof ProjectTaskDependencySchema>

export default ProjectTaskDependencySchema;

import { z } from 'zod';

export const TaskPrioritySchema = z.enum(['LOW','MEDIUM','HIGH','URGENT']);

export type TaskPriorityType = `${z.infer<typeof TaskPrioritySchema>}`

export default TaskPrioritySchema;

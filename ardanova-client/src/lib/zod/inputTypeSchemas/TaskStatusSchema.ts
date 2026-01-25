import { z } from 'zod';

export const TaskStatusSchema = z.enum(['TODO','IN_PROGRESS','REVIEW','COMPLETED','BLOCKED']);

export type TaskStatusType = `${z.infer<typeof TaskStatusSchema>}`

export default TaskStatusSchema;

import { z } from 'zod';

export const TaskTypeSchema = z.enum(['FEATURE','BUG','ENHANCEMENT','DOCUMENTATION','RESEARCH','DESIGN','TESTING','REVIEW','MAINTENANCE','OTHER']);

export type TaskTypeType = `${z.infer<typeof TaskTypeSchema>}`

export default TaskTypeSchema;

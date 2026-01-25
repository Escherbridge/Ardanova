import { z } from 'zod';

export const ICOStatusSchema = z.enum(['PENDING','ACTIVE','SUCCESSFUL','FAILED','CANCELLED','REFUNDING']);

export type ICOStatusType = `${z.infer<typeof ICOStatusSchema>}`

export default ICOStatusSchema;

import { z } from 'zod';

export const FundraisingStatusSchema = z.enum(['PENDING','ACTIVE','SUCCESSFUL','FAILED','CANCELLED','REFUNDING']);

export type FundraisingStatusType = `${z.infer<typeof FundraisingStatusSchema>}`

export default FundraisingStatusSchema;

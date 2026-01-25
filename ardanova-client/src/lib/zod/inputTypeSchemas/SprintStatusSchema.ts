import { z } from 'zod';

export const SprintStatusSchema = z.enum(['PLANNED','ACTIVE','COMPLETED','CANCELLED']);

export type SprintStatusType = `${z.infer<typeof SprintStatusSchema>}`

export default SprintStatusSchema;

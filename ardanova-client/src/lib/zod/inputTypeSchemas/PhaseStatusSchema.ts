import { z } from 'zod';

export const PhaseStatusSchema = z.enum(['PLANNED','IN_PROGRESS','COMPLETED','CANCELLED']);

export type PhaseStatusType = `${z.infer<typeof PhaseStatusSchema>}`

export default PhaseStatusSchema;

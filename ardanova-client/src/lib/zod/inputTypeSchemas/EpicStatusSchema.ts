import { z } from 'zod';

export const EpicStatusSchema = z.enum(['PLANNED','IN_PROGRESS','COMPLETED','CANCELLED']);

export type EpicStatusType = `${z.infer<typeof EpicStatusSchema>}`

export default EpicStatusSchema;

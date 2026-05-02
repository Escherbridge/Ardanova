import { z } from 'zod';

export const FeatureStatusSchema = z.enum(['PLANNED','IN_PROGRESS','COMPLETED','CANCELLED']);

export type FeatureStatusType = `${z.infer<typeof FeatureStatusSchema>}`

export default FeatureStatusSchema;

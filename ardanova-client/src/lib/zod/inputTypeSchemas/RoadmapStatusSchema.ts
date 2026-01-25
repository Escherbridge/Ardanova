import { z } from 'zod';

export const RoadmapStatusSchema = z.enum(['DRAFT','ACTIVE','COMPLETED','ARCHIVED']);

export type RoadmapStatusType = `${z.infer<typeof RoadmapStatusSchema>}`

export default RoadmapStatusSchema;

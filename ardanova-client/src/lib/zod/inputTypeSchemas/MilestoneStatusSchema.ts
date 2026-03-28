import { z } from 'zod';

export const MilestoneStatusSchema = z.enum(['PLANNED','IN_PROGRESS','COMPLETED','CANCELLED']);

export type MilestoneStatusType = `${z.infer<typeof MilestoneStatusSchema>}`

export default MilestoneStatusSchema;

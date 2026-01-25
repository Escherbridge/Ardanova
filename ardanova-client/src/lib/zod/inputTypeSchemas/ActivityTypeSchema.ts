import { z } from 'zod';

export const ActivityTypeSchema = z.enum(['CREATED','UPDATED','DELETED','COMPLETED','JOINED','LEFT','COMMENTED','VOTED','SUBMITTED','REVIEWED','FUNDED','TRANSFERRED','SWAPPED']);

export type ActivityTypeType = `${z.infer<typeof ActivityTypeSchema>}`

export default ActivityTypeSchema;

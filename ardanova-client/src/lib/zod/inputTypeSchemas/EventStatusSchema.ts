import { z } from 'zod';

export const EventStatusSchema = z.enum(['DRAFT','SCHEDULED','LIVE','COMPLETED','CANCELLED']);

export type EventStatusType = `${z.infer<typeof EventStatusSchema>}`

export default EventStatusSchema;

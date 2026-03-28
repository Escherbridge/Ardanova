import { z } from 'zod';

export const EventTypeSchema = z.enum(['MEETING','WORKSHOP','WEBINAR','TOWN_HALL','CRITIQUE','HACKATHON','SOCIAL','OTHER']);

export type EventTypeType = `${z.infer<typeof EventTypeSchema>}`

export default EventTypeSchema;

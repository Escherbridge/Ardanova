import { z } from 'zod';

export const EventVisibilitySchema = z.enum(['PUBLIC','PROJECT_MEMBERS','GUILD_MEMBERS','INVITE_ONLY']);

export type EventVisibilityType = `${z.infer<typeof EventVisibilitySchema>}`

export default EventVisibilitySchema;

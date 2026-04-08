import { z } from 'zod';

export const EventScalarFieldEnumSchema = z.enum(['id','title','slug','description','type','visibility','status','location','locationUrl','isOnline','meetingUrl','timezone','startDate','endDate','maxAttendees','attendeesCount','coverImage','createdAt','updatedAt','organizerId','projectId','guildId']);

export default EventScalarFieldEnumSchema;

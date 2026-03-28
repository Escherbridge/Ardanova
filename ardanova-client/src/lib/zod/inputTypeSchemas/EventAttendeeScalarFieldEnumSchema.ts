import { z } from 'zod';

export const EventAttendeeScalarFieldEnumSchema = z.enum(['id','eventId','userId','status','rsvpAt','attendedAt','notes']);

export default EventAttendeeScalarFieldEnumSchema;

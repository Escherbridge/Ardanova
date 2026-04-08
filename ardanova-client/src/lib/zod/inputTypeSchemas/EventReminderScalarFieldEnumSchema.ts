import { z } from 'zod';

export const EventReminderScalarFieldEnumSchema = z.enum(['id','eventId','userId','remindAt','isSent','sentAt','createdAt']);

export default EventReminderScalarFieldEnumSchema;

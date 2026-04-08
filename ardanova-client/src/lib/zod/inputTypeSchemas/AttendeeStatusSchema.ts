import { z } from 'zod';

export const AttendeeStatusSchema = z.enum(['INVITED','GOING','MAYBE','NOT_GOING','ATTENDED']);

export type AttendeeStatusType = `${z.infer<typeof AttendeeStatusSchema>}`

export default AttendeeStatusSchema;

import { z } from 'zod';

export const ApplicationStatusSchema = z.enum(['PENDING','ACCEPTED','REJECTED','WITHDRAWN']);

export type ApplicationStatusType = `${z.infer<typeof ApplicationStatusSchema>}`

export default ApplicationStatusSchema;

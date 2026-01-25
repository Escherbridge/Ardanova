import { z } from 'zod';
import { ApplicationStatusSchema } from '../inputTypeSchemas/ApplicationStatusSchema'

/////////////////////////////////////////
// PROJECT APPLICATION SCHEMA
/////////////////////////////////////////

export const ProjectApplicationSchema = z.object({
  status: ApplicationStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  roleTitle: z.string(),
  message: z.string(),
  skills: z.string().nullable(),
  experience: z.string().nullable(),
  availability: z.string().nullable(),
  appliedAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
  reviewMessage: z.string().nullable(),
})

export type ProjectApplication = z.infer<typeof ProjectApplicationSchema>

export default ProjectApplicationSchema;

import { z } from 'zod';
import { ApplicationStatusSchema } from '../inputTypeSchemas/ApplicationStatusSchema'

/////////////////////////////////////////
// OPPORTUNITY APPLICATION SCHEMA
/////////////////////////////////////////

export const OpportunityApplicationSchema = z.object({
  status: ApplicationStatusSchema,
  id: z.string().cuid(),
  opportunityId: z.string(),
  applicantId: z.string(),
  coverLetter: z.string(),
  portfolio: z.string().nullable(),
  additionalInfo: z.string().nullable(),
  reviewNotes: z.string().nullable(),
  appliedAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
})

export type OpportunityApplication = z.infer<typeof OpportunityApplicationSchema>

export default OpportunityApplicationSchema;

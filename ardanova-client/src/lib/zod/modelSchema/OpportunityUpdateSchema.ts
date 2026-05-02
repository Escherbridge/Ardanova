import { z } from 'zod';

/////////////////////////////////////////
// OPPORTUNITY UPDATE SCHEMA
/////////////////////////////////////////

export const OpportunityUpdateSchema = z.object({
  id: z.string().cuid(),
  opportunityId: z.string(),
  userId: z.string(),
  title: z.string(),
  content: z.string(),
  images: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type OpportunityUpdate = z.infer<typeof OpportunityUpdateSchema>

export default OpportunityUpdateSchema;

import { z } from 'zod';

export const OpportunityTypeSchema = z.enum(['GUILD_POSITION','PROJECT_ROLE','TASK_BOUNTY','FREELANCE','MENTORSHIP','COLLABORATION']);

export type OpportunityTypeType = `${z.infer<typeof OpportunityTypeSchema>}`

export default OpportunityTypeSchema;

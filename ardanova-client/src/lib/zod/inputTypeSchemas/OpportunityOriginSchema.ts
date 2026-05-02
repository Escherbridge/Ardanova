import { z } from 'zod';

export const OpportunityOriginSchema = z.enum(['TASK_GENERATED','TEAM_POSITION']);

export type OpportunityOriginType = `${z.infer<typeof OpportunityOriginSchema>}`

export default OpportunityOriginSchema;

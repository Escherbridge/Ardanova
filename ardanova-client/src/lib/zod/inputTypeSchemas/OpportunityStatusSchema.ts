import { z } from 'zod';

export const OpportunityStatusSchema = z.enum(['DRAFT','OPEN','IN_REVIEW','FILLED','CLOSED','CANCELLED']);

export type OpportunityStatusType = `${z.infer<typeof OpportunityStatusSchema>}`

export default OpportunityStatusSchema;

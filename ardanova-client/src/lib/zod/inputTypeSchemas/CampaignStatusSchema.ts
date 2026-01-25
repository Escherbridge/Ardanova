import { z } from 'zod';

export const CampaignStatusSchema = z.enum(['DRAFT','SCHEDULED','ACTIVE','COMPLETED','CANCELLED']);

export type CampaignStatusType = `${z.infer<typeof CampaignStatusSchema>}`

export default CampaignStatusSchema;

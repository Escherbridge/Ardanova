import { z } from 'zod';
import { CampaignStatusSchema } from '../inputTypeSchemas/CampaignStatusSchema'

/////////////////////////////////////////
// MARKETING CAMPAIGN SCHEMA
/////////////////////////////////////////

export const MarketingCampaignSchema = z.object({
  status: CampaignStatusSchema,
  id: z.string().cuid(),
  shopId: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  platform: z.string(),
  content: z.string(),
  mediaUrls: z.string().nullable(),
  scheduledAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  userId: z.string(),
})

export type MarketingCampaign = z.infer<typeof MarketingCampaignSchema>

export default MarketingCampaignSchema;

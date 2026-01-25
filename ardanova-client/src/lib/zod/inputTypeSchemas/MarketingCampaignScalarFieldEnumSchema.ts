import { z } from 'zod';

export const MarketingCampaignScalarFieldEnumSchema = z.enum(['id','shopId','name','description','platform','content','mediaUrls','scheduledAt','status','createdAt','updatedAt','userId']);

export default MarketingCampaignScalarFieldEnumSchema;

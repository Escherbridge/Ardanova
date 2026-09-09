import { z } from 'zod';

export const OpportunityBidScalarFieldEnumSchema = z.enum(['id','opportunityId','bidderId','guildId','proposedAmount','guildAwardSplit','proposal','estimatedHours','timeline','deliverables','status','createdAt','reviewedAt']);

export default OpportunityBidScalarFieldEnumSchema;

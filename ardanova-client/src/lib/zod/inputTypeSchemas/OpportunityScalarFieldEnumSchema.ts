import { z } from 'zod';

export const OpportunityScalarFieldEnumSchema = z.enum(['id','title','slug','description','type','origin','status','experienceLevel','requirements','skills','benefits','location','isRemote','compensation','compensationDetails','deadline','maxApplications','applicationsCount','bidsCount','coverImage','createdAt','updatedAt','closedAt','posterId','guildId','projectId','taskId']);

export default OpportunityScalarFieldEnumSchema;

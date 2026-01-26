import { z } from 'zod';

export const GuildScalarFieldEnumSchema = z.enum(['id','name','slug','description','website','email','phone','address','logo','portfolio','specialties','isVerified','rating','reviewsCount','projectsCount','membersCount','isTrending','trendingScore','trendingRank','trendingAt','createdAt','updatedAt','ownerId']);

export default GuildScalarFieldEnumSchema;

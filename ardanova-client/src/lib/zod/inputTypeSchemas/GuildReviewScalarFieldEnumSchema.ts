import { z } from 'zod';

export const GuildReviewScalarFieldEnumSchema = z.enum(['id','guildId','projectId','userId','rating','comment','createdAt']);

export default GuildReviewScalarFieldEnumSchema;

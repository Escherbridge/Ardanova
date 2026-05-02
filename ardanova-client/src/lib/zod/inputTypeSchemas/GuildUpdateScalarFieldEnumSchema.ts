import { z } from 'zod';

export const GuildUpdateScalarFieldEnumSchema = z.enum(['id','guildId','createdById','title','content','images','createdAt']);

export default GuildUpdateScalarFieldEnumSchema;

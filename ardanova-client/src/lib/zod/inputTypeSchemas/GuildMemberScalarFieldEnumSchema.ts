import { z } from 'zod';

export const GuildMemberScalarFieldEnumSchema = z.enum(['id','guildId','userId','role','joinedAt']);

export default GuildMemberScalarFieldEnumSchema;

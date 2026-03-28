import { z } from 'zod';

export const GuildFollowScalarFieldEnumSchema = z.enum(['id','userId','guildId','notifyUpdates','notifyEvents','notifyProjects','createdAt']);

export default GuildFollowScalarFieldEnumSchema;

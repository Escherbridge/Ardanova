import { z } from 'zod';

export const GuildApplicationScalarFieldEnumSchema = z.enum(['id','guildId','userId','requestedRole','message','skills','experience','portfolio','availability','status','reviewedById','reviewMessage','appliedAt','reviewedAt']);

export default GuildApplicationScalarFieldEnumSchema;

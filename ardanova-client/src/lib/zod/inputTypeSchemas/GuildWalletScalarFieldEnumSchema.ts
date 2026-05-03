import { z } from 'zod';

export const GuildWalletScalarFieldEnumSchema = z.enum(['id','guildId','address','provider','label','balance','reservedBalance','isVerified','createdAt','updatedAt']);

export default GuildWalletScalarFieldEnumSchema;

import { z } from 'zod';

export const GuildInvitationScalarFieldEnumSchema = z.enum(['id','guildId','invitedById','invitedUserId','invitedEmail','role','message','status','token','expiresAt','createdAt','respondedAt']);

export default GuildInvitationScalarFieldEnumSchema;

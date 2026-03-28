import { z } from 'zod';

export const ProjectInvitationScalarFieldEnumSchema = z.enum(['id','projectId','invitedById','invitedUserId','invitedEmail','role','message','status','token','expiresAt','createdAt','respondedAt']);

export default ProjectInvitationScalarFieldEnumSchema;

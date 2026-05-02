import { z } from 'zod';
import { ProjectRoleSchema } from '../inputTypeSchemas/ProjectRoleSchema'
import { InvitationStatusSchema } from '../inputTypeSchemas/InvitationStatusSchema'

/////////////////////////////////////////
// PROJECT INVITATION SCHEMA
/////////////////////////////////////////

export const ProjectInvitationSchema = z.object({
  role: ProjectRoleSchema,
  status: InvitationStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  invitedById: z.string(),
  invitedUserId: z.string().nullable(),
  invitedEmail: z.string().nullable(),
  message: z.string().nullable(),
  token: z.string().nullable(),
  expiresAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  respondedAt: z.coerce.date().nullable(),
})

export type ProjectInvitation = z.infer<typeof ProjectInvitationSchema>

export default ProjectInvitationSchema;

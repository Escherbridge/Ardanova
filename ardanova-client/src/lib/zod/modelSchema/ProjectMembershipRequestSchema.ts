import { z } from 'zod';
import { ProjectRoleSchema } from '../inputTypeSchemas/ProjectRoleSchema'
import { MembershipRequestStatusSchema } from '../inputTypeSchemas/MembershipRequestStatusSchema'

/////////////////////////////////////////
// PROJECT MEMBERSHIP REQUEST SCHEMA
/////////////////////////////////////////

export const ProjectMembershipRequestSchema = z.object({
  requestedRole: ProjectRoleSchema,
  status: MembershipRequestStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  message: z.string(),
  skills: z.string().nullable(),
  motivation: z.string().nullable(),
  portfolio: z.string().nullable(),
  reviewedById: z.string().nullable(),
  reviewMessage: z.string().nullable(),
  createdAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
})

export type ProjectMembershipRequest = z.infer<typeof ProjectMembershipRequestSchema>

export default ProjectMembershipRequestSchema;

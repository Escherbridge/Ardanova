import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { ProjectRoleSchema } from '../inputTypeSchemas/ProjectRoleSchema'

/////////////////////////////////////////
// PROJECT MEMBER SCHEMA
/////////////////////////////////////////

export const ProjectMemberSchema = z.object({
  role: ProjectRoleSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  tokenBalance: z.instanceof(Prisma.Decimal, { message: "Field 'tokenBalance' must be a Decimal. Location: ['Models', 'ProjectMember']"}),
  votingPower: z.instanceof(Prisma.Decimal, { message: "Field 'votingPower' must be a Decimal. Location: ['Models', 'ProjectMember']"}),
  joinedAt: z.coerce.date(),
  invitedById: z.string().nullable(),
})

export type ProjectMember = z.infer<typeof ProjectMemberSchema>

export default ProjectMemberSchema;

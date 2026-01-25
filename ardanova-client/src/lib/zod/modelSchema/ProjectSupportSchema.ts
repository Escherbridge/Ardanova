import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { SupportTypeSchema } from '../inputTypeSchemas/SupportTypeSchema'

/////////////////////////////////////////
// PROJECT SUPPORT SCHEMA
/////////////////////////////////////////

export const ProjectSupportSchema = z.object({
  supportType: SupportTypeSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  monthlyAmount: z.instanceof(Prisma.Decimal, { message: "Field 'monthlyAmount' must be a Decimal. Location: ['Models', 'ProjectSupport']"}).nullable(),
  message: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ProjectSupport = z.infer<typeof ProjectSupportSchema>

export default ProjectSupportSchema;

import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// PROJECT RESOURCE SCHEMA
/////////////////////////////////////////

export const ProjectResourceSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  quantity: z.number().int(),
  estimatedCost: z.instanceof(Prisma.Decimal, { message: "Field 'estimatedCost' must be a Decimal. Location: ['Models', 'ProjectResource']"}).nullable(),
  recurringCost: z.instanceof(Prisma.Decimal, { message: "Field 'recurringCost' must be a Decimal. Location: ['Models', 'ProjectResource']"}).nullable(),
  recurringIntervalDays: z.number().int().nullable(),
  isRequired: z.boolean(),
  isObtained: z.boolean(),
  createdAt: z.coerce.date(),
})

export type ProjectResource = z.infer<typeof ProjectResourceSchema>

export default ProjectResourceSchema;

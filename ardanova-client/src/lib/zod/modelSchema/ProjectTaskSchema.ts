import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { TaskStatusSchema } from '../inputTypeSchemas/TaskStatusSchema'
import { TaskPrioritySchema } from '../inputTypeSchemas/TaskPrioritySchema'
import { EscrowStatusSchema } from '../inputTypeSchemas/EscrowStatusSchema'

/////////////////////////////////////////
// PROJECT TASK SCHEMA
/////////////////////////////////////////

export const ProjectTaskSchema = z.object({
  status: TaskStatusSchema,
  priority: TaskPrioritySchema,
  escrowStatus: EscrowStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  backlogItemId: z.string().nullable(),
  title: z.string(),
  description: z.string().nullable(),
  estimatedHours: z.number().int().nullable(),
  actualHours: z.number().int().nullable(),
  tokenReward: z.instanceof(Prisma.Decimal, { message: "Field 'tokenReward' must be a Decimal. Location: ['Models', 'ProjectTask']"}).nullable(),
  dueDate: z.coerce.date().nullable(),
  completedAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assignedToId: z.string().nullable(),
})

export type ProjectTask = z.infer<typeof ProjectTaskSchema>

export default ProjectTaskSchema;

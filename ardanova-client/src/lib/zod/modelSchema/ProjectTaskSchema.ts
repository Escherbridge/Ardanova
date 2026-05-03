import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { TaskStatusSchema } from '../inputTypeSchemas/TaskStatusSchema'
import { TaskPrioritySchema } from '../inputTypeSchemas/TaskPrioritySchema'
import { TaskTypeSchema } from '../inputTypeSchemas/TaskTypeSchema'
import { EffortEstimateSchema } from '../inputTypeSchemas/EffortEstimateSchema'
import { EscrowStatusSchema } from '../inputTypeSchemas/EscrowStatusSchema'

/////////////////////////////////////////
// PROJECT TASK SCHEMA
/////////////////////////////////////////

export const ProjectTaskSchema = z.object({
  status: TaskStatusSchema,
  priority: TaskPrioritySchema,
  taskType: TaskTypeSchema,
  effortEstimate: EffortEstimateSchema.nullable(),
  escrowStatus: EscrowStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  pbiId: z.string().nullable(),
  featureId: z.string().nullable(),
  sprintId: z.string().nullable(),
  epicId: z.string().nullable(),
  milestoneId: z.string().nullable(),
  guildId: z.string().nullable(),
  title: z.string(),
  description: z.string().nullable(),
  estimatedHours: z.number().int().nullable(),
  actualHours: z.number().int().nullable(),
  equityReward: z.instanceof(Prisma.Decimal, { message: "Field 'equityReward' must be a Decimal. Location: ['Models', 'ProjectTask']"}).nullable(),
  dueDate: z.coerce.date().nullable(),
  completedAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assignedToId: z.string().nullable(),
  opportunityId: z.string().nullable(),
})

export type ProjectTask = z.infer<typeof ProjectTaskSchema>

export default ProjectTaskSchema;

import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { CompensationModelSchema } from '../inputTypeSchemas/CompensationModelSchema'

/////////////////////////////////////////
// TASK COMPENSATION SCHEMA
/////////////////////////////////////////

export const TaskCompensationSchema = z.object({
  model: CompensationModelSchema,
  id: z.string().cuid(),
  taskId: z.string(),
  tokenAmount: z.instanceof(Prisma.Decimal, { message: "Field 'tokenAmount' must be a Decimal. Location: ['Models', 'TaskCompensation']"}).nullable(),
  hourlyRate: z.instanceof(Prisma.Decimal, { message: "Field 'hourlyRate' must be a Decimal. Location: ['Models', 'TaskCompensation']"}).nullable(),
  equityPercent: z.instanceof(Prisma.Decimal, { message: "Field 'equityPercent' must be a Decimal. Location: ['Models', 'TaskCompensation']"}).nullable(),
  stableCoinAmount: z.instanceof(Prisma.Decimal, { message: "Field 'stableCoinAmount' must be a Decimal. Location: ['Models', 'TaskCompensation']"}).nullable(),
  vestingMonths: z.number().int().nullable(),
  createdAt: z.coerce.date(),
})

export type TaskCompensation = z.infer<typeof TaskCompensationSchema>

export default TaskCompensationSchema;

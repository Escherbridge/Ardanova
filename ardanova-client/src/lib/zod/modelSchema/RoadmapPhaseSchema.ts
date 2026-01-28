import { z } from 'zod';
import { PhaseStatusSchema } from '../inputTypeSchemas/PhaseStatusSchema'

/////////////////////////////////////////
// ROADMAP PHASE SCHEMA
/////////////////////////////////////////

export const RoadmapPhaseSchema = z.object({
  status: PhaseStatusSchema,
  id: z.string().cuid(),
  roadmapId: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  order: z.number().int(),
  startDate: z.coerce.date().nullable(),
  endDate: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type RoadmapPhase = z.infer<typeof RoadmapPhaseSchema>

export default RoadmapPhaseSchema;

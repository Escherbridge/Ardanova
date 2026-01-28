import { z } from 'zod';
import { RoadmapStatusSchema } from '../inputTypeSchemas/RoadmapStatusSchema'

/////////////////////////////////////////
// ROADMAP SCHEMA
/////////////////////////////////////////

export const RoadmapSchema = z.object({
  status: RoadmapStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  title: z.string(),
  vision: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type Roadmap = z.infer<typeof RoadmapSchema>

export default RoadmapSchema;

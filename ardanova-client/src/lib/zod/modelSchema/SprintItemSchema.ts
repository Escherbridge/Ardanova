import { z } from 'zod';

/////////////////////////////////////////
// SPRINT ITEM SCHEMA
/////////////////////////////////////////

export const SprintItemSchema = z.object({
  id: z.string().cuid(),
  sprintId: z.string(),
  taskId: z.string(),
  order: z.number().int(),
  addedAt: z.coerce.date(),
})

export type SprintItem = z.infer<typeof SprintItemSchema>

export default SprintItemSchema;

import { z } from 'zod';

export const BacklogStatusSchema = z.enum(['NEW','READY','IN_PROGRESS','DONE','BLOCKED']);

export type BacklogStatusType = `${z.infer<typeof BacklogStatusSchema>}`

export default BacklogStatusSchema;

import { z } from 'zod';

export const PBIStatusSchema = z.enum(['NEW','READY','IN_PROGRESS','DONE','CANCELLED']);

export type PBIStatusType = `${z.infer<typeof PBIStatusSchema>}`

export default PBIStatusSchema;

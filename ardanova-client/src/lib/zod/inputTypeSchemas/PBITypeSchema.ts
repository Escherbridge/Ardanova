import { z } from 'zod';

export const PBITypeSchema = z.enum(['FEATURE','ENHANCEMENT','BUG','TECHNICAL_DEBT','SPIKE']);

export type PBITypeType = `${z.infer<typeof PBITypeSchema>}`

export default PBITypeSchema;

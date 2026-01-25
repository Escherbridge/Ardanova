import { z } from 'zod';

export const VerificationLevelSchema = z.enum(['ANONYMOUS','VERIFIED','PRO','EXPERT']);

export type VerificationLevelType = `${z.infer<typeof VerificationLevelSchema>}`

export default VerificationLevelSchema;

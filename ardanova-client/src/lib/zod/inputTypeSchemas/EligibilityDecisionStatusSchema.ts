import { z } from 'zod';

export const EligibilityDecisionStatusSchema = z.enum(['APPROVED','REJECTED','EXPIRED','REVOKED']);

export type EligibilityDecisionStatusType = `${z.infer<typeof EligibilityDecisionStatusSchema>}`

export default EligibilityDecisionStatusSchema;

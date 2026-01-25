import { z } from 'zod';

export const SubmissionStatusSchema = z.enum(['PENDING','APPROVED','REJECTED','REVISION_REQUESTED']);

export type SubmissionStatusType = `${z.infer<typeof SubmissionStatusSchema>}`

export default SubmissionStatusSchema;

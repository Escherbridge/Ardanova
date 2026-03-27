import { z } from 'zod';
import { KycProviderSchema } from '../inputTypeSchemas/KycProviderSchema'
import { KycStatusSchema } from '../inputTypeSchemas/KycStatusSchema'

/////////////////////////////////////////
// KYC SUBMISSION SCHEMA
/////////////////////////////////////////

export const KycSubmissionSchema = z.object({
  provider: KycProviderSchema,
  status: KycStatusSchema,
  id: z.string().cuid(),
  userId: z.string(),
  reviewerId: z.string().nullable(),
  reviewNotes: z.string().nullable(),
  rejectionReason: z.string().nullable(),
  providerSessionId: z.string().nullable(),
  providerResult: z.string().nullable(),
  submittedAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
  expiresAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type KycSubmission = z.infer<typeof KycSubmissionSchema>

export default KycSubmissionSchema;

import { z } from 'zod';
import { KycDocumentTypeSchema } from '../inputTypeSchemas/KycDocumentTypeSchema'

/////////////////////////////////////////
// KYC DOCUMENT SCHEMA
/////////////////////////////////////////

export const KycDocumentSchema = z.object({
  type: KycDocumentTypeSchema,
  id: z.string().cuid(),
  submissionId: z.string(),
  fileUrl: z.string(),
  fileName: z.string(),
  mimeType: z.string().nullable(),
  fileSizeBytes: z.number().int().nullable(),
  metadata: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type KycDocument = z.infer<typeof KycDocumentSchema>

export default KycDocumentSchema;

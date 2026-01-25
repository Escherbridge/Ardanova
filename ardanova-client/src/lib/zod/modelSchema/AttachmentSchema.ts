import { z } from 'zod';
import { MimeTypeSchema } from '../inputTypeSchemas/MimeTypeSchema'

/////////////////////////////////////////
// ATTACHMENT SCHEMA
/////////////////////////////////////////

export const AttachmentSchema = z.object({
  type: MimeTypeSchema,
  id: z.string().cuid(),
  uploadedById: z.string(),
  bucketPath: z.string().nullable(),
  createdAt: z.coerce.date(),
  lastUsedAt: z.coerce.date().nullable(),
})

export type Attachment = z.infer<typeof AttachmentSchema>

export default AttachmentSchema;

import { z } from 'zod';

export const MimeTypeSchema = z.enum(['IMAGE','VIDEO','AUDIO','DOCUMENT','ARCHIVE','OTHER']);

export type MimeTypeType = `${z.infer<typeof MimeTypeSchema>}`

export default MimeTypeSchema;

import { z } from 'zod';

export const AttachmentScalarFieldEnumSchema = z.enum(['id','uploadedById','bucketPath','type','createdAt','lastUsedAt']);

export default AttachmentScalarFieldEnumSchema;

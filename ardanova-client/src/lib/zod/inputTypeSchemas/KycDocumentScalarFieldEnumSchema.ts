import { z } from 'zod';

export const KycDocumentScalarFieldEnumSchema = z.enum(['id','submissionId','type','fileUrl','fileName','mimeType','fileSizeBytes','metadata','createdAt']);

export default KycDocumentScalarFieldEnumSchema;

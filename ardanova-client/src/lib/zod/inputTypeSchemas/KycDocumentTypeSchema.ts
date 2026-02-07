import { z } from 'zod';

export const KycDocumentTypeSchema = z.enum(['GOVERNMENT_ID','PASSPORT','DRIVERS_LICENSE','SELFIE','PROOF_OF_ADDRESS']);

export type KycDocumentTypeType = `${z.infer<typeof KycDocumentTypeSchema>}`

export default KycDocumentTypeSchema;

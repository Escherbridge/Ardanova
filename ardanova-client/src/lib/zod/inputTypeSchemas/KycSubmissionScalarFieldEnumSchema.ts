import { z } from 'zod';

export const KycSubmissionScalarFieldEnumSchema = z.enum(['id','userId','provider','status','reviewerId','reviewNotes','rejectionReason','providerSessionId','providerResult','submittedAt','reviewedAt','expiresAt','createdAt','updatedAt']);

export default KycSubmissionScalarFieldEnumSchema;

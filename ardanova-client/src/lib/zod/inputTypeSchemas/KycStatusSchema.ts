import { z } from 'zod';

export const KycStatusSchema = z.enum(['PENDING','IN_REVIEW','APPROVED','REJECTED','EXPIRED']);

export type KycStatusType = `${z.infer<typeof KycStatusSchema>}`

export default KycStatusSchema;

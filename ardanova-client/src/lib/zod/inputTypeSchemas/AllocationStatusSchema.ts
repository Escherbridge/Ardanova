import { z } from 'zod';

export const AllocationStatusSchema = z.enum(['RESERVED','DISTRIBUTED','REVOKED','BURNED']);

export type AllocationStatusType = `${z.infer<typeof AllocationStatusSchema>}`

export default AllocationStatusSchema;

import { z } from 'zod';
import { AllocationStatusSchema } from '../inputTypeSchemas/AllocationStatusSchema'
import { TokenHolderClassSchema } from '../inputTypeSchemas/TokenHolderClassSchema'

/////////////////////////////////////////
// TOKEN ALLOCATION SCHEMA
/////////////////////////////////////////

export const TokenAllocationSchema = z.object({
  status: AllocationStatusSchema,
  holderClass: TokenHolderClassSchema,
  id: z.string().cuid(),
  projectTokenConfigId: z.string(),
  pbiId: z.string().nullable(),
  recipientUserId: z.string().nullable(),
  equityPercentage: z.number(),
  tokenAmount: z.number().int(),
  isLiquid: z.boolean(),
  distributedAt: z.coerce.date().nullable(),
  distributionTxHash: z.string().nullable(),
  burnedAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type TokenAllocation = z.infer<typeof TokenAllocationSchema>

export default TokenAllocationSchema;

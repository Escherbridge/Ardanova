import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// TREASURY SCHEMA
/////////////////////////////////////////

export const TreasurySchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  balance: z.instanceof(Prisma.Decimal, { message: "Field 'balance' must be a Decimal. Location: ['Models', 'Treasury']"}),
  tokenAssetId: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type Treasury = z.infer<typeof TreasurySchema>

export default TreasurySchema;

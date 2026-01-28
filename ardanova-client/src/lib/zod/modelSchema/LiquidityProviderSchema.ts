import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// LIQUIDITY PROVIDER SCHEMA
/////////////////////////////////////////

export const LiquidityProviderSchema = z.object({
  id: z.string().cuid(),
  poolId: z.string(),
  userId: z.string(),
  shares: z.instanceof(Prisma.Decimal, { message: "Field 'shares' must be a Decimal. Location: ['Models', 'LiquidityProvider']"}),
  share1In: z.instanceof(Prisma.Decimal, { message: "Field 'share1In' must be a Decimal. Location: ['Models', 'LiquidityProvider']"}),
  share2In: z.instanceof(Prisma.Decimal, { message: "Field 'share2In' must be a Decimal. Location: ['Models', 'LiquidityProvider']"}),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type LiquidityProvider = z.infer<typeof LiquidityProviderSchema>

export default LiquidityProviderSchema;

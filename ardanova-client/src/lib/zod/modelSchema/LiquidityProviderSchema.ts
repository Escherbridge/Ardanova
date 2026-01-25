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
  token1In: z.instanceof(Prisma.Decimal, { message: "Field 'token1In' must be a Decimal. Location: ['Models', 'LiquidityProvider']"}),
  token2In: z.instanceof(Prisma.Decimal, { message: "Field 'token2In' must be a Decimal. Location: ['Models', 'LiquidityProvider']"}),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type LiquidityProvider = z.infer<typeof LiquidityProviderSchema>

export default LiquidityProviderSchema;

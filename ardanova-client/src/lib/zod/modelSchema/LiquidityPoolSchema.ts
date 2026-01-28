import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// LIQUIDITY POOL SCHEMA
/////////////////////////////////////////

export const LiquidityPoolSchema = z.object({
  id: z.string().cuid(),
  share1Id: z.string(),
  share2Id: z.string(),
  reserve1: z.instanceof(Prisma.Decimal, { message: "Field 'reserve1' must be a Decimal. Location: ['Models', 'LiquidityPool']"}),
  reserve2: z.instanceof(Prisma.Decimal, { message: "Field 'reserve2' must be a Decimal. Location: ['Models', 'LiquidityPool']"}),
  totalShares: z.instanceof(Prisma.Decimal, { message: "Field 'totalShares' must be a Decimal. Location: ['Models', 'LiquidityPool']"}),
  feePercent: z.instanceof(Prisma.Decimal, { message: "Field 'feePercent' must be a Decimal. Location: ['Models', 'LiquidityPool']"}),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type LiquidityPool = z.infer<typeof LiquidityPoolSchema>

export default LiquidityPoolSchema;

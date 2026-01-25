import { z } from 'zod';

export const LiquidityPoolScalarFieldEnumSchema = z.enum(['id','token1Id','token2Id','reserve1','reserve2','totalShares','feePercent','isActive','createdAt','updatedAt']);

export default LiquidityPoolScalarFieldEnumSchema;

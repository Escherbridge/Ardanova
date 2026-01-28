import { z } from 'zod';

export const LiquidityPoolScalarFieldEnumSchema = z.enum(['id','share1Id','share2Id','reserve1','reserve2','totalShares','feePercent','isActive','createdAt','updatedAt']);

export default LiquidityPoolScalarFieldEnumSchema;

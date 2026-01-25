import { z } from 'zod';

export const LiquidityProviderScalarFieldEnumSchema = z.enum(['id','poolId','userId','shares','token1In','token2In','createdAt','updatedAt']);

export default LiquidityProviderScalarFieldEnumSchema;

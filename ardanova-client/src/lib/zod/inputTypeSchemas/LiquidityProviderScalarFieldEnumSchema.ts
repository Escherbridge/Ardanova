import { z } from 'zod';

export const LiquidityProviderScalarFieldEnumSchema = z.enum(['id','poolId','userId','shares','share1In','share2In','createdAt','updatedAt']);

export default LiquidityProviderScalarFieldEnumSchema;

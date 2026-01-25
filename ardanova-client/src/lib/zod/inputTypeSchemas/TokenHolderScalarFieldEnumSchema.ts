import { z } from 'zod';

export const TokenHolderScalarFieldEnumSchema = z.enum(['id','tokenId','userId','balance','stakedAmount','lockedAmount','createdAt','updatedAt']);

export default TokenHolderScalarFieldEnumSchema;

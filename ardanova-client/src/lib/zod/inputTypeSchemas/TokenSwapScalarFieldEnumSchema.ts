import { z } from 'zod';

export const TokenSwapScalarFieldEnumSchema = z.enum(['id','userId','fromTokenId','toTokenId','fromAmount','toAmount','exchangeRate','fee','txHash','status','createdAt','completedAt']);

export default TokenSwapScalarFieldEnumSchema;

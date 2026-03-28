import { z } from 'zod';

export const ShareSwapScalarFieldEnumSchema = z.enum(['id','userId','fromShareId','toShareId','fromAmount','toAmount','exchangeRate','fee','txHash','status','createdAt','completedAt']);

export default ShareSwapScalarFieldEnumSchema;

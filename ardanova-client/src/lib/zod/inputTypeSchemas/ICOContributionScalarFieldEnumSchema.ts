import { z } from 'zod';

export const ICOContributionScalarFieldEnumSchema = z.enum(['id','icoId','userId','amount','tokenAmount','paymentAsset','txHash','status','createdAt']);

export default ICOContributionScalarFieldEnumSchema;

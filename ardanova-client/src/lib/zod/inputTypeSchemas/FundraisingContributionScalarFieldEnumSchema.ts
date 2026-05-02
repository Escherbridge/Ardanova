import { z } from 'zod';

export const FundraisingContributionScalarFieldEnumSchema = z.enum(['id','fundraisingId','userId','amount','shareAmount','paymentAsset','txHash','status','createdAt']);

export default FundraisingContributionScalarFieldEnumSchema;

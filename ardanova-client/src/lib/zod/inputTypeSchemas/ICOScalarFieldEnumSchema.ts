import { z } from 'zod';

export const ICOScalarFieldEnumSchema = z.enum(['id','tokenId','fundingGoal','currentFunding','minContribution','maxContribution','tokenPrice','startDate','endDate','status','createdAt','updatedAt']);

export default ICOScalarFieldEnumSchema;

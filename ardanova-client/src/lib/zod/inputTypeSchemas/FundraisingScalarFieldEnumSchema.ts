import { z } from 'zod';

export const FundraisingScalarFieldEnumSchema = z.enum(['id','shareId','fundingGoal','currentFunding','minContribution','maxContribution','sharePrice','startDate','endDate','status','createdAt','updatedAt']);

export default FundraisingScalarFieldEnumSchema;

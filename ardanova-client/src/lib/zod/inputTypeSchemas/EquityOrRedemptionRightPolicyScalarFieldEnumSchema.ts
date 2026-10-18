import { z } from 'zod';

export const EquityOrRedemptionRightPolicyScalarFieldEnumSchema = z.enum(['id','projectId','kind','version','jurisdiction','disclosureVersion','eligibilityPolicyVersion','termsHash','termsSnapshot','effectiveFrom','retiredAt','createdAt']);

export default EquityOrRedemptionRightPolicyScalarFieldEnumSchema;

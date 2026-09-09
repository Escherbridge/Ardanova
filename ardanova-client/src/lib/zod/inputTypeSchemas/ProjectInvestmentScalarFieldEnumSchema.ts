import { z } from 'zod';

export const ProjectInvestmentScalarFieldEnumSchema = z.enum(['id','projectTokenConfigId','userId','usdAmount','tokenAmount','investedAt','protectionEligible','protectionPaidOut','protectionAmount','protectionPaidAt','showBackerName']);

export default ProjectInvestmentScalarFieldEnumSchema;

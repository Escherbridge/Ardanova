import { z } from 'zod';

export const ProjectInvestmentScalarFieldEnumSchema = z.enum(['id','projectTokenConfigId','userId','usdAmount','tokenAmount','investedAt','showBackerName','protectionEligible','protectionPaidOut','protectionAmount','protectionPaidAt']);

export default ProjectInvestmentScalarFieldEnumSchema;

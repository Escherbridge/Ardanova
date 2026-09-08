import { z } from 'zod';

export const ProjectInvestmentScalarFieldEnumSchema = z.enum(['id','projectTokenConfigId','userId','usdAmount','tokenAmount','stripePaymentIntentId','investedAt','protectionEligible','protectionPaidOut','protectionAmount','protectionPaidAt','showBackerName']);

export default ProjectInvestmentScalarFieldEnumSchema;

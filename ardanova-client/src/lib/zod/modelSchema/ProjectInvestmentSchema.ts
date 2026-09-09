import { z } from 'zod';

/////////////////////////////////////////
// PROJECT INVESTMENT SCHEMA
/////////////////////////////////////////

export const ProjectInvestmentSchema = z.object({
  id: z.string().cuid(),
  projectTokenConfigId: z.string(),
  userId: z.string(),
  usdAmount: z.number(),
  tokenAmount: z.number().int(),
  investedAt: z.coerce.date(),
  protectionEligible: z.boolean(),
  protectionPaidOut: z.boolean(),
  protectionAmount: z.number().nullable(),
  protectionPaidAt: z.coerce.date().nullable(),
  showBackerName: z.boolean(),
})

export type ProjectInvestment = z.infer<typeof ProjectInvestmentSchema>

export default ProjectInvestmentSchema;

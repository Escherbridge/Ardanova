import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'

/////////////////////////////////////////
// PROJECT TOKEN POLICY SCHEMA
/////////////////////////////////////////

export const ProjectTokenPolicySchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  assetDefinitionId: z.string(),
  version: z.number().int(),
  termsHash: z.string(),
  allocationRules: JsonValueSchema,
  effectiveFrom: z.coerce.date(),
  retiredAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
})

export type ProjectTokenPolicy = z.infer<typeof ProjectTokenPolicySchema>

export default ProjectTokenPolicySchema;

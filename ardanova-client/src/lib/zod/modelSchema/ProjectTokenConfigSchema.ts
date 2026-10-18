import { z } from 'zod';
import { ProjectTokenStatusSchema } from '../inputTypeSchemas/ProjectTokenStatusSchema'
import { ProjectGateStatusSchema } from '../inputTypeSchemas/ProjectGateStatusSchema'

/////////////////////////////////////////
// PROJECT TOKEN CONFIG SCHEMA
/////////////////////////////////////////

export const ProjectTokenConfigSchema = z.object({
  status: ProjectTokenStatusSchema,
  gateStatus: ProjectGateStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  assetId: z.string().nullable(),
  assetDefinitionId: z.string().nullable(),
  assetName: z.string(),
  unitName: z.string(),
  assetScale: z.number().int().nullable(),
  totalSupply: z.number().int(),
  allocatedSupply: z.number().int(),
  distributedSupply: z.number().int(),
  reservedSupply: z.number().int(),
  mintTxHash: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  fundingGoal: z.number(),
  fundingRaised: z.number(),
  gate1ClearedAt: z.coerce.date().nullable(),
  gate2ClearedAt: z.coerce.date().nullable(),
  failedAt: z.coerce.date().nullable(),
  contributorSupply: z.number().int(),
  investorSupply: z.number().int(),
  founderSupply: z.number().int(),
  burnedSupply: z.number().int(),
  successCriteria: z.string().nullable(),
  successVerifiedBy: z.string().nullable(),
  assetChain: z.string().nullable(),
  treasuryChain: z.string().nullable(),
  treasuryReference: z.string().nullable(),
  treasuryAddress: z.string().nullable(),
  treasuryResolvedAt: z.coerce.date().nullable(),
  contributorPartitionSupply: z.number().int(),
  investorPartitionSupply: z.number().int(),
  partitionsDeclaredAt: z.coerce.date().nullable(),
  unitsDisclosure: z.string().nullable(),
  provisionedAt: z.coerce.date().nullable(),
  provisionedByUserId: z.string().nullable(),
  provisioningFailureReason: z.string().nullable(),
})

export type ProjectTokenConfig = z.infer<typeof ProjectTokenConfigSchema>

export default ProjectTokenConfigSchema;

import { z } from 'zod';

export const ProjectTokenConfigScalarFieldEnumSchema = z.enum(['id','projectId','assetId','assetName','unitName','totalSupply','allocatedSupply','distributedSupply','reservedSupply','mintTxHash','status','createdAt','updatedAt','fundingGoal','fundingRaised','gateStatus','gate1ClearedAt','gate2ClearedAt','failedAt','contributorSupply','investorSupply','founderSupply','burnedSupply','successCriteria','successVerifiedBy']);

export default ProjectTokenConfigScalarFieldEnumSchema;

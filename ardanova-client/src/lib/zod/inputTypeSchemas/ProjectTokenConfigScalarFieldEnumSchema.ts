import { z } from 'zod';

export const ProjectTokenConfigScalarFieldEnumSchema = z.enum(['id','projectId','assetId','assetName','unitName','assetScale','totalSupply','allocatedSupply','distributedSupply','reservedSupply','mintTxHash','status','createdAt','updatedAt','fundingGoal','fundingRaised','gateStatus','gate1ClearedAt','gate2ClearedAt','failedAt','contributorSupply','investorSupply','founderSupply','burnedSupply','successCriteria','successVerifiedBy','assetChain','treasuryChain','treasuryReference','treasuryAddress','treasuryResolvedAt','contributorPartitionSupply','investorPartitionSupply','partitionsDeclaredAt','unitsDisclosure','provisionedAt','provisionedByUserId','provisioningFailureReason']);

export default ProjectTokenConfigScalarFieldEnumSchema;

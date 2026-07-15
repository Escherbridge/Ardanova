import { z } from 'zod';

export const TaskCommerceAgreementScalarFieldEnumSchema = z.enum(['id','semanticKey','status','projectId','taskId','bidId','contributorUserId','projectTokenConfigId','assetCode','awardAmount','scale','acceptedTermsSnapshot','termsHash','escrowId','questRunId','settlementId','acceptedAt','releaseAuthorizedAt','settledAt','cancelledAt','createdAt','updatedAt']);

export default TaskCommerceAgreementScalarFieldEnumSchema;

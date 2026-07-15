import { z } from 'zod';

export const EconomicSettlementScalarFieldEnumSchema = z.enum(['id','kind','status','idempotencyKey','externalEventId','beneficiaryUserId','authorizedByUserId','projectId','taskId','escrowId','assetCode','amount','scale','termsSnapshot','azoaOperationId','azoaReceipt','azoaReplayed','failureCode','failureDetail','version','createdAt','authorizedAt','submittedAt','confirmedAt','updatedAt']);

export default EconomicSettlementScalarFieldEnumSchema;

import { z } from 'zod';

export const EconomicOutboxScalarFieldEnumSchema = z.enum(['id','settlementId','status','payloadVersion','attemptCount','availableAt','leaseToken','leaseExpiresAt','lastAttemptAt','dispatchedAt','reconciliationRequiredAt','failureCode','failureDetail','createdAt','updatedAt']);

export default EconomicOutboxScalarFieldEnumSchema;

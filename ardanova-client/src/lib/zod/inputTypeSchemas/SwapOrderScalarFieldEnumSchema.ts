import { z } from 'zod';

export const SwapOrderScalarFieldEnumSchema = z.enum(['id','quoteId','actorUserId','status','economicSettlementId','acceptedAt','confirmedAt','failureCode','createdAt','updatedAt']);

export default SwapOrderScalarFieldEnumSchema;

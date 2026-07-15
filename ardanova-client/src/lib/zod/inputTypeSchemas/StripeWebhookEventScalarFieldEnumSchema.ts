import { z } from 'zod';

export const StripeWebhookEventScalarFieldEnumSchema = z.enum(['id','eventType','status','attemptCount','receivedAt','processingLeaseExpiresAt','completedAt','lastFailedAt']);

export default StripeWebhookEventScalarFieldEnumSchema;

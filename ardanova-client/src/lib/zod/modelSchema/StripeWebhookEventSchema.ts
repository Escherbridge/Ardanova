import { z } from 'zod';
import { StripeWebhookEventStatusSchema } from '../inputTypeSchemas/StripeWebhookEventStatusSchema'

/////////////////////////////////////////
// STRIPE WEBHOOK EVENT SCHEMA
/////////////////////////////////////////

export const StripeWebhookEventSchema = z.object({
  status: StripeWebhookEventStatusSchema,
  id: z.string(),
  eventType: z.string(),
  attemptCount: z.number().int(),
  receivedAt: z.coerce.date(),
  processingLeaseExpiresAt: z.coerce.date(),
  completedAt: z.coerce.date().nullable(),
  lastFailedAt: z.coerce.date().nullable(),
})

export type StripeWebhookEvent = z.infer<typeof StripeWebhookEventSchema>

export default StripeWebhookEventSchema;

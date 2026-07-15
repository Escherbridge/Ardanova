import { z } from 'zod';

export const StripeWebhookEventStatusSchema = z.enum(['PROCESSING','COMPLETED','FAILED']);

export type StripeWebhookEventStatusType = `${z.infer<typeof StripeWebhookEventStatusSchema>}`

export default StripeWebhookEventStatusSchema;

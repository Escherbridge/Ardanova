import { z } from 'zod';
import { EconomicOutboxStatusSchema } from '../inputTypeSchemas/EconomicOutboxStatusSchema'

/////////////////////////////////////////
// ECONOMIC OUTBOX SCHEMA
/////////////////////////////////////////

export const EconomicOutboxSchema = z.object({
  status: EconomicOutboxStatusSchema,
  id: z.string().cuid(),
  settlementId: z.string(),
  payloadVersion: z.number().int(),
  attemptCount: z.number().int(),
  availableAt: z.coerce.date(),
  leaseToken: z.string().nullable(),
  leaseExpiresAt: z.coerce.date().nullable(),
  lastAttemptAt: z.coerce.date().nullable(),
  dispatchedAt: z.coerce.date().nullable(),
  reconciliationRequiredAt: z.coerce.date().nullable(),
  failureCode: z.string().nullable(),
  failureDetail: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type EconomicOutbox = z.infer<typeof EconomicOutboxSchema>

export default EconomicOutboxSchema;

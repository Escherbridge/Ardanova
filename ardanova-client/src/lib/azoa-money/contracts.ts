import { z } from "zod";

const exactMinorUnits = z
  .string()
  .max(16)
  .regex(/^(?:0|[1-9]\d*)$/u)
  .refine(
    (value) => BigInt(value) <= BigInt(Number.MAX_SAFE_INTEGER),
    "Amount exceeds the supported exact display range.",
  );
const minorUnits = z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER);
export const requestKeySchema = z
  .string()
  .min(1)
  .max(128)
  .regex(/^[A-Za-z0-9._:-]+$/u);
export const recordIdSchema = z.string().min(1).max(128);
export const moneyRequestSchema = z
  .object({
    requestKey: requestKeySchema,
    amountMinorUnits: minorUnits.positive(),
  })
  .strict();
export const holdingSchema = z
  .object({
    projectTokenConfigId: recordIdSchema,
    holderClass: z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]),
  })
  .strict();
export type MoneyHolding = z.infer<typeof holdingSchema>;

export const accountSchema = z.object({
  kycStatus: z.enum([
    "Unknown",
    "Pending",
    "Approved",
    "Rejected",
    "UnderReview",
  ]),
  identityReady: z.boolean(),
  kycReady: z.boolean(),
  walletReady: z.boolean(),
  ready: z.boolean(),
  unavailableReason: z.string().nullish(),
  reviewDueAt: z.string().nullish(),
  ownership: z.enum(["custodial", "claimed"]),
  consent: z.enum(["granted", "none"]),
  consentScopes: z.array(z.string()),
  consentExpiresAt: z.string().nullable(),
});
export const accountCapabilitiesSchema = z.object({
  ready: z.boolean(),
  kycAvailable: z.boolean(),
  unavailableReason: z.string().nullish(),
});
export const kycSchema = z.object({
  verificationUrl: z.string().nullish(),
  expiresAt: z.string().nullish(),
  instructions: z.string().nullish(),
  developmentSimulation: z.boolean(),
});
export const revokeSchema = z.object({
  revoked: z.literal(true),
  revokedAt: z.string().datetime({ offset: true }),
});
export const capabilitiesSchema = z.object({
  available: z.boolean(),
  unavailableReason: z.string().nullable(),
});
export const checkoutSchema = z.object({
  paymentId: recordIdSchema,
  status: z.enum([
    "starting",
    "awaiting_payment",
    "processing",
    "complete",
    "failed",
    "expired",
  ]),
  settlementState: z.enum([
    "not_started",
    "payment_received",
    "funds_applied",
    "payment_failed",
  ]),
  paymentUrl: z.string().nullish(),
  resumed: z.boolean(),
  amountPaidMinorUnits: minorUnits.nullish(),
  currencyCode: z.string().nullish(),
  expiresAt: z.string().nullish(),
});
export const checkoutVoidSchema = z.discriminatedUnion("outcome", [
  z.object({ outcome: z.literal("voided") }),
  z.object({ outcome: z.literal("revealed"), payment: checkoutSchema }),
]);
export const availabilitySchema = z.object({
  available: z.boolean(),
  destinationReady: z.boolean(),
  cashableMinorUnits: minorUnits,
  onHoldMinorUnits: minorUnits,
  availableFrom: z.string().nullable(),
  minimumAmountMinorUnits: minorUnits,
  maximumAmountMinorUnits: minorUnits,
  currencyCode: z.string().min(1),
  statusMessage: z.string(),
});
export const destinationSchema = z.object({
  available: z.boolean(),
  opened: z.boolean(),
  readyToReceive: z.boolean(),
  detailsSubmitted: z.boolean(),
  outstandingRequirementCount: minorUnits,
  setupUrl: z.string().nullable(),
  setupUrlExpiresAt: z.string().nullable(),
  statusMessage: z.string(),
});
export const withdrawalSchema = z.object({
  id: recordIdSchema,
  status: z.string(),
  statusMessage: z.string(),
  amountMinorUnits: minorUnits.positive(),
  currencyCode: z.string(),
  requestedAt: z.string(),
  paidAt: z.string().nullable(),
  reversedAt: z.string().nullable(),
});
export const sellableSchema = z.object({
  projectTokenConfigId: recordIdSchema,
  projectId: recordIdSchema,
  unitName: z.string(),
  holderClass: holdingSchema.shape.holderClass.nullable(),
  sellableUnitCount: minorUnits,
  canSell: z.boolean(),
  statusMessage: z.string(),
});
export const saleQuoteSchema = z.object({
  quoteId: z.string().nullable(),
  expiresAt: z.string().nullable(),
  sourceUnitAmount: minorUnits.positive(),
  quantityBaseUnits: z
    .string()
    .max(10)
    .regex(/^[1-9]\d*$/u),
  platformMinorUnits: exactMinorUnits,
  currencyCode: z.literal("ARDA"),
  currencyDecimals: z.literal(2),
  canSell: z.boolean(),
  statusMessage: z.string(),
});
export const saleSchema = z.object({
  id: recordIdSchema,
  status: z.enum(["PENDING", "PROCESSING", "COMPLETED", "FAILED", "CANCELLED"]),
  sourceTokenAmount: minorUnits.positive(),
  quantityBaseUnits: z
    .string()
    .max(10)
    .regex(/^[1-9]\d*$/u),
  ardaMinorUnits: exactMinorUnits,
  currencyCode: z.literal("ARDA"),
  currencyDecimals: z.literal(2),
  requestedAt: z.string(),
  failureReason: z.string().nullish(),
});
export const handoffSchema = z.object({
  requestId: recordIdSchema,
  ownership: z.enum(["custodial", "claimed"]),
  status: z.enum(["pending", "approving", "approved", "declined"]),
  expiresAt: z.string(),
  delivery: z.literal("operator_stub"),
});
export type MemberHandoff = z.infer<typeof handoffSchema>;

export const cashOutVoidSchema = z.discriminatedUnion("outcome", [
  z.object({ outcome: z.literal("voided") }),
  z.object({ outcome: z.literal("revealed"), cashOut: withdrawalSchema }),
]);
export const saleVoidSchema = z.discriminatedUnion("outcome", [
  z.object({ outcome: z.literal("voided") }),
  z.object({ outcome: z.literal("revealed"), sale: saleSchema }),
]);

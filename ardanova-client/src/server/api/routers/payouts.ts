import { z } from "zod";
import { apiPath } from "~/lib/api/path";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  adminProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { getAdminApiClient } from "~/server/admin-api-client";

// ---------------------------------------------------------------------------
// Cashing out, in the two steps a person takes.
//
// Step one sells project units for platform funds. Step two sends those funds
// to their own bank. Neither step decides anything here: the backend runs one
// assessment for the quote and for the sale alike, so this router is a proxy
// that carries its wording through rather than a second place rules live.
// ---------------------------------------------------------------------------

const TokenHolderClassSchema = z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]);

const sellUnitsInputSchema = z
  .object({
    sourceProjectTokenConfigId: z.string().min(1),
    sourceTokenAmount: z.number().int().positive(),
    holderClass: TokenHolderClassSchema,
  })
  .strict();

const quoteSaleInputSchema = z
  .object({
    projectTokenConfigId: z.string().min(1),
    holderClass: TokenHolderClassSchema,
    units: z.number().int().positive(),
  })
  .strict();

const withdrawInputSchema = z
  .object({
    // Caller-stable so a double-tapped button resumes one withdrawal rather
    // than starting a second. The backend and the node both re-check the
    // alphabet; this keeps a bad key from ever becoming a request.
    requestKey: z
      .string()
      .min(1)
      .max(128)
      .regex(/^[A-Za-z0-9._:-]+$/),
    amountMinorUnits: z.number().int().positive(),
  })
  .strict();

// ---------------------------------------------------------------------------
// Response shapes
// ---------------------------------------------------------------------------

const sellableUnitsSchema = z.object({
  projectTokenConfigId: z.string(),
  projectId: z.string(),
  unitName: z.string(),
  // Nullable: a holding with no recorded holder class is listed but never sellable, and the
  // API refuses rather than inventing a class for it.
  holderClass: TokenHolderClassSchema.nullable(),
  heldUnits: z.number(),
  committedUnits: z.number(),
  restrictedUnits: z.number(),
  sellableUnitCount: z.number(),
  restrictedUntil: z.string().nullable(),
  canSell: z.boolean(),
  statusMessage: z.string(),
});

const saleQuoteSchema = z.object({
  projectTokenConfigId: z.string(),
  unitName: z.string(),
  sourceUnitAmount: z.number(),
  platformMinorUnits: z.number(),
  usdReferenceMinorUnits: z.number(),
  sellableUnitCount: z.number(),
  restrictedUntil: z.string().nullable(),
  canSell: z.boolean(),
  statusMessage: z.string(),
});

const withdrawAvailabilitySchema = z.object({
  available: z.boolean(),
  destinationReady: z.boolean(),
  cashableMinorUnits: z.number(),
  onHoldMinorUnits: z.number(),
  availableFrom: z.string().nullable(),
  minimumAmountMinorUnits: z.number(),
  maximumAmountMinorUnits: z.number(),
  currencyCode: z.string(),
  statusMessage: z.string(),
});

const withdrawDestinationSchema = z.object({
  available: z.boolean(),
  opened: z.boolean(),
  readyToReceive: z.boolean(),
  detailsSubmitted: z.boolean(),
  outstandingRequirementCount: z.number(),
  setupUrl: z.string().nullable(),
  setupUrlExpiresAt: z.string().nullable(),
  statusMessage: z.string(),
});

const withdrawalSchema = z.object({
  id: z.string(),
  status: z.string(),
  statusMessage: z.string(),
  amountMinorUnits: z.number(),
  currencyCode: z.string(),
  requestedAt: z.string(),
  paidAt: z.string().nullable(),
  reversedAt: z.string().nullable(),
});

export type SellableUnits = z.infer<typeof sellableUnitsSchema>;
export type SaleQuote = z.infer<typeof saleQuoteSchema>;
export type WithdrawAvailability = z.infer<typeof withdrawAvailabilitySchema>;
export type WithdrawDestination = z.infer<typeof withdrawDestinationSchema>;
export type Withdrawal = z.infer<typeof withdrawalSchema>;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Maps a backend refusal onto a tRPC code, keeping the backend's own wording.
 *
 * The status is the whole point: 403 is "these units, not now" and the message
 * is meant to be read; 503 is "this deployment cannot do it at all". Flattening
 * both to BAD_REQUEST tells a person they typed something wrong when they did not.
 */
function refusal(
  status: number,
  message: string | undefined,
  fallback: string,
) {
  const code =
    status === 401
      ? "UNAUTHORIZED"
      : status === 403
        ? "FORBIDDEN"
        : status === 404
          ? "NOT_FOUND"
          : status === 409
            ? "CONFLICT"
            : status === 503
              ? "SERVICE_UNAVAILABLE"
              : "BAD_REQUEST";

  return new TRPCError({ code, message: message ?? fallback });
}

function parsed<T>(schema: z.ZodType<T>, payload: unknown, label: string): T {
  const result = schema.safeParse(payload);
  if (!result.success) {
    throw new TRPCError({
      code: "INTERNAL_SERVER_ERROR",
      message: `The backend returned an invalid ${label} response.`,
    });
  }
  return result.data;
}

// ---------------------------------------------------------------------------
// Router
// ---------------------------------------------------------------------------

export const payoutsRouter = createTRPCRouter({
  // ---- Step one: sell units for funds ----

  /** Sells project units for platform funds. */
  sellUnits: protectedProcedure
    .input(sellUnitsInputSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.payouts.requestPayout({
        sourceProjectTokenConfigId: input.sourceProjectTokenConfigId,
        sourceTokenAmount: input.sourceTokenAmount,
        holderClass: input.holderClass,
      });

      if (response.error || !response.data) {
        throw refusal(
          response.status,
          response.error,
          "That sale could not be completed, so nothing was moved.",
        );
      }

      return response.data;
    }),

  /** What a sale would pay, checked against the same gates the sale itself faces. */
  quoteSale: protectedProcedure
    .input(quoteSaleInputSchema)
    .query(async ({ input }) => {
      const params = new URLSearchParams({
        projectTokenConfigId: input.projectTokenConfigId,
        holderClass: input.holderClass,
        units: String(input.units),
      });
      const response = await apiClient.get<unknown>(
        apiPath`/api/Payouts/quote?${params}`,
      );

      if (response.error || response.data === undefined) {
        throw refusal(
          response.status,
          response.error,
          "We could not work out what that sale would pay.",
        );
      }

      return parsed(saleQuoteSchema, response.data, "sale quote");
    }),

  /** Everything this person holds, with what is sellable now and what is still held. */
  getSellableUnits: protectedProcedure.query(async () => {
    const response = await apiClient.get<unknown>("/api/Payouts/sellable");

    if (response.error || response.data === undefined) {
      throw refusal(
        response.status,
        response.error,
        "Your units could not be loaded.",
      );
    }

    return parsed(
      z.array(sellableUnitsSchema),
      response.data,
      "sellable units",
    );
  }),

  /** Re-reads a sale the node left unfinished and settles the record on what it finds. */
  processPayout: adminProcedure
    .input(z.object({ payoutRequestId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().payouts.processPayout(
        input.payoutRequestId,
      );

      if (response.error || !response.data) {
        throw refusal(
          response.status,
          response.error,
          "That sale could not be reconciled.",
        );
      }

      return response.data;
    }),

  /** Calls off a sale that has not gone through, and returns the units. */
  cancelPayout: protectedProcedure
    .input(z.object({ payoutRequestId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.payouts.cancelPayout(
        input.payoutRequestId,
      );

      if (response.error || !response.data) {
        throw refusal(
          response.status,
          response.error,
          "That sale could not be called off.",
        );
      }

      return response.data;
    }),

  // ---- Queries over this person's own sales ----

  getPayoutsByUser: protectedProcedure.query(async () => {
    const response = await apiClient.payouts.getMine();

    if (response.error || !response.data) {
      throw refusal(
        response.status,
        response.error,
        "Your sales could not be loaded.",
      );
    }

    return response.data;
  }),

  getPendingPayouts: adminProcedure.query(async () => {
    const response = await getAdminApiClient().payouts.getPendingPayouts();

    if (response.error || !response.data) {
      throw refusal(
        response.status,
        response.error,
        "Outstanding sales could not be loaded.",
      );
    }

    return response.data;
  }),

  // ---- Step two: send the funds to a bank ----

  /** What this person can send to their bank today, and what is still held. */
  getWithdrawAvailability: protectedProcedure.query(async () => {
    const response = await apiClient.get<unknown>(
      "/api/Payouts/withdraw/availability",
    );

    if (response.error || response.data === undefined) {
      throw refusal(
        response.status,
        response.error,
        "We could not check what you can send right now.",
      );
    }

    return parsed(
      withdrawAvailabilitySchema,
      response.data,
      "withdrawal availability",
    );
  }),

  /** Where this person's money goes, and whether it is ready to receive any. */
  getWithdrawDestination: protectedProcedure.query(async () => {
    const response = await apiClient.get<unknown>(
      "/api/Payouts/withdraw/destination",
    );

    if (response.error || response.data === undefined) {
      throw refusal(
        response.status,
        response.error,
        "We could not check where your money would go.",
      );
    }

    return parsed(
      withdrawDestinationSchema,
      response.data,
      "withdrawal destination",
    );
  }),

  /** Starts or resumes setting up where this person's money goes. */
  beginWithdrawDestinationSetup: protectedProcedure.mutation(async () => {
    const response = await apiClient.post<unknown>(
      "/api/Payouts/withdraw/destination",
      {},
    );

    if (response.error || response.data === undefined) {
      throw refusal(
        response.status,
        response.error,
        "We could not start setting up where your money goes.",
      );
    }

    return parsed(
      withdrawDestinationSchema,
      response.data,
      "withdrawal destination",
    );
  }),

  /** Sends money to this person's own bank. The same request key never sends twice. */
  withdraw: protectedProcedure
    .input(withdrawInputSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<unknown>("/api/Payouts/withdraw", {
        requestKey: input.requestKey,
        amountMinorUnits: input.amountMinorUnits,
      });

      if (response.error || response.data === undefined) {
        throw refusal(
          response.status,
          response.error,
          "That withdrawal could not be started, so nothing was sent.",
        );
      }

      return parsed(withdrawalSchema, response.data, "withdrawal");
    }),

  /** This person's own withdrawals, newest first, as the backend records them. */
  getWithdrawals: protectedProcedure.query(async () => {
    const response = await apiClient.get<unknown>("/api/Payouts/withdraw");

    if (response.error || response.data === undefined) {
      throw refusal(
        response.status,
        response.error,
        "Your withdrawals could not be loaded.",
      );
    }

    return parsed(z.array(withdrawalSchema), response.data, "withdrawals");
  }),
});

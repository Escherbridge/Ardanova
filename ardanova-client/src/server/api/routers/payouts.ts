import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  adminProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import {
  PAYOUT_PROCESSING_PAUSED,
  PAYOUT_PROCESSING_PAUSED_MESSAGE,
} from "~/lib/commerce/portfolio-contract";
import { getAdminApiClient } from "~/server/admin-api-client";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const TokenHolderClassSchema = z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]);

const createPayoutRequestSchema = z.object({
  sourceProjectTokenConfigId: z.string().min(1),
  sourceTokenAmount: z.number().int().positive(),
  holderClass: TokenHolderClassSchema,
});

// ---------------------------------------------------------------------------
// Router - contract guard plus proxy to the .NET API
// ---------------------------------------------------------------------------

export const payoutsRouter = createTRPCRouter({
  // ---- Mutations ----

  requestPayout: protectedProcedure
    .input(createPayoutRequestSchema)
    .mutation(async ({ input }) => {
      if (PAYOUT_PROCESSING_PAUSED) {
        throw new TRPCError({
          code: "SERVICE_UNAVAILABLE",
          message: PAYOUT_PROCESSING_PAUSED_MESSAGE,
        });
      }

      const response = await apiClient.payouts.requestPayout({
        sourceProjectTokenConfigId: input.sourceProjectTokenConfigId,
        sourceTokenAmount: input.sourceTokenAmount,
        holderClass: input.holderClass,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: response.status === 503 ? "SERVICE_UNAVAILABLE" : "BAD_REQUEST",
          message: response.error ?? "Failed to request payout",
        });
      }

      return response.data;
    }),

  processPayout: adminProcedure
    .input(z.object({ payoutRequestId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().payouts.processPayout(
        input.payoutRequestId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: response.status === 503 ? "SERVICE_UNAVAILABLE" : "BAD_REQUEST",
          message: response.error ?? "Failed to process payout",
        });
      }

      return response.data;
    }),

  cancelPayout: protectedProcedure
    .input(z.object({ payoutRequestId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.payouts.cancelPayout(
        input.payoutRequestId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to cancel payout",
        });
      }

      return response.data;
    }),

  // ---- Queries ----

  getPayoutsByUser: protectedProcedure.query(async () => {
    const response = await apiClient.payouts.getMine();

    if (response.error || !response.data) {
      throw new TRPCError({
        code: "BAD_REQUEST",
        message: response.error ?? "Failed to get user payouts",
      });
    }

    return response.data;
  }),

  getPendingPayouts: adminProcedure.query(async () => {
    const response = await getAdminApiClient().payouts.getPendingPayouts();

    if (response.error || !response.data) {
      throw new TRPCError({
        code: "BAD_REQUEST",
        message: response.error ?? "Failed to get pending payouts",
      });
    }

    return response.data;
  }),
});

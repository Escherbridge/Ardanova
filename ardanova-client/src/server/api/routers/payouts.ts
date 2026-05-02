import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, adminProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const PayoutStatusSchema = z.enum(["PENDING", "PROCESSING", "COMPLETED", "FAILED", "CANCELLED"]);
const TokenHolderClassSchema = z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]);

const createPayoutRequestSchema = z.object({
  sourceProjectTokenConfigId: z.string().optional(),
  sourceTokenAmount: z.number().int().positive(),
  holderClass: TokenHolderClassSchema,
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const payoutsRouter = createTRPCRouter({
  // ---- Mutations ----

  requestPayout: protectedProcedure
    .input(z.object({ userId: z.string().min(1) }).merge(createPayoutRequestSchema))
    .mutation(async ({ input }) => {
      const response = await apiClient.payouts.requestPayout(input.userId, {
        sourceProjectTokenConfigId: input.sourceProjectTokenConfigId,
        sourceTokenAmount: input.sourceTokenAmount,
        holderClass: input.holderClass,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to request payout",
        });
      }

      return response.data;
    }),

  processPayout: adminProcedure
    .input(z.object({ payoutRequestId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.payouts.processPayout(input.payoutRequestId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to process payout",
        });
      }

      return response.data;
    }),

  cancelPayout: adminProcedure
    .input(z.object({ payoutRequestId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.payouts.cancelPayout(input.payoutRequestId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to cancel payout",
        });
      }

      return response.data;
    }),

  // ---- Queries ----

  getPayoutsByUser: protectedProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.payouts.getPayoutsByUser(input.userId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get user payouts",
        });
      }

      return response.data;
    }),

  getPendingPayouts: protectedProcedure
    .query(async () => {
      const response = await apiClient.payouts.getPendingPayouts();

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get pending payouts",
        });
      }

      return response.data;
    }),
});

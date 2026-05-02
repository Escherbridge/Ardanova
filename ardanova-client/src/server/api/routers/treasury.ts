import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, adminProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const TreasuryTransactionTypeSchema = z.enum([
  "FUNDING_INFLOW",
  "ALLOCATION_INDEX",
  "ALLOCATION_LIQUID",
  "ALLOCATION_OPS",
  "PAYOUT_DEBIT",
  "INDEX_RETURN",
  "PROFIT_SHARE",
  "REBALANCE",
  "TRUST_PROTECTION",
  "FOUNDER_BURN",
]);

const processFundingInflowSchema = z.object({
  usdAmount: z.number().positive(),
  projectId: z.string().optional(),
});

const rebalanceSchema = z.object({
  requiredLiquid: z.number().positive(),
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const treasuryRouter = createTRPCRouter({
  // ---- Queries ----

  getStatus: protectedProcedure
    .query(async () => {
      const response = await apiClient.treasury.getStatus();

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get treasury status",
        });
      }

      return response.data;
    }),

  getTransactions: protectedProcedure
    .input(z.object({ limit: z.number().int().positive().default(50).optional() }))
    .query(async ({ input }) => {
      const response = await apiClient.treasury.getTransactions(input.limit ?? 50);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get treasury transactions",
        });
      }

      return response.data;
    }),

  getExchangeTreasuryStatus: protectedProcedure
    .query(async () => {
      const response = await apiClient.treasury.getExchangeTreasuryStatus();

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get exchange treasury status",
        });
      }

      return response.data;
    }),

  // ---- Mutations ----

  processFundingInflow: adminProcedure
    .input(processFundingInflowSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.treasury.processFundingInflow(
        input.usdAmount,
        input.projectId
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to process funding inflow",
        });
      }

      return response.data;
    }),

  applyIndexReturn: adminProcedure
    .mutation(async () => {
      const response = await apiClient.treasury.applyIndexReturn();

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to apply index return",
        });
      }

      return response.data;
    }),

  rebalance: adminProcedure
    .input(rebalanceSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.treasury.rebalance(input.requiredLiquid);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to rebalance treasury",
        });
      }

      return response.data;
    }),

  reconcile: adminProcedure
    .mutation(async () => {
      const response = await apiClient.treasury.reconcile();

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to reconcile treasury",
        });
      }

      return response.data;
    }),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const TokenHolderClassSchema = z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]);

const getBalanceSchema = z.object({
  userId: z.string().min(1),
  projectTokenConfigId: z.string().min(1),
  holderClass: TokenHolderClassSchema,
});

const checkLiquiditySchema = z.object({
  userId: z.string().min(1),
  projectTokenConfigId: z.string().min(1),
  holderClass: TokenHolderClassSchema,
});

const getConversionPreviewSchema = z.object({
  projectTokenConfigId: z.string().min(1),
  tokenAmount: z.number().int().positive(),
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const tokenBalancesRouter = createTRPCRouter({
  // ---- Balance Queries ----

  getBalance: protectedProcedure
    .input(getBalanceSchema)
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getBalance(
        input.userId,
        input.projectTokenConfigId,
        input.holderClass
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Token balance not found",
        });
      }

      return response.data;
    }),

  getArdaBalance: protectedProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getArdaBalance(input.userId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "ARDA balance not found",
        });
      }

      return response.data;
    }),

  getPortfolio: protectedProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getPortfolio(input.userId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Portfolio not found",
        });
      }

      return response.data;
    }),

  checkLiquidity: protectedProcedure
    .input(checkLiquiditySchema)
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.checkLiquidity(
        input.userId,
        input.projectTokenConfigId,
        input.holderClass
      );

      if (response.error || response.data === undefined || response.data === null) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to check liquidity",
        });
      }

      return response.data;
    }),

  // ---- Exchange Rate Queries ----

  getProjectTokenValue: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getProjectTokenValue(input.configId);

      if (response.error || response.data === undefined || response.data === null) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Project token value not found",
        });
      }

      return response.data;
    }),

  getArdaValue: protectedProcedure
    .query(async () => {
      const response = await apiClient.tokenBalances.getArdaValue();

      if (response.error || response.data === undefined || response.data === null) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get ARDA value",
        });
      }

      return response.data;
    }),

  getConversionPreview: protectedProcedure
    .input(getConversionPreviewSchema)
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getConversionPreview(
        input.projectTokenConfigId,
        input.tokenAmount
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get conversion preview",
        });
      }

      return response.data;
    }),
});

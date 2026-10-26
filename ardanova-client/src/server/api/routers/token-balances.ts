import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const TokenHolderClassSchema = z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]);

const getBalanceSchema = z.object({
  projectTokenConfigId: z.string().min(1),
  holderClass: TokenHolderClassSchema,
});

const checkLiquiditySchema = z.object({
  projectTokenConfigId: z.string().min(1),
  holderClass: TokenHolderClassSchema,
});

const getConversionPreviewSchema = z.object({
  projectTokenConfigId: z.string().min(1),
  tokenAmount: z.number().int().positive(),
});

/** Backend status -> tRPC code for a failed read: 404 stays "not found", other 4xx stay rejections, 5xx / unknown are server faults; an empty 2xx keeps the read's own code. */
function readFailureCode(
  status: number | undefined,
  whenEmpty: "NOT_FOUND" | "BAD_REQUEST",
) {
  if (status !== undefined && status >= 200 && status < 300) return whenEmpty;
  if (status === 401) return "UNAUTHORIZED" as const;
  if (status === 403) return "FORBIDDEN" as const;
  if (status === 404) return "NOT_FOUND" as const;
  if (status !== undefined && status >= 400 && status < 500)
    return "BAD_REQUEST" as const;
  return "INTERNAL_SERVER_ERROR" as const;
}

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const tokenBalancesRouter = createTRPCRouter({
  // ---- Balance Queries ----

  getBalance: protectedProcedure
    .input(getBalanceSchema)
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getBalance(
        input.projectTokenConfigId,
        input.holderClass,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: readFailureCode(response.status, "NOT_FOUND"),
          message: response.error ?? "Token balance not found",
        });
      }

      return response.data;
    }),

  getArdaBalance: protectedProcedure.query(async () => {
    const response = await apiClient.tokenBalances.getArdaBalance();

    if (response.error || !response.data) {
      throw new TRPCError({
        code: readFailureCode(response.status, "NOT_FOUND"),
        message: response.error ?? "ARDA balance not found",
      });
    }

    return response.data;
  }),

  getPortfolio: protectedProcedure.query(async () => {
    const response = await apiClient.tokenBalances.getPortfolio();

    if (response.error || !response.data) {
      throw new TRPCError({
        code: readFailureCode(response.status, "NOT_FOUND"),
        message: response.error ?? "Portfolio not found",
      });
    }

    return response.data;
  }),

  checkLiquidity: protectedProcedure
    .input(checkLiquiditySchema)
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.checkLiquidity(
        input.projectTokenConfigId,
        input.holderClass,
      );

      if (
        response.error ||
        response.data === undefined ||
        response.data === null
      ) {
        throw new TRPCError({
          code: readFailureCode(response.status, "BAD_REQUEST"),
          message: response.error ?? "Failed to check liquidity",
        });
      }

      return response.data;
    }),

  // ---- Exchange Rate Queries ----

  getProjectTokenValue: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.tokenBalances.getProjectTokenValue(
        input.configId,
      );

      if (
        response.error ||
        response.data === undefined ||
        response.data === null
      ) {
        throw new TRPCError({
          code: readFailureCode(response.status, "NOT_FOUND"),
          message: response.error ?? "Project token value not found",
        });
      }

      return response.data;
    }),

  getArdaValue: protectedProcedure.query(async () => {
    const response = await apiClient.tokenBalances.getArdaValue();

    if (
      response.error ||
      response.data === undefined ||
      response.data === null
    ) {
      throw new TRPCError({
        code: readFailureCode(response.status, "BAD_REQUEST"),
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
        input.tokenAmount,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: readFailureCode(response.status, "BAD_REQUEST"),
          message: response.error ?? "Failed to get conversion preview",
        });
      }

      return response.data;
    }),
});

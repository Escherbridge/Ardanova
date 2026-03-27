import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const getConversionPreviewSchema = z.object({
  projectTokenConfigId: z.string().min(1),
  tokenAmount: z.number().int().positive(),
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const exchangeRouter = createTRPCRouter({
  // ---- Project Token Exchange ----

  getProjectTokenValue: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.exchange.getProjectTokenValue(input.configId);

      if (response.error || response.data === undefined || response.data === null) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Project token value not found",
        });
      }

      return response.data;
    }),

  // ---- ARDA Exchange ----

  getArdaValue: protectedProcedure
    .query(async () => {
      const response = await apiClient.exchange.getArdaValue();

      if (response.error || response.data === undefined || response.data === null) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get ARDA value",
        });
      }

      return response.data;
    }),

  // ---- Conversion Preview ----

  getConversionPreview: protectedProcedure
    .input(getConversionPreviewSchema)
    .query(async ({ input }) => {
      const response = await apiClient.exchange.getConversionPreview(
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

  // ---- Treasury Status ----

  getTreasuryStatus: protectedProcedure
    .query(async () => {
      const response = await apiClient.exchange.getTreasuryStatus();

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get treasury status",
        });
      }

      return response.data;
    }),
});

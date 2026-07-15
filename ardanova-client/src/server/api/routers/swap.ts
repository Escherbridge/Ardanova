import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export type SwapStatus = "PENDING" | "PROCESSING" | "COMPLETED" | "FAILED" | "CANCELLED";

export interface SwapPreviewDto {
  sourceTokenAmount: number;
  sourceUnitName: string;
  sourceUsdValue: number;
  ardaAmount: number;
  targetTokenAmount: number;
  targetUnitName: string;
  targetUsdValue: number;
  sourceTokenRate: number;
  targetTokenRate: number;
  ardaRate: number;
}

export interface SwapResultDto {
  id: string;
  userId: string;
  sourceConfigId: string;
  targetConfigId: string;
  sourceTokenAmount: number;
  sourceUnitName: string;
  sourceUsdValue: number;
  ardaAmount: number;
  targetTokenAmount: number;
  targetUnitName: string;
  targetUsdValue: number;
  status: SwapStatus;
  createdAt: string;
}

export interface SwapHistoryDto {
  id: string;
  sourceUnitName: string;
  sourceTokenAmount: number;
  targetUnitName: string;
  targetTokenAmount: number;
  sourceUsdValue: number;
  targetUsdValue: number;
  status: SwapStatus;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const swapPreviewInputSchema = z.object({
  // Retained temporarily for callers built before the server-owned identity
  // boundary. It is deliberately ignored; the session determines the account.
  userId: z.string().min(1).optional(),
  sourceConfigId: z.string().min(1),
  targetConfigId: z.string().min(1),
  sourceTokenAmount: z.number().int().positive(),
});

const executeSwapInputSchema = z.object({
  userId: z.string().min(1).optional(),
  sourceConfigId: z.string().min(1),
  targetConfigId: z.string().min(1),
  sourceTokenAmount: z.number().int().positive(),
});

// ---------------------------------------------------------------------------
// Router — thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const swapRouter = createTRPCRouter({
  getPreview: protectedProcedure
    .input(swapPreviewInputSchema)
    .query(async ({ input }) => {
      const params = new URLSearchParams({
        sourceConfigId: input.sourceConfigId,
        targetConfigId: input.targetConfigId,
        sourceTokenAmount: String(input.sourceTokenAmount),
      });
      const response = await (apiClient as unknown as { get: <T>(url: string) => Promise<{ data?: T; error?: string }> })
        .get<SwapPreviewDto>(`/api/Swaps/preview?${params.toString()}`);

      if (response.error ?? !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get swap preview",
        });
      }
      return response.data!;
    }),

  executeSwap: protectedProcedure
    .input(executeSwapInputSchema)
    .mutation(async ({ input }) => {
      const response = await (apiClient as unknown as { post: <T>(url: string, body: unknown) => Promise<{ data?: T; error?: string }> })
        .post<SwapResultDto>(
          "/api/Swaps",
          {
            sourceConfigId: input.sourceConfigId,
            targetConfigId: input.targetConfigId,
            sourceTokenAmount: input.sourceTokenAmount,
          },
        );

      if (response.error ?? !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to execute swap",
        });
      }
      return response.data!;
    }),

  getHistory: protectedProcedure
    .input(z.object({ userId: z.string().min(1).optional() }))
    .query(async () => {
      const response = await (apiClient as unknown as { get: <T>(url: string) => Promise<{ data?: T; error?: string }> })
        .get<SwapHistoryDto[]>("/api/Swaps/history");

      if (response.error ?? !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get swap history",
        });
      }
      return response.data!;
    }),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export type SwapStatus =
  | "PENDING"
  | "PROCESSING"
  | "COMPLETED"
  | "FAILED"
  | "CANCELLED";

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

const swapPreviewInputSchema = z
  .object({
    sourceConfigId: z.string().min(1),
    targetConfigId: z.string().min(1),
    sourceTokenAmount: z.number().int().positive(),
  })
  .strict();

const executeSwapInputSchema = z
  .object({
    sourceConfigId: z.string().min(1),
    targetConfigId: z.string().min(1),
    sourceTokenAmount: z.number().int().positive(),
  })
  .strict();

const swapStatusSchema = z.enum([
  "PENDING",
  "PROCESSING",
  "COMPLETED",
  "FAILED",
  "CANCELLED",
]);

const swapPreviewSchema = z
  .object({
    sourceTokenAmount: z.number().int().positive(),
    sourceUnitName: z.string().min(1),
    sourceUsdValue: z.number().finite().nonnegative(),
    ardaAmount: z.number().finite().nonnegative(),
    targetTokenAmount: z.number().int().nonnegative(),
    targetUnitName: z.string().min(1),
    targetUsdValue: z.number().finite().nonnegative(),
    sourceTokenRate: z.number().finite().nonnegative(),
    targetTokenRate: z.number().finite().nonnegative(),
    ardaRate: z.number().finite().nonnegative(),
  })
  .strict() satisfies z.ZodType<SwapPreviewDto>;

const swapResultSchema = z
  .object({
    id: z.string().min(1),
    userId: z.string().min(1),
    sourceConfigId: z.string().min(1),
    targetConfigId: z.string().min(1),
    sourceTokenAmount: z.number().int().positive(),
    sourceUnitName: z.string().min(1),
    sourceUsdValue: z.number().finite().nonnegative(),
    ardaAmount: z.number().finite().nonnegative(),
    targetTokenAmount: z.number().int().nonnegative(),
    targetUnitName: z.string().min(1),
    targetUsdValue: z.number().finite().nonnegative(),
    status: swapStatusSchema,
    createdAt: z.string().min(1),
  })
  .strict() satisfies z.ZodType<SwapResultDto>;

const swapHistoryItemSchema = z
  .object({
    id: z.string().min(1),
    sourceUnitName: z.string().min(1),
    sourceTokenAmount: z.number().int().positive(),
    targetUnitName: z.string().min(1),
    targetTokenAmount: z.number().int().nonnegative(),
    sourceUsdValue: z.number().finite().nonnegative(),
    targetUsdValue: z.number().finite().nonnegative(),
    status: swapStatusSchema,
    createdAt: z.string().min(1),
  })
  .strict() satisfies z.ZodType<SwapHistoryDto>;

const swapHistorySchema = z.array(swapHistoryItemSchema);

function parseSwapResponse<T>(
  schema: z.ZodType<T>,
  payload: unknown,
  label: string,
): T {
  const parsed = schema.safeParse(payload);
  if (!parsed.success) {
    throw new TRPCError({
      code: "INTERNAL_SERVER_ERROR",
      message: `The backend returned an invalid ${label} response.`,
    });
  }
  return parsed.data;
}

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
      const response = await apiClient.get<unknown>(
        `/api/Swaps/preview?${params.toString()}`,
      );

      if (response.error || response.data === undefined) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get swap preview",
        });
      }
      return parseSwapResponse(
        swapPreviewSchema,
        response.data,
        "swap preview",
      );
    }),

  executeSwap: protectedProcedure
    .input(executeSwapInputSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<unknown>("/api/Swaps", {
        sourceConfigId: input.sourceConfigId,
        targetConfigId: input.targetConfigId,
        sourceTokenAmount: input.sourceTokenAmount,
      });

      if (response.error || response.data === undefined) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to execute swap",
        });
      }
      return parseSwapResponse(swapResultSchema, response.data, "swap result");
    }),

  getHistory: protectedProcedure.query(async () => {
    const response = await apiClient.get<unknown>("/api/Swaps/history");

    if (response.error || response.data === undefined) {
      throw new TRPCError({
        code: "BAD_REQUEST",
        message: response.error ?? "Failed to get swap history",
      });
    }
    return parseSwapResponse(swapHistorySchema, response.data, "swap history");
  }),
});

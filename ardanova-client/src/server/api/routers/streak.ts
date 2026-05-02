import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const streakRouter = createTRPCRouter({
  // ---- Queries ----

  getMyStreak: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.streaks.getByUserId(ctx.session.user.id);
    if (response.error) {
      // No streak yet is an expected state, return null
      if (response.status === 404) return null;
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error,
      });
    }
    return response.data ?? null;
  }),

  // ---- Mutations ----

  recordActivity: protectedProcedure.mutation(async ({ ctx }) => {
    const response = await apiClient.streaks.recordActivity(
      ctx.session.user.id,
    );
    if (!response.data) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error ?? "Failed to record activity",
      });
    }
    return response.data;
  }),

  resetStreak: protectedProcedure.mutation(async ({ ctx }) => {
    const response = await apiClient.streaks.resetStreak(ctx.session.user.id);
    if (!response.data) {
      throw new TRPCError({
        code: "NOT_FOUND",
        message: response.error ?? "Streak not found",
      });
    }
    return response.data;
  }),
});

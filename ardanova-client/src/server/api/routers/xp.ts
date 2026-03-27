import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
  adminProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Router
// ---------------------------------------------------------------------------

export const xpRouter = createTRPCRouter({
  // ---- Queries ----

  /**
   * Get the current authenticated user's total XP.
   */
  getMyXP: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.xpEvents.getTotalXP(ctx.session.user.id);
    return { totalXP: response.data ?? 0 };
  }),

  /**
   * Get the current authenticated user's XP event history with pagination.
   */
  getMyXPHistory: protectedProcedure
    .input(
      z
        .object({
          eventType: z.string().optional(),
          limit: z.number().int().min(1).max(100).optional(),
          offset: z.number().int().min(0).optional(),
        })
        .optional(),
    )
    .query(async ({ input, ctx }) => {
      const response = await apiClient.xpEvents.getHistory(
        ctx.session.user.id,
        input?.eventType,
        input?.limit,
        input?.offset,
      );
      return response.data ?? [];
    }),

  /**
   * Get any user's total XP by ID (public, for leaderboards).
   */
  getXPByUser: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.xpEvents.getTotalXP(input.userId);
      return { totalXP: response.data ?? 0 };
    }),

  /**
   * Get the XP rewards configuration (public).
   */
  getRewards: publicProcedure.query(async () => {
    const response = await apiClient.xpEvents.getRewardsConfig();
    return response.data;
  }),

  // ---- Leveling Queries ----

  /**
   * Get the current authenticated user's level info including progress to
   * the next level.
   */
  getMyLevel: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.xpEvents.getSummary(ctx.session.user.id);
    if (!response.data)
      throw new TRPCError({ code: "NOT_FOUND" });
    return response.data;
  }),

  /**
   * Get XP thresholds for any level (public, for UI display).
   */
  getLevelInfo: publicProcedure
    .input(z.object({ level: z.number().int().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.xpEvents.getLevelInfo(input.level);
      return response.data;
    }),

  // ---- Mutations ----

  /**
   * Admin-only procedure to manually award XP to a user.
   * Most XP awarding happens internally via service calls from other services.
   */
  awardXP: adminProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        eventType: z.string().min(1),
        amount: z.number().int().min(1),
        source: z.string().min(1),
        sourceId: z.string().optional(),
        metadata: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.xpEvents.award({
        userId: input.userId,
        eventType: input.eventType,
        amount: input.amount,
        source: input.source,
        sourceId: input.sourceId,
        metadata: input.metadata,
      });
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to award XP",
        });
      return response.data;
    }),
});

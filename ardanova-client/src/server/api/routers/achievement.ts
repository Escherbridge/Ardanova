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

export const achievementRouter = createTRPCRouter({
  // ---- Queries ----

  /**
   * Get all achievements (public).
   */
  getAll: publicProcedure.query(async () => {
    const response = await apiClient.achievements.getAll();
    return response.data ?? [];
  }),

  /**
   * Get a single achievement by ID (public).
   */
  getById: publicProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.achievements.getById(input.id);
      if (!response.data)
        throw new TRPCError({ code: "NOT_FOUND", message: "Achievement not found" });
      return response.data;
    }),

  /**
   * Get achievements by category (public).
   */
  getByCategory: publicProcedure
    .input(z.object({ category: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.achievements.getByCategory(input.category);
      return response.data ?? [];
    }),

  /**
   * Get the current authenticated user's achievements.
   */
  getMyAchievements: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.achievements.getUserAchievements(
      ctx.session.user.id,
    );
    return response.data ?? [];
  }),

  /**
   * Get any user's achievements by user ID (public, for profiles).
   */
  getAchievementsByUser: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.achievements.getUserAchievements(input.userId);
      return response.data ?? [];
    }),

  // ---- Mutations ----

  /**
   * Admin-only: Create a new achievement.
   */
  create: adminProcedure
    .input(
      z.object({
        name: z.string().min(1),
        description: z.string().min(1),
        category: z.string().min(1),
        criteria: z.string().min(1),
        xpReward: z.number().int().min(0),
        equityReward: z.number().optional(),
        rarity: z.string().optional(),
        icon: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.achievements.create({
        name: input.name,
        description: input.description,
        category: input.category,
        criteria: input.criteria,
        xpReward: input.xpReward,
        equityReward: input.equityReward,
        rarity: input.rarity,
        icon: input.icon,
      });
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to create achievement",
        });
      return response.data;
    }),

  /**
   * Admin-only: Update an existing achievement.
   */
  update: adminProcedure
    .input(
      z.object({
        id: z.string().min(1),
        name: z.string().optional(),
        description: z.string().optional(),
        criteria: z.string().optional(),
        xpReward: z.number().int().min(0).optional(),
        equityReward: z.number().optional(),
        rarity: z.string().optional(),
        icon: z.string().optional(),
        isActive: z.boolean().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const { id, ...data } = input;
      const response = await apiClient.achievements.update(id, data);
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update achievement",
        });
      return response.data;
    }),

  /**
   * Admin-only: Delete an achievement.
   */
  delete: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.achievements.delete(input.id);
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to delete achievement",
        });
      return { success: true };
    }),

  /**
   * Admin-only: Update a user's progress on an achievement.
   */
  updateProgress: adminProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        achievementId: z.string().min(1),
        progress: z.number().int().min(0).max(100),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.achievements.updateProgress(
        input.userId,
        input.achievementId,
        { progress: input.progress },
      );
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update progress",
        });
      return response.data;
    }),

  /**
   * Admin-only: Award an achievement to a user.
   */
  awardAchievement: adminProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        achievementId: z.string().min(1),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.achievements.awardAchievement(
        input.userId,
        input.achievementId,
      );
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to award achievement",
        });
      return response.data;
    }),
});

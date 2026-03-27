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
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const leaderboardRouter = createTRPCRouter({
  // ---- Queries ----

  getById: publicProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.leaderboards.getById(input.id);
      if (!response.data)
        throw new TRPCError({ code: "NOT_FOUND", message: "Leaderboard not found" });
      return response.data;
    }),

  getByPeriod: publicProcedure
    .input(z.object({ period: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.leaderboards.getByPeriod(input.period);
      return response.data ?? [];
    }),

  getByCategory: publicProcedure
    .input(z.object({ category: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.leaderboards.getByCategory(input.category);
      return response.data ?? [];
    }),

  getEntries: publicProcedure
    .input(z.object({ leaderboardId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.leaderboards.getEntries(input.leaderboardId);
      return response.data ?? [];
    }),

  getMyRankings: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.leaderboards.getUserRankings(
      ctx.session.user.id,
    );
    return response.data ?? [];
  }),

  getUserRankings: publicProcedure
    .input(z.object({ userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.leaderboards.getUserRankings(input.userId);
      return response.data ?? [];
    }),

  // ---- Mutations ----

  create: adminProcedure
    .input(
      z.object({
        period: z.string().min(1),
        category: z.string().min(1),
        startDate: z.string().min(1),
        endDate: z.string().min(1),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.leaderboards.create({
        period: input.period,
        category: input.category,
        startDate: input.startDate,
        endDate: input.endDate,
      });
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to create leaderboard",
        });
      return response.data;
    }),

  delete: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.leaderboards.delete(input.id);
      if (response.error)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      return { success: true };
    }),

  addEntry: adminProcedure
    .input(
      z.object({
        leaderboardId: z.string().min(1),
        userId: z.string().min(1),
        score: z.number().int().min(0),
        metadata: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.leaderboards.addEntry({
        leaderboardId: input.leaderboardId,
        userId: input.userId,
        score: input.score,
        metadata: input.metadata,
      });
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to add entry",
        });
      return response.data;
    }),

  updateEntry: adminProcedure
    .input(
      z.object({
        entryId: z.string().min(1),
        score: z.number().int().optional(),
        rank: z.number().int().optional(),
        metadata: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const { entryId, ...data } = input;
      const response = await apiClient.leaderboards.updateEntry(entryId, data);
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update entry",
        });
      return response.data;
    }),

  deleteEntry: adminProcedure
    .input(z.object({ entryId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.leaderboards.deleteEntry(input.entryId);
      if (response.error)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      return { success: true };
    }),
});

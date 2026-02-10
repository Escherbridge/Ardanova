import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const userRouter = createTRPCRouter({
  // Get all users with pagination
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.users.getAll(input.page, input.limit);
      if (response.error) {
        throw new Error(response.error);
      }
      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage ? String(input.page + 1) : undefined,
        totalCount: response.data?.totalCount,
        totalPages: response.data?.totalPages,
      };
    }),

  // Search users
  search: publicProcedure
    .input(
      z.object({
        query: z.string(),
        page: z.number().min(1).default(1),
        limit: z.number().min(1).max(100).default(20),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.users.search(input.query, input.page, input.limit);
      if (response.error) {
        throw new Error(response.error);
      }
      return {
        items: response.data?.items ?? [],
        totalCount: response.data?.totalCount,
      };
    }),

  // Get user by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getById(input.id);
      if (response.error || !response.data) {
        throw new Error(response.error ?? "User not found");
      }
      return response.data;
    }),

  // Follow a user
  follow: protectedProcedure
    .input(z.object({ userId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const followerId = ctx.session.user.id;
      const response = await apiClient.users.follow(input.userId, followerId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data;
    }),

  // Unfollow a user
  unfollow: protectedProcedure
    .input(z.object({ userId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const followerId = ctx.session.user.id;
      const response = await apiClient.users.unfollow(input.userId, followerId);
      if (response.error) {
        throw new Error(response.error);
      }
      return true;
    }),

  // Get followers of a user
  getFollowers: publicProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowers(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? [];
    }),

  // Get who a user is following
  getFollowing: publicProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowing(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? [];
    }),

  // Get who a user is following with full user details
  getFollowingWithUsers: protectedProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowing(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      const follows = response.data ?? [];

      const userResults = await Promise.all(
        follows.map(async (follow) => {
          const userResp = await apiClient.users.getById(follow.followingId);
          if (userResp.error || !userResp.data) return null;
          return userResp.data;
        })
      );

      return userResults.filter((u): u is NonNullable<typeof u> => u !== null);
    }),

  // Check if current user follows another user
  isFollowing: protectedProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input, ctx }) => {
      const followerId = ctx.session.user.id;
      const response = await apiClient.users.isFollowing(input.userId, followerId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? false;
    }),

  // Get follow counts for a user
  getFollowCounts: publicProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowCounts(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? { followersCount: 0, followingCount: 0 };
    }),
});

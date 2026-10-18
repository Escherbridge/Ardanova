import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { User } from "~/lib/api/ardanova/endpoints/users";

const privateUserFields = [
  "email",
  "emailVerified",
  "phone",
  "role",
  "trustScore",
] as const;

/** A user as anyone may see them; contact and authority fields belong to the user alone. */
export type PublicUser = Omit<User, (typeof privateUserFields)[number]> & {
  email?: string;
};

/** Strips contact and authority fields; see `src/server/auth/AGENTS.md` §User directory privacy. */
export function toPublicUser(user: User): PublicUser {
  const projected: Partial<User> = { ...user };
  for (const field of privateUserFields) delete projected[field];
  return projected as PublicUser;
}

/** A record id safe to place in an API path segment; see `src/server/auth/AGENTS.md` §Path-safe ids. */
export const recordIdSchema = z
  .string()
  .regex(/^[A-Za-z0-9_-]{1,128}$/, "Invalid id");

/** Returns the record only when the backend answered with the user that was asked for. */
export function requireRequestedUser(user: User | undefined, requestedId: string): User {
  if (!user || typeof user !== "object" || user.id !== requestedId) {
    throw new TRPCError({ code: "NOT_FOUND", message: "User not found" });
  }
  return user;
}

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
        items: (response.data?.items ?? []).map(toPublicUser),
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
        items: (response.data?.items ?? []).map(toPublicUser),
        totalCount: response.data?.totalCount,
      };
    }),

  // Get user by ID; the full record only for the signed-in user themself
  getById: publicProcedure
    .input(z.object({ id: recordIdSchema }))
    .query(async ({ input, ctx }): Promise<User | PublicUser> => {
      const response = await apiClient.users.getById(input.id);
      if (response.error) throw new Error(response.error);
      const user = requireRequestedUser(response.data, input.id);
      return ctx.session?.user.id === user.id ? user : toPublicUser(user);
    }),

  // Follow a user
  follow: protectedProcedure
    .input(z.object({ userId: recordIdSchema }))
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
    .input(z.object({ userId: recordIdSchema }))
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
    .input(z.object({ userId: recordIdSchema }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowers(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? [];
    }),

  // Get who a user is following
  getFollowing: publicProcedure
    .input(z.object({ userId: recordIdSchema }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowing(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? [];
    }),

  // Get who a user is following with full user details
  getFollowingWithUsers: protectedProcedure
    .input(z.object({ userId: recordIdSchema }))
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
          return toPublicUser(userResp.data);
        })
      );

      return userResults.filter((u): u is NonNullable<typeof u> => u !== null);
    }),

  // Check if current user follows another user
  isFollowing: protectedProcedure
    .input(z.object({ userId: recordIdSchema }))
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
    .input(z.object({ userId: recordIdSchema }))
    .query(async ({ input }) => {
      const response = await apiClient.users.getFollowCounts(input.userId);
      if (response.error) {
        throw new Error(response.error);
      }
      return response.data ?? { followersCount: 0, followingCount: 0 };
    }),
});

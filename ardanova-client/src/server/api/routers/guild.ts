import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient, CreateGuildSchema, UpdateGuildSchema, CreateGuildReviewSchema } from "~/lib/api";

export const guildRouter = createTRPCRouter({
  // Get all guilds with pagination
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        verified: z.boolean().optional(),
      })
    )
    .query(async ({ input }) => {
      const { limit, page, verified } = input;

      // If filtering by verified, use dedicated endpoint
      if (verified) {
        const response = await apiClient.guilds.getVerified();
        if (response.error) {
          throw new Error(response.error);
        }
        return { items: response.data ?? [], nextCursor: undefined };
      }

      // Default: get paged results
      const response = await apiClient.guilds.getPaged(page, limit);
      if (response.error) {
        throw new Error(response.error);
      }

      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage ? String(page + 1) : undefined,
        totalCount: response.data?.totalCount,
        totalPages: response.data?.totalPages,
      };
    }),

  // Get guild by ID or slug
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // Try by ID first
      let response = await apiClient.guilds.getById(input.id);

      // If not found by ID, try by slug
      if (response.status === 404) {
        response = await apiClient.guilds.getBySlug(input.id);
      }

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Guild not found");
      }

      return response.data;
    }),

  // Get guild by slug
  getBySlug: publicProcedure
    .input(z.object({ slug: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getBySlug(input.slug);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Guild not found");
      }

      return response.data;
    }),

  // Get user's guild (as owner)
  getMyGuild: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;

    const response = await apiClient.guilds.getByOwnerId(userId);

    if (response.status === 404) {
      return null; // User doesn't own a guild
    }

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data;
  }),

  // Get verified guilds
  getVerified: publicProcedure.query(async () => {
    const response = await apiClient.guilds.getVerified();

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Create a new guild
  create: protectedProcedure
    .input(CreateGuildSchema.omit({ ownerId: true }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Check if user already owns a guild
      const existing = await apiClient.guilds.getByOwnerId(userId);
      if (existing.data) {
        throw new Error("You already own a guild");
      }

      const response = await apiClient.guilds.create({
        ...input,
        ownerId: userId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create guild");
      }

      return response.data;
    }),

  // Update guild
  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        data: UpdateGuildSchema,
      })
    )
    .mutation(async ({ input, ctx }) => {
      const { id, data } = input;
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.guilds.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Guild not found");
      }

      if (existing.data.ownerId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.guilds.update(id, data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update guild");
      }

      return response.data;
    }),

  // Delete guild
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const { id } = input;
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.guilds.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Guild not found");
      }

      if (existing.data.ownerId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.guilds.delete(id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete guild");
      }

      return { success: true };
    }),

  // Get guild members
  getMembers: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getMembers(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Add member to guild
  addMember: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        userId: z.string(),
        role: z.string(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const sessionUserId = ctx.session.user.id;

      // Verify ownership
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== sessionUserId) {
        throw new Error("Only the guild owner can add members");
      }

      const response = await apiClient.guilds.addMember(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add member");
      }

      return response.data;
    }),

  // Remove member from guild
  removeMember: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        memberId: z.string(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const { guildId, memberId } = input;
      const sessionUserId = ctx.session.user.id;

      // Verify ownership
      const guild = await apiClient.guilds.getById(guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== sessionUserId) {
        throw new Error("Only the guild owner can remove members");
      }

      const response = await apiClient.guilds.removeMember(guildId, memberId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to remove member");
      }

      return { success: true };
    }),

  // Get guild reviews
  getReviews: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getReviews(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Create a review for a guild
  createReview: protectedProcedure
    .input(CreateGuildReviewSchema.omit({ userId: true }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.guilds.createReview({
        ...input,
        userId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create review");
      }

      return response.data;
    }),

  // Delete a review
  deleteReview: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        reviewId: z.string(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const { guildId, reviewId } = input;
      const userId = ctx.session.user.id;

      // Get review to verify ownership
      const reviews = await apiClient.guilds.getReviews(guildId);
      const review = reviews.data?.find((r) => r.id === reviewId);

      if (!review) {
        throw new Error("Review not found");
      }

      if (review.userId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.guilds.deleteReview(guildId, reviewId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete review");
      }

      return { success: true };
    }),

  // Get guild's bids
  getBids: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify user is owner or member of guild
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      // For now, only allow owner to view bids
      if (guild.data.ownerId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.guilds.getBids(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),
});

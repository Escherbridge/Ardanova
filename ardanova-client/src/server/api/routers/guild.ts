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

  /** Guilds owned by a user (for public profiles). API may return one guild or an array. */
  getGuildsForOwner: publicProcedure
    .input(z.object({ ownerId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getByOwnerId(input.ownerId);
      if (response.error || response.status === 404 || !response.data) {
        return [];
      }
      const raw = response.data;
      const list = Array.isArray(raw) ? raw : [raw];
      return list.filter(Boolean);
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

  // Get user's guilds (as owner or with ADMIN/RECRUITER role)
  getMyGuilds: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const guilds = [];

    // Get guild where user is owner
    const ownedGuildResponse = await apiClient.guilds.getByOwnerId(userId);
    if (ownedGuildResponse.data) {
      guilds.push(ownedGuildResponse.data);
    }

    // Note: Currently no backend endpoint to efficiently get guilds by member role.
    // If needed in the future, add an API endpoint like:
    // GET /api/guilds/member/{userId}?roles=ADMIN,RECRUITER
    // For now, this returns guilds where user is owner.

    return guilds;
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
        name: input.name,
        description: input.description,
        email: input.email,
        phone: input.phone,
        logoUrl: input.logoUrl,
        website: input.website,
        slug: input.slug,
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

      const response = await apiClient.guilds.addMember({
        guildId: input.guildId,
        userId: input.userId,
        role: input.role,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add member");
      }

      return response.data;
    }),

  updateMember: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        memberId: z.string(),
        role: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const sessionUserId = ctx.session.user.id;
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }
      if (guild.data.ownerId !== sessionUserId) {
        throw new Error("Only the guild owner can update members");
      }
      const response = await apiClient.guilds.updateMember(input.guildId, input.memberId, {
        role: input.role,
      });
      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update member");
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
    .input(CreateGuildReviewSchema.omit({ reviewerId: true }).extend({ guildId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.guilds.createReview({
        guildId: input.guildId,
        reviewerId: userId,
        rating: input.rating,
        title: input.title,
        content: input.content,
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

      if (review.reviewerId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.guilds.deleteReview(guildId, reviewId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete review");
      }

      return { success: true };
    }),

  // ========================================
  // GUILD UPDATES
  // ========================================

  // Get updates for a guild
  getUpdates: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getUpdates(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Create an update (owner only)
  createUpdate: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        title: z.string().min(1),
        content: z.string().min(10),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== userId) {
        throw new Error("Only the guild owner can create updates");
      }

      const response = await apiClient.guilds.createUpdate(input.guildId, {
        title: input.title,
        content: input.content,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create update");
      }

      return response.data;
    }),

  // Delete an update
  deleteUpdate: protectedProcedure
    .input(z.object({ guildId: z.string(), updateId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== userId) {
        throw new Error("Only the guild owner can delete updates");
      }

      const response = await apiClient.guilds.deleteUpdate(input.guildId, input.updateId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete update");
      }

      return { success: true };
    }),

  // ========================================
  // GUILD APPLICATIONS
  // ========================================

  // Get applications (owner only)
  getApplications: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== userId) {
        throw new Error("Only the guild owner can view applications");
      }

      const response = await apiClient.guilds.getApplications(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Submit application
  submitApplication: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        requestedRole: z.string().min(1),
        message: z.string().min(20),
        skills: z.string().optional(),
        experience: z.string().optional(),
        portfolio: z.string().url().optional(),
        availability: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.guilds.submitApplication(input.guildId, {
        userId: userId,
        requestedRole: input.requestedRole,
        message: input.message,
        skills: input.skills,
        experience: input.experience,
        portfolio: input.portfolio,
        availability: input.availability,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to submit application");
      }

      return response.data;
    }),

  // Accept/reject application (owner only)
  reviewApplication: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        applicationId: z.string(),
        status: z.enum(["APPROVED", "REJECTED"]),
        reviewMessage: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get application to verify ownership
      const application = await apiClient.guilds.getApplicationById(input.guildId, input.applicationId);
      if (application.error || !application.data) {
        throw new Error("Application not found");
      }

      // Verify guild ownership
      const guild = await apiClient.guilds.getById(application.data.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== userId) {
        throw new Error("Only the guild owner can review applications");
      }

      // Call the appropriate endpoint based on status
      const reviewData = { reviewMessage: input.reviewMessage };
      const response = input.status === "APPROVED"
        ? await apiClient.guilds.acceptApplication(input.guildId, input.applicationId, reviewData)
        : await apiClient.guilds.rejectApplication(input.guildId, input.applicationId, reviewData);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to review application");
      }

      return response.data;
    }),

  // ========================================
  // GUILD INVITATIONS
  // ========================================

  // Get invitations sent by guild (owner only)
  getInvitations: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      if (guild.data.ownerId !== userId) {
        throw new Error("Only the guild owner can view invitations");
      }

      const response = await apiClient.guilds.getInvitations(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Create invitation (owner only)
  createInvitation: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        invitedUserId: z.string().optional(),
        invitedEmail: z.string().email().optional(),
        role: z.string(),
        message: z.string().optional(),
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
        throw new Error("Only the guild owner can create invitations");
      }

      const response = await apiClient.guilds.createInvitation(input.guildId, {
        invitedById: sessionUserId,
        invitedUserId: input.invitedUserId,
        invitedEmail: input.invitedEmail,
        role: input.role,
        message: input.message,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create invitation");
      }

      return response.data;
    }),

  // Accept/reject invitation (invited user)
  respondToInvitation: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        invitationId: z.string(),
        accept: z.boolean(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get invitation to verify recipient
      const invitation = await apiClient.guilds.getInvitationById(input.guildId, input.invitationId);
      if (invitation.error || !invitation.data) {
        throw new Error("Invitation not found");
      }

      if (invitation.data.invitedUserId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.guilds.respondToInvitation(input.guildId, input.invitationId, {
        accept: input.accept,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to respond to invitation");
      }

      return response.data;
    }),

  // ========================================
  // GUILD FOLLOWS
  // ========================================

  // Get followers
  getFollowers: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getFollowers(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Follow guild
  followGuild: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.guilds.followGuild(input.guildId, userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to follow guild");
      }

      return response.data;
    }),

  // Unfollow guild
  unfollowGuild: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.guilds.unfollowGuild(input.guildId, userId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to unfollow guild");
      }

      return { success: true };
    }),

  // Check if user follows guild
  isFollowing: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.guilds.isFollowing(input.guildId, userId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? false;
    }),

});

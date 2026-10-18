import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  protectedProcedure,
  publicProcedure,
} from "~/server/api/trpc";
import {
  apiClient,
  CreateGuildSchema,
  UpdateGuildSchema,
  CreateGuildReviewSchema,
  type ApiResponse,
  type Guild,
} from "~/lib/api";
import { GrantableGuildRoleSchema } from "~/lib/api/ardanova/endpoints/guilds";

/** Thin proxy: who may act on a guild is decided by the .NET guild services from the signed actor (Services/AGENTS.md §Guild authority). */

function normalizeGuilds(value: Guild | Guild[] | null | undefined): Guild[] {
  if (!value) return [];
  return Array.isArray(value) ? value : [value];
}

/** Turns a backend refusal into the tRPC error that carries its plain-language reason to the reader. */
function apiError(response: ApiResponse<unknown>, fallback: string): TRPCError {
  const code =
    response.status === 401
      ? "UNAUTHORIZED"
      : response.status === 403
        ? "FORBIDDEN"
        : response.status === 404
          ? "NOT_FOUND"
          : response.status === 409
            ? "CONFLICT"
            : response.status === 400
              ? "BAD_REQUEST"
              : "INTERNAL_SERVER_ERROR";
  return new TRPCError({ code, message: response.error ?? fallback });
}

/** Returns the payload of a successful call, or throws the backend's refusal. */
function unwrap<T>(response: ApiResponse<T>, fallback: string): T {
  if (response.error || response.data === undefined || response.data === null) {
    throw apiError(response, fallback);
  }
  return response.data;
}

/** For calls that answer 204 No Content. */
function ensureOk(response: ApiResponse<unknown>, fallback: string): { success: true } {
  if (response.error) throw apiError(response, fallback);
  return { success: true };
}

export const guildRouter = createTRPCRouter({
  // Get all guilds with pagination
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        verified: z.boolean().optional(),
      }),
    )
    .query(async ({ input }) => {
      const { limit, page, verified } = input;

      if (verified) {
        const response = await apiClient.guilds.getVerified();
        if (response.error) throw apiError(response, "Failed to load guilds");
        return { items: response.data ?? [], nextCursor: undefined };
      }

      const response = await apiClient.guilds.getPaged(page, limit);
      if (response.error) throw apiError(response, "Failed to load guilds");

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
      let response = await apiClient.guilds.getById(input.id);
      if (response.status === 404) {
        response = await apiClient.guilds.getBySlug(input.id);
      }
      return unwrap(response, "Guild not found");
    }),

  getBySlug: publicProcedure
    .input(z.object({ slug: z.string() }))
    .query(async ({ input }) =>
      unwrap(await apiClient.guilds.getBySlug(input.slug), "Guild not found"),
    ),

  /** Guilds owned by a user (for public profiles). API may return one guild or an array. */
  getGuildsForOwner: publicProcedure
    .input(z.object({ ownerId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getByOwnerId(input.ownerId);
      if (response.error || response.status === 404 || !response.data) {
        return [];
      }
      return normalizeGuilds(response.data);
    }),

  // Get user's guild (as owner)
  getMyGuild: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.guilds.getByOwnerId(ctx.session.user.id);
    if (response.status === 404) return null;
    if (response.error) throw apiError(response, "Failed to load your guild");
    return normalizeGuilds(response.data)[0] ?? null;
  }),

  // Get user's guilds (as owner)
  getMyGuilds: protectedProcedure.query(async ({ ctx }) => {
    const ownedGuildResponse = await apiClient.guilds.getByOwnerId(
      ctx.session.user.id,
    );
    return normalizeGuilds(ownedGuildResponse.data);
  }),

  getVerified: publicProcedure.query(async () => {
    const response = await apiClient.guilds.getVerified();
    if (response.error) throw apiError(response, "Failed to load guilds");
    return response.data ?? [];
  }),

  /** The backend makes the signed actor the owner and refuses a second guild for the same owner. */
  create: protectedProcedure
    .input(CreateGuildSchema)
    .mutation(async ({ input }) =>
      unwrap(await apiClient.guilds.create(input), "Failed to create guild"),
    ),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: UpdateGuildSchema }))
    .mutation(async ({ input }) =>
      unwrap(
        await apiClient.guilds.update(input.id, input.data),
        "Failed to update guild",
      ),
    ),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) =>
      ensureOk(await apiClient.guilds.delete(input.id), "Failed to delete guild"),
    ),

  getMembers: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getMembers(input.guildId);
      if (response.error) throw apiError(response, "Failed to load members");
      return response.data ?? [];
    }),

  addMember: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        userId: z.string(),
        role: GrantableGuildRoleSchema,
      }),
    )
    .mutation(async ({ input }) =>
      unwrap(
        await apiClient.guilds.addMember(input.guildId, {
          userId: input.userId,
          role: input.role,
        }),
        "Failed to add member",
      ),
    ),

  updateMember: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        memberId: z.string(),
        role: GrantableGuildRoleSchema.optional(),
      }),
    )
    .mutation(async ({ input }) =>
      unwrap(
        await apiClient.guilds.updateMember(input.guildId, input.memberId, {
          role: input.role,
        }),
        "Failed to update member",
      ),
    ),

  removeMember: protectedProcedure
    .input(z.object({ guildId: z.string(), memberId: z.string() }))
    .mutation(async ({ input }) =>
      ensureOk(
        await apiClient.guilds.removeMember(input.guildId, input.memberId),
        "Failed to remove member",
      ),
    ),

  getReviews: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getReviews(input.guildId);
      if (response.error) throw apiError(response, "Failed to load reviews");
      return response.data ?? [];
    }),

  createReview: protectedProcedure
    .input(CreateGuildReviewSchema.extend({ guildId: z.string() }))
    .mutation(async ({ input }) => {
      const { guildId, ...review } = input;
      return unwrap(
        await apiClient.guilds.createReview(guildId, review),
        "Failed to create review",
      );
    }),

  deleteReview: protectedProcedure
    .input(z.object({ guildId: z.string(), reviewId: z.string() }))
    .mutation(async ({ input }) =>
      ensureOk(
        await apiClient.guilds.deleteReview(input.guildId, input.reviewId),
        "Failed to delete review",
      ),
    ),

  // ========================================
  // GUILD UPDATES
  // ========================================

  getUpdates: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getUpdates(input.guildId);
      if (response.error) throw apiError(response, "Failed to load updates");
      return response.data ?? [];
    }),

  createUpdate: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        title: z.string().min(1),
        content: z.string().min(10),
      }),
    )
    .mutation(async ({ input }) =>
      unwrap(
        await apiClient.guilds.createUpdate(input.guildId, {
          title: input.title,
          content: input.content,
        }),
        "Failed to create update",
      ),
    ),

  deleteUpdate: protectedProcedure
    .input(z.object({ guildId: z.string(), updateId: z.string() }))
    .mutation(async ({ input }) =>
      ensureOk(
        await apiClient.guilds.deleteUpdate(input.guildId, input.updateId),
        "Failed to delete update",
      ),
    ),

  // ========================================
  // GUILD APPLICATIONS
  // ========================================

  getApplications: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getApplications(input.guildId);
      if (response.error) throw apiError(response, "Failed to load applications");
      return response.data ?? [];
    }),

  submitApplication: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        requestedRole: GrantableGuildRoleSchema,
        message: z.string().min(20),
        skills: z.string().optional(),
        experience: z.string().optional(),
        portfolio: z.string().url().optional(),
        availability: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const { guildId, ...application } = input;
      return unwrap(
        await apiClient.guilds.submitApplication(guildId, application),
        "Failed to submit application",
      );
    }),

  reviewApplication: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        applicationId: z.string(),
        status: z.enum(["APPROVED", "REJECTED"]),
        reviewMessage: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const reviewData = { reviewMessage: input.reviewMessage };
      const response =
        input.status === "APPROVED"
          ? await apiClient.guilds.acceptApplication(
              input.guildId,
              input.applicationId,
              reviewData,
            )
          : await apiClient.guilds.rejectApplication(
              input.guildId,
              input.applicationId,
              reviewData,
            );
      return unwrap(response, "Failed to review application");
    }),

  // ========================================
  // GUILD INVITATIONS
  // ========================================

  getInvitations: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getInvitations(input.guildId);
      if (response.error) throw apiError(response, "Failed to load invitations");
      return response.data ?? [];
    }),

  /** Invitations addressed to the signed-in member that can still be answered. */
  getMyInvitations: protectedProcedure.query(async () => {
    const response = await apiClient.guilds.getMyInvitations();
    if (response.error) throw apiError(response, "Failed to load your invitations");
    return response.data ?? [];
  }),

  createInvitation: protectedProcedure
    .input(
      z
        .object({
          guildId: z.string(),
          invitedUserId: z.string().optional(),
          invitedEmail: z.string().email().optional(),
          role: GrantableGuildRoleSchema,
          message: z.string().optional(),
        })
        .refine((v) => v.invitedUserId ?? v.invitedEmail, {
          message: "Name the person to invite by their account email",
          path: ["invitedEmail"],
        }),
    )
    .mutation(async ({ input }) => {
      const { guildId, ...invitation } = input;
      return unwrap(
        await apiClient.guilds.createInvitation(guildId, invitation),
        "Failed to create invitation",
      );
    }),

  respondToInvitation: protectedProcedure
    .input(
      z.object({
        guildId: z.string(),
        invitationId: z.string(),
        accept: z.boolean(),
      }),
    )
    .mutation(async ({ input }) =>
      unwrap(
        await apiClient.guilds.respondToInvitation(
          input.guildId,
          input.invitationId,
          { accept: input.accept },
        ),
        "Failed to respond to invitation",
      ),
    ),

  // ========================================
  // GUILD FOLLOWS
  // ========================================

  getFollowers: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.guilds.getFollowers(input.guildId);
      if (response.error) throw apiError(response, "Failed to load followers");
      return response.data ?? [];
    }),

  followGuild: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .mutation(async ({ input }) =>
      unwrap(
        await apiClient.guilds.followGuild(input.guildId),
        "Failed to follow guild",
      ),
    ),

  unfollowGuild: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .mutation(async ({ input }) =>
      ensureOk(
        await apiClient.guilds.unfollowGuild(input.guildId),
        "Failed to unfollow guild",
      ),
    ),

  isFollowing: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input, ctx }) => {
      const response = await apiClient.guilds.isFollowing(
        input.guildId,
        ctx.session.user.id,
      );
      if (response.error) throw apiError(response, "Failed to read follow state");
      return response.data ?? false;
    }),
});

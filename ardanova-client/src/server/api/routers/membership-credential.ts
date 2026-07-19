import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const MembershipGrantTypeSchema = z.enum([
  "FOUNDER",
  "DAO_VOTE",
  "CONTRIBUTION_THRESHOLD",
  "APPLICATION_APPROVED",
  "GAME_SDK_THRESHOLD",
]);

const grantMembershipCredentialSchema = z.object({
  projectId: z.string().min(1),
  userId: z.string().min(1),
  grantedVia: MembershipGrantTypeSchema,
  grantedByProposalId: z.string().min(1).optional(),
});

function membershipMutationsUnavailable(): never {
  throw new TRPCError({
    code: "NOT_IMPLEMENTED",
    message:
      "Membership credential changes are paused until scope and grant authority are enforced by the backend. A client-supplied grant reason is never authorization.",
  });
}

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const membershipCredentialRouter = createTRPCRouter({
  // ---- Queries ----

  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getByProjectId(
        input.projectId,
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  getActiveByProjectId: publicProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response =
        await apiClient.membershipCredentials.getActiveByProjectId(
          input.projectId,
        );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getById(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Membership credential not found",
        });
      }
      return response.data;
    }),

  getByUserId: protectedProcedure
    .input(z.object({ userId: z.string().optional() }))
    .query(async ({ input, ctx }) => {
      const userId = input.userId ?? ctx.session.user.id;
      const response =
        await apiClient.membershipCredentials.getByUserId(userId);
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  getMyCredential: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response =
        await apiClient.membershipCredentials.getByProjectAndUser(
          input.projectId,
          userId,
        );
      // Return null if not found (expected for users without credentials)
      if (response.error || !response.data) {
        return null;
      }
      return response.data;
    }),

  getByGuildId: publicProcedure
    .input(z.object({ guildId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getByGuildId(
        input.guildId,
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  getActiveByGuildId: publicProcedure
    .input(z.object({ guildId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getActiveByGuildId(
        input.guildId,
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  checkCredential: publicProcedure
    .input(
      z.object({ projectId: z.string().min(1), userId: z.string().min(1) }),
    )
    .query(async ({ input }) => {
      const response =
        await apiClient.membershipCredentials.getByProjectAndUser(
          input.projectId,
          input.userId,
        );

      if (response.error || !response.data) {
        return { hasCredential: false, status: null as string | null };
      }

      return { hasCredential: true, status: response.data.status };
    }),

  // ---- Mutations ----

  grant: protectedProcedure
    .input(grantMembershipCredentialSchema)
    .mutation(membershipMutationsUnavailable),

  revoke: protectedProcedure
    .input(
      z.object({ id: z.string().min(1), revokeTxHash: z.string().optional() }),
    )
    .mutation(membershipMutationsUnavailable),

  suspend: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(membershipMutationsUnavailable),

  reactivate: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(membershipMutationsUnavailable),

  updateTier: protectedProcedure
    .input(
      z.object({
        credentialId: z.string().min(1),
        tier: z.enum(["BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND"]),
      }),
    )
    .mutation(membershipMutationsUnavailable),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { MembershipGrantType } from "~/lib/api/ardanova/endpoints/membership-credentials";

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

// ---------------------------------------------------------------------------
// Helper: verify caller is project owner via apiClient
// ---------------------------------------------------------------------------

async function verifyProjectOwner(projectId: string, callerId: string) {
  const projectResponse = await apiClient.projects.getById(projectId);

  if (projectResponse.error || !projectResponse.data) {
    throw new TRPCError({ code: "NOT_FOUND", message: "Project not found" });
  }

  if (projectResponse.data.createdById !== callerId) {
    throw new TRPCError({
      code: "FORBIDDEN",
      message: "Only the project owner can perform this action",
    });
  }

  return projectResponse.data;
}

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const membershipCredentialRouter = createTRPCRouter({
  // ---- Queries ----

  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getByProjectId(input.projectId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getActiveByProjectId: publicProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getActiveByProjectId(input.projectId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
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
      const response = await apiClient.membershipCredentials.getByUserId(userId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getMyCredential: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.membershipCredentials.getByProjectAndUser(
        input.projectId,
        userId
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
      const response = await apiClient.membershipCredentials.getByGuildId(input.guildId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getActiveByGuildId: publicProcedure
    .input(z.object({ guildId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getActiveByGuildId(input.guildId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  checkCredential: publicProcedure
    .input(z.object({ projectId: z.string().min(1), userId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getByProjectAndUser(
        input.projectId,
        input.userId
      );

      if (response.error || !response.data) {
        return { hasCredential: false, status: null as string | null };
      }

      return { hasCredential: true, status: response.data.status };
    }),

  // ---- Mutations ----

  grant: protectedProcedure
    .input(grantMembershipCredentialSchema)
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      // For DAO_VOTE grants, the proposal system handles authorization
      // For other grants, only the project owner can grant
      if (input.grantedVia !== "DAO_VOTE") {
        await verifyProjectOwner(input.projectId, callerId);
      }

      const response = await apiClient.membershipCredentials.grant({
        projectId: input.projectId,
        userId: input.userId,
        grantedVia: input.grantedVia as MembershipGrantType,
        grantedByProposalId: input.grantedByProposalId,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to grant membership credential",
        });
      }

      return response.data;
    }),

  revoke: protectedProcedure
    .input(z.object({ id: z.string().min(1), revokeTxHash: z.string().optional() }))
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      // Fetch credential to find associated project for authorization
      const credentialResponse = await apiClient.membershipCredentials.getById(input.id);
      if (credentialResponse.error || !credentialResponse.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(credentialResponse.data.projectId, callerId);

      const response = await apiClient.membershipCredentials.revoke(input.id, {
        revokeTxHash: input.revokeTxHash,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to revoke membership credential",
        });
      }

      return response.data;
    }),

  suspend: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      const credentialResponse = await apiClient.membershipCredentials.getById(input.id);
      if (credentialResponse.error || !credentialResponse.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(credentialResponse.data.projectId, callerId);

      const response = await apiClient.membershipCredentials.suspend(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to suspend membership credential",
        });
      }

      return response.data;
    }),

  reactivate: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      const credentialResponse = await apiClient.membershipCredentials.getById(input.id);
      if (credentialResponse.error || !credentialResponse.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(credentialResponse.data.projectId, callerId);

      const response = await apiClient.membershipCredentials.reactivate(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to reactivate membership credential",
        });
      }

      return response.data;
    }),

  updateTier: protectedProcedure
    .input(
      z.object({
        credentialId: z.string().min(1),
        tier: z.enum(["BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND"]),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      const credentialResponse = await apiClient.membershipCredentials.getById(input.credentialId);
      if (credentialResponse.error || !credentialResponse.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(credentialResponse.data.projectId, callerId);

      const response = await apiClient.membershipCredentials.updateTier(input.credentialId, {
        tier: input.tier,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to update credential tier",
        });
      }

      return response.data;
    }),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
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

const grantAndMintSchema = z.object({
  projectId: z.string().optional(),
  guildId: z.string().optional(),
  userId: z.string().min(1),
  grantedVia: MembershipGrantTypeSchema,
  grantedByProposalId: z.string().optional(),
});

const upgradeTierSchema = z.object({
  tier: z.string().min(1),
});

const checkAutoGrantSchema = z.object({
  userId: z.string().min(1),
  projectId: z.string().optional(),
  guildId: z.string().optional(),
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const credentialUtilityRouter = createTRPCRouter({
  // ---- Mutations ----

  grantAndMint: protectedProcedure
    .input(grantAndMintSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.credentialUtility.grantAndMint({
        projectId: input.projectId,
        guildId: input.guildId,
        userId: input.userId,
        grantedVia: input.grantedVia,
        grantedByProposalId: input.grantedByProposalId,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to grant and mint credential",
        });
      }

      return response.data;
    }),

  revokeAndBurn: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.credentialUtility.revokeAndBurn(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to revoke and burn credential",
        });
      }

      return response.data;
    }),

  upgradeTier: protectedProcedure
    .input(z.object({ id: z.string().min(1) }).merge(upgradeTierSchema))
    .mutation(async ({ input }) => {
      const response = await apiClient.credentialUtility.upgradeTier(input.id, {
        tier: input.tier,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to upgrade credential tier",
        });
      }

      return response.data;
    }),

  checkAutoGrant: protectedProcedure
    .input(checkAutoGrantSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.credentialUtility.checkAutoGrant({
        userId: input.userId,
        projectId: input.projectId,
        guildId: input.guildId,
      });

      if (response.error) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error,
        });
      }

      // Return null if no credential was auto-granted (expected behavior)
      return response.data ?? null;
    }),

  retryMint: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.credentialUtility.retryMint(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to retry credential mint",
        });
      }

      return response.data;
    }),

  // ---- Queries ----

  getChainData: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.credentialUtility.getChainData(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Credential not found",
        });
      }

      return response.data;
    }),
});

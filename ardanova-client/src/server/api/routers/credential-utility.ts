import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  adminProcedure,
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

function credentialMutationsUnavailable(): never {
  throw new TRPCError({
    code: "NOT_IMPLEMENTED",
    message:
      "Credential changes are paused until scope and grant authority are bound and audited by the backend.",
  });
}

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const credentialUtilityRouter = createTRPCRouter({
  // ---- Mutations ----

  grantAndMint: protectedProcedure
    .input(grantAndMintSchema)
    .mutation(credentialMutationsUnavailable),

  revokeAndBurn: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(credentialMutationsUnavailable),

  upgradeTier: adminProcedure
    .input(z.object({ id: z.string().min(1) }).merge(upgradeTierSchema))
    .mutation(credentialMutationsUnavailable),

  checkAutoGrant: protectedProcedure
    .input(checkAutoGrantSchema)
    .mutation(credentialMutationsUnavailable),

  retryMint: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(credentialMutationsUnavailable),

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

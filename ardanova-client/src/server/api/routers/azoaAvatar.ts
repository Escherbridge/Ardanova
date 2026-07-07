import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// AZOA Avatar router — THIN PROXY → .NET (no Prisma, no business logic).
//
// Every procedure targets the authenticated user (`ctx.session.user.id`) only;
// the user id is never accepted from client input — that is what keeps these
// endpoints IDOR-safe. Business logic (idempotent register, wallet binding,
// Tier-2 readiness, KYC posture) lives entirely in the .NET backend.
// ---------------------------------------------------------------------------

export const azoaAvatarRouter = createTRPCRouter({
  /**
   * Read the current user's AZOA avatar/wallet linkage status.
   */
  getStatus: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.azoaAvatar.getStatus(ctx.session.user.id);
    if (!response.data)
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error ?? "Failed to fetch AZOA avatar status",
      });
    return response.data;
  }),

  /**
   * Idempotently link the current user to an AZOA avatar.
   * A node 403 KYC_FORBIDDEN surfaces as a FORBIDDEN tRPC error the client can
   * translate into an actionable "KYC required to transact" message.
   */
  ensureAvatar: protectedProcedure.mutation(async ({ ctx }) => {
    const response = await apiClient.azoaAvatar.ensureAvatar(ctx.session.user.id);
    if (!response.data)
      throw new TRPCError({
        code: response.status === 403 ? "FORBIDDEN" : "INTERNAL_SERVER_ERROR",
        message: response.error ?? "Failed to ensure AZOA avatar",
      });
    return response.data;
  }),

  /**
   * Ensure a wallet reference is bound/cached for the current user's avatar.
   */
  ensureWallet: protectedProcedure.mutation(async ({ ctx }) => {
    const response = await apiClient.azoaAvatar.ensureWallet(ctx.session.user.id);
    if (!response.data)
      throw new TRPCError({
        code: response.status === 403 ? "FORBIDDEN" : "INTERNAL_SERVER_ERROR",
        message: response.error ?? "Failed to ensure AZOA wallet",
      });
    return response.data;
  }),
});

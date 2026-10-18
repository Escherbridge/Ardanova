import { z } from "zod";
import { segment } from "~/lib/api/path";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { ApiResponse } from "~/lib/api";
import type { MembershipCredential } from "~/lib/api/ardanova/endpoints/membership-credentials";

// ---------------------------------------------------------------------------
// Scope-scoped credential routes.
//
// The scope and the recipient travel in the PATH and these calls carry no body
// at all. That is deliberate: the previous body-scoped grant let the caller name
// the project, the recipient, the grant reason and a proposal id, which is four
// separate ways to nominate an authority you do not hold. The API derives the
// grant reason from its own records and refuses anything else, so there is
// nothing left for this router to send.
// ---------------------------------------------------------------------------

const CREDENTIALS_BASE = "/api/membershipcredentials";
const UTILITY_BASE = "/api/CredentialUtility";

const projectScope = z.object({
  projectId: z.string().min(1),
  userId: z.string().min(1),
});

const guildScope = z.object({
  guildId: z.string().min(1),
  userId: z.string().min(1),
});

function scopePath(base: string, scope: { projectId: string } | { guildId: string }, userId: string) {
  const scoped =
    "projectId" in scope
      ? `project/${segment(scope.projectId)}`
      : `guild/${segment(scope.guildId)}`;
  return `${base}/${scoped}/user/${segment(userId)}`;
}

/** Turns an API refusal into the tRPC code that matches it, so the UI can tell them apart. */
function unwrap<T>(response: ApiResponse<T>, fallbackMessage: string): T {
  if (!response.error && response.status >= 200 && response.status < 300) {
    return response.data as T;
  }

  const message = response.error ?? fallbackMessage;
  const code =
    response.status === 403
      ? "FORBIDDEN"
      : response.status === 404
        ? "NOT_FOUND"
        : response.status === 409
          ? "CONFLICT"
          : response.status === 400
            ? "BAD_REQUEST"
            : response.status === 501
              ? "NOT_IMPLEMENTED"
              : "INTERNAL_SERVER_ERROR";

  throw new TRPCError({ code, message });
}

// ---------------------------------------------------------------------------
// Router - thin proxy to the .NET API via apiClient
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

  /**
   * Issues a credential to a member of a project.
   *
   * `grantedVia` is accepted only so existing callers keep type-checking; it is
   * NOT sent. The API decides the grant reason from the recipient's standing in
   * the project, which is the whole point: a manager cannot label a grant.
   */
  grant: protectedProcedure
    .input(
      projectScope.extend({
        grantedVia: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.post<MembershipCredential>(
        scopePath(CREDENTIALS_BASE, { projectId: input.projectId }, input.userId),
        {},
      );
      return unwrap(response, "The credential could not be issued.");
    }),

  /** Issues a credential to a member of a guild. */
  grantForGuild: protectedProcedure
    .input(guildScope)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<MembershipCredential>(
        scopePath(CREDENTIALS_BASE, { guildId: input.guildId }, input.userId),
        {},
      );
      return unwrap(response, "The credential could not be issued.");
    }),

  /** Withdraws a member's project credential and returns the issued proof. */
  withdraw: protectedProcedure
    .input(projectScope)
    .mutation(async ({ input }) => {
      const response = await apiClient.delete<MembershipCredential>(
        scopePath(CREDENTIALS_BASE, { projectId: input.projectId }, input.userId),
      );
      return unwrap(response, "The credential could not be withdrawn.");
    }),

  /** Pauses a member's project credential without withdrawing it. */
  pause: protectedProcedure
    .input(projectScope)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<MembershipCredential>(
        `${scopePath(CREDENTIALS_BASE, { projectId: input.projectId }, input.userId)}/pause`,
        {},
      );
      return unwrap(response, "The credential could not be paused.");
    }),

  /** Restores a paused project credential. */
  resume: protectedProcedure
    .input(projectScope)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<MembershipCredential>(
        `${scopePath(CREDENTIALS_BASE, { projectId: input.projectId }, input.userId)}/resume`,
        {},
      );
      return unwrap(response, "The credential could not be restored.");
    }),

  /** Re-submits a verification-pending credential for issuance. */
  reissue: protectedProcedure
    .input(projectScope)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<MembershipCredential>(
        `${scopePath(UTILITY_BASE, { projectId: input.projectId }, input.userId)}/reissue`,
        {},
      );
      return unwrap(response, "Verification could not be completed. Try again shortly.");
    }),

  /**
   * Retained so existing callers keep type-checking, and refused on purpose.
   *
   * A member's tier is earned from what they have contributed; a credential
   * carries a copy of it and cannot hold a different value. There is no longer
   * anything for a project to set here.
   */
  updateTier: protectedProcedure
    .input(
      z.object({
        credentialId: z.string().min(1),
        tier: z.enum(["BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND"]),
      }),
    )
    .mutation((): never => {
      throw new TRPCError({
        code: "CONFLICT",
        message:
          "A member's level is earned from their contributions, so it cannot be set here. " +
          "It updates on its own as they contribute.",
      });
    }),
});

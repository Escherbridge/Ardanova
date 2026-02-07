import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, adminProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const userRoleSchema = z.enum(["INDIVIDUAL", "GUILD", "ADMIN"]);
const userTypeSchema = z.enum([
  "INNOVATOR",
  "SUPPORTER",
  "VOLUNTEER",
  "FREELANCER",
  "SME_OWNER",
  "GUILD_MEMBER",
]);
const verificationLevelSchema = z.enum([
  "ANONYMOUS",
  "VERIFIED",
  "PRO",
  "EXPERT",
]);

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient (admin-only)
// ---------------------------------------------------------------------------

export const adminRouter = createTRPCRouter({
  /**
   * Update a user's role (admin only).
   */
  updateUserRole: adminProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        role: userRoleSchema,
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.users.updateRole(input.userId, {
        role: input.role,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: response.status === 404 ? "NOT_FOUND" : "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update user role",
        });
      }

      return response.data;
    }),

  /**
   * Update a user's user type (admin only).
   */
  updateUserType: adminProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        userType: userTypeSchema,
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.users.updateUserType(input.userId, {
        userType: input.userType,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: response.status === 404 ? "NOT_FOUND" : "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update user type",
        });
      }

      return response.data;
    }),

  /**
   * Update a user's verification level (admin only).
   */
  updateVerificationLevel: adminProcedure
    .input(
      z.object({
        userId: z.string().min(1),
        verificationLevel: verificationLevelSchema,
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.users.updateVerificationLevel(
        input.userId,
        { verificationLevel: input.verificationLevel },
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: response.status === 404 ? "NOT_FOUND" : "INTERNAL_SERVER_ERROR",
          message:
            response.error ?? "Failed to update user verification level",
        });
      }

      return response.data;
    }),
});

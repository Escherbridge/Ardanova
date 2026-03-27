import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
  adminProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const referralRouter = createTRPCRouter({
  // ---- Queries ----

  getById: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.referrals.getById(input.id);
      if (!response.data)
        throw new TRPCError({ code: "NOT_FOUND", message: "Referral not found" });
      return response.data;
    }),

  getMyReferrals: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.referrals.getByReferrerId(
      ctx.session.user.id,
    );
    return response.data ?? [];
  }),

  getByReferrerId: protectedProcedure
    .input(z.object({ referrerId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.referrals.getByReferrerId(input.referrerId);
      return response.data ?? [];
    }),

  getByReferredId: protectedProcedure
    .input(z.object({ referredId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.referrals.getByReferredId(input.referredId);
      if (!response.data) {
        if (response.status === 404) return null;
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to fetch referral",
        });
      }
      return response.data;
    }),

  getByCode: publicProcedure
    .input(z.object({ code: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.referrals.getByCode(input.code);
      if (!response.data) {
        if (response.status === 404) return null;
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to fetch referral",
        });
      }
      return response.data;
    }),

  // ---- Mutations ----

  create: protectedProcedure
    .input(
      z.object({
        referredId: z.string().min(1),
        referralCode: z.string().optional(),
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.referrals.create({
        referrerId: ctx.session.user.id,
        referredId: input.referredId,
        referralCode: input.referralCode,
      });
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to create referral",
        });
      return response.data;
    }),

  complete: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.referrals.complete(input.id);
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to complete referral",
        });
      return response.data;
    }),

  claimReward: protectedProcedure
    .input(
      z.object({
        id: z.string().min(1),
        xpAmount: z.number().int().min(0),
        equityAmount: z.number().optional(),
        tokenAmount: z.number().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.referrals.claimReward(input.id, {
        xpAmount: input.xpAmount,
        equityAmount: input.equityAmount,
        tokenAmount: input.tokenAmount,
      });
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to claim reward",
        });
      return response.data;
    }),

  expire: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.referrals.expire(input.id);
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to expire referral",
        });
      return response.data;
    }),

  cancel: adminProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.referrals.cancel(input.id);
      if (!response.data)
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to cancel referral",
        });
      return response.data;
    }),
});

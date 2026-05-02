import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const walletRouter = createTRPCRouter({
  getMyWallets: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.wallets.getByUserId(userId);
    if (response.error) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
    }
    return response.data ?? [];
  }),

  getPrimary: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.wallets.getPrimary(userId);
    if (response.error && response.status !== 404) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
    }
    return response.data ?? null;
  }),

  create: protectedProcedure
    .input(
      z.object({
        address: z.string().min(1),
        provider: z.string().optional(),
        label: z.string().optional(),
        isPrimary: z.boolean().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.wallets.create({
        userId,
        address: input.address,
        provider: input.provider,
        label: input.label,
        isPrimary: input.isPrimary,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to create wallet" });
      }
      return response.data;
    }),

  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        label: z.string().optional(),
        isPrimary: z.boolean().optional(),
      })
    )
    .mutation(async ({ input }) => {
      const { id, ...data } = input;
      const response = await apiClient.wallets.update(id, data);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to update wallet" });
      }
      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.delete(input.id);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return { success: true };
    }),

  verify: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.verify(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to verify wallet" });
      }
      return response.data;
    }),

  setPrimary: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.setPrimary(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to set primary" });
      }
      return response.data;
    }),
});

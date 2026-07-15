import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const walletRouter = createTRPCRouter({
  getMyWallets: protectedProcedure.query(async () => {
    const response = await apiClient.wallets.getMine();
    if (response.error) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error,
      });
    }
    return response.data ?? [];
  }),

  getPrimary: protectedProcedure.query(async () => {
    const response = await apiClient.wallets.getMyPrimary();
    if (response.error && response.status !== 404) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error,
      });
    }
    return response.data ?? null;
  }),

  create: protectedProcedure
    .input(
      z.object({
        address: z.string().min(1),
        provider: z.enum([
          "PERA",
          "DEFLY",
          "ALGOSIGNER",
          "WALLETCONNECT",
          "OTHER",
        ]),
        label: z.string().optional(),
        isPrimary: z.boolean().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.create({
        address: input.address,
        provider: input.provider,
        label: input.label,
        isPrimary: input.isPrimary,
      });
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to create wallet",
        });
      }
      return response.data;
    }),

  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        label: z.string().optional(),
        isPrimary: z.boolean().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const { id, ...data } = input;
      const response = await apiClient.wallets.update(id, data);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update wallet",
        });
      }
      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.delete(input.id);
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return { success: true };
    }),

  issueVerificationChallenge: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.issueVerificationChallenge(
        input.id,
      );
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message:
            response.error ?? "Failed to issue wallet verification challenge",
        });
      }
      return response.data;
    }),

  completeVerificationChallenge: protectedProcedure
    .input(
      z.object({
        id: z.string().min(1),
        challengeId: z.string().min(1),
        nonce: z.string().min(1),
        signature: z.string().min(1),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.completeVerificationChallenge(
        input.id,
        {
          challengeId: input.challengeId,
          nonce: input.nonce,
          signature: input.signature,
        },
      );
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to complete wallet verification",
        });
      }
      return response.data;
    }),

  setPrimary: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.wallets.setPrimary(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to set primary",
        });
      }
      return response.data;
    }),
});

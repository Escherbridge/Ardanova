import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const taskEscrowRouter = createTRPCRouter({
  getByTaskId: protectedProcedure
    .input(z.object({ taskId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.taskEscrows.getByTaskId(input.taskId);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "NOT_FOUND", message: response.error ?? "Escrow not found" });
      }
      return response.data;
    }),

  getByFunderId: protectedProcedure
    .query(async () => {
      const response = await apiClient.taskEscrows.getMine();
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  create: protectedProcedure
    .input(
      z.object({
        taskId: z.string(),
        shareId: z.string(),
        amount: z.number(),
        txHashFund: z.string().optional(),
      })
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.create({
        taskId: input.taskId,
        shareId: input.shareId,
        amount: input.amount,
        txHashFund: input.txHashFund,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to create escrow" });
      }
      return response.data;
    }),

  release: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        txHash: z.string().optional(),
      })
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.release(input.id, { txHash: input.txHash });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to release" });
      }
      return response.data;
    }),

  dispute: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.dispute(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to dispute" });
      }
      return response.data;
    }),

  refund: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        txHash: z.string().optional(),
      })
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.refund(input.id, { txHash: input.txHash });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to refund" });
      }
      return response.data;
    }),
});

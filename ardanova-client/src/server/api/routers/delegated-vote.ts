import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const delegatedVoteRouter = createTRPCRouter({
  getByDelegator: protectedProcedure
    .input(z.object({ delegatorId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getByDelegator(input.delegatorId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getByDelegatee: protectedProcedure
    .input(z.object({ delegateeId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getByDelegatee(input.delegateeId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getByProject: protectedProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getByProject(input.projectId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getActiveByProject: protectedProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getActiveByProject(input.projectId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getTotalPower: protectedProcedure
    .input(
      z.object({
        delegateeId: z.string(),
        projectId: z.string(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getTotalPower(input.delegateeId, input.projectId);
      if (response.error && response.status !== 404) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? 0;
    }),

  create: protectedProcedure
    .input(
      z.object({
        projectId: z.string(),
        delegatorId: z.string(),
        delegateeId: z.string(),
        shareId: z.string(),
        amount: z.number(),
        expiresAt: z.string().optional(),
      })
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.delegatedVotes.create({
        projectId: input.projectId,
        delegatorId: input.delegatorId,
        delegateeId: input.delegateeId,
        shareId: input.shareId,
        amount: input.amount,
        expiresAt: input.expiresAt,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to create delegation" });
      }
      return response.data;
    }),

  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        amount: z.number().optional(),
        expiresAt: z.string().optional(),
      })
    )
    .mutation(async ({ input }) => {
      const { id, ...data } = input;
      const response = await apiClient.delegatedVotes.update(id, data);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to update delegation" });
      }
      return response.data;
    }),

  revoke: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.delegatedVotes.revoke(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to revoke" });
      }
      return response.data;
    }),
});

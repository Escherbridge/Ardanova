import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

function delegatedVoteMutationsUnavailable(): never {
  throw new TRPCError({
    code: "NOT_IMPLEMENTED",
    message:
      "Vote delegation changes are paused until the backend binds the delegator and verifies project authority atomically.",
  });
}

export const delegatedVoteRouter = createTRPCRouter({
  getByDelegator: protectedProcedure
    .input(z.object({ delegatorId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getByDelegator(
        input.delegatorId,
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  getByDelegatee: protectedProcedure
    .input(z.object({ delegateeId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getByDelegatee(
        input.delegateeId,
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  getByProject: protectedProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getByProject(
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

  getActiveByProject: protectedProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getActiveByProject(
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

  getTotalPower: protectedProcedure
    .input(
      z.object({
        delegateeId: z.string(),
        projectId: z.string(),
      }),
    )
    .query(async ({ input }) => {
      const response = await apiClient.delegatedVotes.getTotalPower(
        input.delegateeId,
        input.projectId,
      );
      if (response.error && response.status !== 404) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
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
      }),
    )
    .mutation(delegatedVoteMutationsUnavailable),

  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        amount: z.number().optional(),
        expiresAt: z.string().optional(),
      }),
    )
    .mutation(delegatedVoteMutationsUnavailable),

  revoke: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(delegatedVoteMutationsUnavailable),
});

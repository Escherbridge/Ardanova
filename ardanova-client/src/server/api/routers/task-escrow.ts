import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  protectedProcedure,
  adminProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { getAdminApiClient } from "~/server/admin-api-client";
import { taskEscrowDtoSchema } from "~/lib/contracts/task-escrow-contract";

const taskEscrowListSchema = z.array(taskEscrowDtoSchema);

function parseEscrow(data: unknown) {
  const parsed = taskEscrowDtoSchema.safeParse(data);
  if (!parsed.success) {
    throw new TRPCError({
      code: "INTERNAL_SERVER_ERROR",
      message: "Backend returned an invalid task-escrow contract",
    });
  }
  return parsed.data;
}

export const taskEscrowRouter = createTRPCRouter({
  getByTaskId: protectedProcedure
    .input(z.object({ taskId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.taskEscrows.getByTaskId(input.taskId);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Escrow not found",
        });
      }
      return parseEscrow(response.data);
    }),

  getByFunderId: protectedProcedure.query(async () => {
    const response = await apiClient.taskEscrows.getMine();
    if (response.error) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error,
      });
    }
    const parsed = taskEscrowListSchema.safeParse(response.data ?? []);
    if (!parsed.success) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: "Backend returned an invalid task-escrow list contract",
      });
    }
    return parsed.data;
  }),

  release: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.release(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to release",
        });
      }
      return parseEscrow(response.data);
    }),

  dispute: protectedProcedure
    .input(
      z.object({
        id: z.string().min(1),
        reason: z.enum([
          "SCOPE_DISPUTE",
          "QUALITY_ISSUE",
          "NON_DELIVERY",
          "OTHER",
        ]),
        description: z.string().trim().min(20).max(4000),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.dispute(input.id, {
        reason: input.reason,
        description: input.description,
      });
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to dispute",
        });
      }
      return parseEscrow(response.data);
    }),

  refund: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        reason: z.string().trim().min(1).max(2000).optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.refund(input.id, {
        reason: input.reason,
      });
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to refund",
        });
      }
      return parseEscrow(response.data);
    }),

  /** Administrator adjudication of a disputed escrow. See TaskEscrowsController.Resolve. */
  resolve: adminProcedure
    .input(
      z.object({
        id: z.string().min(1),
        outcome: z.enum(["REFUND", "REJECT"]),
      }),
    )
    .mutation(async ({ ctx, input }) => {
      const response = await getAdminApiClient().taskEscrows.resolve(
        input.id,
        {
          outcome: input.outcome,
          resolvedByUserId: ctx.session.user.id,
        },
      );
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to resolve dispute",
        });
      }
      return parseEscrow(response.data);
    }),

  /** All disputed escrows, for the admin adjudication queue. Admin-credentialed, not funder-scoped. */
  listDisputed: adminProcedure.query(async () => {
    const response = await getAdminApiClient().taskEscrows.getAllDisputed();
    if (response.error) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error,
      });
    }
    const parsed = taskEscrowListSchema.safeParse(response.data ?? []);
    if (!parsed.success) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: "Backend returned an invalid task-escrow list contract",
      });
    }
    return parsed.data;
  }),
});

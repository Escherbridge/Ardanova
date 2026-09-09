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

/**
 * The .NET API's refusals carry meaning the UI has to keep: 403 is "not your decision to make",
 * 409 is "the money is not in a state that allows this". Flattening both to a generic failure is
 * how a surface starts telling people something that is not true.
 */
function escrowErrorCode(status: number) {
  if (status === 403) return "FORBIDDEN" as const;
  if (status === 404) return "NOT_FOUND" as const;
  if (status === 409) return "CONFLICT" as const;
  return "BAD_REQUEST" as const;
}

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

  /**
   * Opens the escrow record for one task. Only the task travels: the funder, the amount and the
   * share of record are derived by the .NET API from the project and the accepted agreement.
   */
  create: protectedProcedure
    .input(z.object({ taskId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.create({
        taskId: input.taskId,
      });
      if (response.error || !response.data) {
        throw new TRPCError({
          code: escrowErrorCode(response.status),
          message: response.error ?? "Failed to open the escrow record",
        });
      }
      return parseEscrow(response.data);
    }),

  /**
   * The entry to FUNDED. The shared node decides: this only asks the API to confirm the
   * reservation behind the escrow is actually held, and nothing about the money is sent.
   */
  fund: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.taskEscrows.fund(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: escrowErrorCode(response.status),
          message: response.error ?? "Failed to confirm the funds are set aside",
        });
      }
      return parseEscrow(response.data);
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

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  adminProcedure,
  createTRPCRouter,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { getAdminApiClient } from "~/server/admin-api-client";
import {
  failProjectDtoSchema,
  gateTransitionResultDtoSchema,
  projectGateStatusDtoSchema,
} from "~/lib/contracts/tokenomics-contract";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const clearGateSchema = z.object({
  configId: z.string().min(1),
  verifiedByUserId: z.string().min(1),
});

const failProjectSchema = z
  .object({ configId: z.string().min(1) })
  .merge(failProjectDtoSchema);

function parseGateContract<T>(
  schema: z.ZodType<T>,
  data: unknown,
  contractName: string,
): T {
  const result = schema.safeParse(data);
  if (!result.success) {
    throw new TRPCError({
      code: "INTERNAL_SERVER_ERROR",
      message: `Backend returned an invalid ${contractName} contract`,
    });
  }

  return result.data;
}

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const projectGatesRouter = createTRPCRouter({
  // ---- Queries ----

  getStatus: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectGates.getStatus(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Gate status not found",
        });
      }

      return parseGateContract(
        projectGateStatusDtoSchema,
        response.data,
        "project gate status",
      );
    }),

  // ---- Mutations ----

  evaluateGate: adminProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectGates.evaluateGate(
        input.configId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to evaluate gate",
        });
      }

      return parseGateContract(
        gateTransitionResultDtoSchema,
        response.data,
        "gate transition",
      );
    }),

  clearGate: adminProcedure
    .input(clearGateSchema)
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectGates.clearGate(
        input.configId,
        input.verifiedByUserId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to clear gate",
        });
      }

      return parseGateContract(
        gateTransitionResultDtoSchema,
        response.data,
        "gate transition",
      );
    }),

  failProject: adminProcedure
    .input(failProjectSchema)
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectGates.failProject(
        input.configId,
        { reason: input.reason },
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to mark project as failed",
        });
      }

      return parseGateContract(
        gateTransitionResultDtoSchema,
        response.data,
        "gate transition",
      );
    }),
});

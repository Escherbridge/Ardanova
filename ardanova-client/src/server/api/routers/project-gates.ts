import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const ProjectGateStatusSchema = z.enum(["FUNDING", "ACTIVE", "SUCCEEDED", "FAILED"]);

const clearGateSchema = z.object({
  configId: z.string().min(1),
  verifiedByUserId: z.string().min(1),
});

const failProjectSchema = z.object({
  configId: z.string().min(1),
  reason: z.string().min(1),
});

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

      return response.data;
    }),

  // ---- Mutations ----

  evaluateGate: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectGates.evaluateGate(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to evaluate gate",
        });
      }

      return response.data;
    }),

  clearGate: protectedProcedure
    .input(clearGateSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.projectGates.clearGate(
        input.configId,
        input.verifiedByUserId
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to clear gate",
        });
      }

      return response.data;
    }),

  failProject: protectedProcedure
    .input(failProjectSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.projectGates.failProject(input.configId, {
        reason: input.reason,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to mark project as failed",
        });
      }

      return response.data;
    }),
});

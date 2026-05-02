import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, adminProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const ProjectTokenStatusSchema = z.enum(["PENDING", "ACTIVE", "FROZEN", "DISSOLVED"]);
const ProjectGateStatusSchema = z.enum(["FUNDING", "ACTIVE", "SUCCEEDED", "FAILED"]);
const TokenHolderClassSchema = z.enum(["CONTRIBUTOR", "INVESTOR", "FOUNDER"]);
const AllocationStatusSchema = z.enum(["RESERVED", "DISTRIBUTED", "REVOKED", "BURNED"]);

const createProjectTokenConfigSchema = z.object({
  projectId: z.string().min(1),
  totalSupply: z.number().int().positive(),
  fundingGoal: z.number().positive(),
  unitName: z.string().min(1).max(8),
  assetName: z.string().optional(),
  successCriteria: z.string().optional(),
});

const createTokenAllocationSchema = z.object({
  taskId: z.string().min(1),
  equityPercentage: z.number().positive(),
});

const createInvestorAllocationSchema = z.object({
  userId: z.string().min(1),
  usdAmount: z.number().positive(),
});

const createFounderAllocationSchema = z.object({
  userId: z.string().min(1),
  equityPercentage: z.number().positive(),
});

const failProjectSchema = z.object({
  reason: z.string().min(1),
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const projectTokensRouter = createTRPCRouter({
  // ---- Config CRUD ----

  createConfig: protectedProcedure
    .input(createProjectTokenConfigSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.createConfig({
        projectId: input.projectId,
        totalSupply: input.totalSupply,
        fundingGoal: input.fundingGoal,
        unitName: input.unitName,
        assetName: input.assetName,
        successCriteria: input.successCriteria,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to create project token config",
        });
      }

      return response.data;
    }),

  getConfig: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getConfig(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Project token config not found",
        });
      }

      return response.data;
    }),

  getConfigByProject: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getConfigByProject(input.projectId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Project token config not found for project",
        });
      }

      return response.data;
    }),

  getSupply: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getSupply(input.id);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Project token config not found",
        });
      }

      return response.data;
    }),

  // ---- Allocations ----

  allocateToTask: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }).merge(createTokenAllocationSchema))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.allocateToTask(input.configId, {
        taskId: input.taskId,
        equityPercentage: input.equityPercentage,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to allocate tokens to task",
        });
      }

      return response.data;
    }),

  allocateToInvestor: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }).merge(createInvestorAllocationSchema))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.allocateToInvestor(input.configId, {
        userId: input.userId,
        usdAmount: input.usdAmount,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to allocate tokens to investor",
        });
      }

      return response.data;
    }),

  allocateToFounder: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }).merge(createFounderAllocationSchema))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.allocateToFounder(input.configId, {
        userId: input.userId,
        equityPercentage: input.equityPercentage,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to allocate tokens to founder",
        });
      }

      return response.data;
    }),

  distribute: protectedProcedure
    .input(z.object({
      allocationId: z.string().min(1),
      recipientUserId: z.string().min(1),
    }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.distribute(
        input.allocationId,
        input.recipientUserId
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to distribute tokens",
        });
      }

      return response.data;
    }),

  revoke: protectedProcedure
    .input(z.object({ allocationId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.revoke(input.allocationId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to revoke allocation",
        });
      }

      return response.data;
    }),

  getAllocations: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getAllocations(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get allocations",
        });
      }

      return response.data;
    }),

  getAllocationsByTask: protectedProcedure
    .input(z.object({ taskId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getAllocationsByTask(input.taskId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get task allocations",
        });
      }

      return response.data;
    }),

  getInvestors: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getInvestors(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get project investors",
        });
      }

      return response.data;
    }),

  // ---- Gate Management ----

  getGateStatus: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getGateStatus(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Gate status not found",
        });
      }

      return response.data;
    }),

  evaluateGate: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.evaluateGate(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to evaluate gate",
        });
      }

      return response.data;
    }),

  clearGate: adminProcedure
    .input(z.object({
      configId: z.string().min(1),
      verifiedByUserId: z.string().min(1),
    }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.clearGate(
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
    .input(z.object({ configId: z.string().min(1) }).merge(failProjectSchema))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.failProject(input.configId, {
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

  // ---- Failure Handling ----

  burnFounder: adminProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.burnFounder(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to burn founder tokens",
        });
      }

      return response.data;
    }),

  trustProtection: adminProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.trustProtection(input.configId);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to process trust protection",
        });
      }

      return response.data;
    }),
});

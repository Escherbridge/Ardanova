import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  adminProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { getAdminApiClient } from "~/server/admin-api-client";
import {
  createFounderAllocationDtoSchema,
  createInvestorAllocationDtoSchema,
  createProjectTokenConfigDtoSchema,
  createTokenAllocationDtoSchema,
  failProjectDtoSchema,
  gateTransitionResultDtoSchema,
  projectGateStatusDtoSchema,
  projectInvestmentDtoSchema,
  projectTokenConfigDtoSchema,
  projectTokenMetadataBatchDtoSchema,
  tokenAllocationDtoSchema,
} from "~/lib/contracts/tokenomics-contract";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const tokenAllocationListSchema = z.array(tokenAllocationDtoSchema);
const projectInvestmentListSchema = z.array(projectInvestmentDtoSchema);

function parseBackendContract<T>(
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
// Router - read operations use the service client; privileged mutations use the admin client.
// ---------------------------------------------------------------------------

export const projectTokensRouter = createTRPCRouter({
  // ---- Config CRUD ----

  createConfig: adminProcedure
    .input(createProjectTokenConfigDtoSchema)
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.createConfig({
        projectId: input.projectId,
        totalSupply: input.totalSupply,
        fundingGoal: input.fundingGoal,
        unitName: input.unitName,
        assetScale: input.assetScale,
        assetName: input.assetName,
        reservedPercentage: input.reservedPercentage,
        successCriteria: input.successCriteria,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to create project token config",
        });
      }

      return parseBackendContract(
        projectTokenConfigDtoSchema,
        response.data,
        "project-token config",
      );
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

      return parseBackendContract(
        projectTokenConfigDtoSchema,
        response.data,
        "project-token config",
      );
    }),

  getMetadata: protectedProcedure
    .input(
      z.object({
        ids: z.array(z.string().trim().min(1).max(200)).min(1).max(500),
      }),
    )
    .query(async ({ input }) => {
      const ids = [...new Set(input.ids.map((id) => id.trim()))];
      const response = await apiClient.projectTokens.getMetadata(ids);

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_GATEWAY",
          message: response.error ?? "Failed to load project-token metadata",
        });
      }

      return parseBackendContract(
        projectTokenMetadataBatchDtoSchema,
        response.data,
        "project-token metadata batch",
      );
    }),

  getConfigByProject: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getConfigByProject(
        input.projectId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message:
            response.error ?? "Project token config not found for project",
        });
      }

      return parseBackendContract(
        projectTokenConfigDtoSchema,
        response.data,
        "project-token config",
      );
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

      return parseBackendContract(
        projectTokenConfigDtoSchema,
        response.data,
        "project-token supply",
      );
    }),

  // ---- Allocations ----

  allocateToPbi: adminProcedure
    .input(
      z
        .object({ configId: z.string().min(1) })
        .merge(createTokenAllocationDtoSchema),
    )
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.allocateToPbi(
        input.configId,
        {
          pbiId: input.pbiId,
          equityPercentage: input.equityPercentage,
        },
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to allocate tokens to PBI",
        });
      }

      return parseBackendContract(
        tokenAllocationDtoSchema,
        response.data,
        "PBI token allocation",
      );
    }),

  allocateToInvestor: adminProcedure
    .input(
      z
        .object({ configId: z.string().min(1) })
        .merge(createInvestorAllocationDtoSchema),
    )
    .mutation(async ({ input }) => {
      const response =
        await getAdminApiClient().projectTokens.allocateToInvestor(
          input.configId,
          {
            userId: input.userId,
            usdAmount: input.usdAmount,
            tokenAmount: input.tokenAmount,
          },
        );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to allocate tokens to investor",
        });
      }

      return parseBackendContract(
        tokenAllocationDtoSchema,
        response.data,
        "investor token allocation",
      );
    }),

  allocateToFounder: adminProcedure
    .input(
      z
        .object({ configId: z.string().min(1) })
        .merge(createFounderAllocationDtoSchema),
    )
    .mutation(async ({ input }) => {
      const response =
        await getAdminApiClient().projectTokens.allocateToFounder(
          input.configId,
          {
            userId: input.userId,
            equityPercentage: input.equityPercentage,
          },
        );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to allocate tokens to founder",
        });
      }

      return parseBackendContract(
        tokenAllocationDtoSchema,
        response.data,
        "founder token allocation",
      );
    }),

  distribute: adminProcedure
    .input(
      z.object({
        allocationId: z.string().min(1),
        recipientUserId: z.string().min(1),
      }),
    )
    .mutation(() => {
      throw new TRPCError({
        code: "NOT_IMPLEMENTED",
        message:
          "Token distribution is paused until the backend provides an atomic allocation transition with durable idempotency.",
      });
    }),

  revoke: adminProcedure
    .input(z.object({ allocationId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.revoke(
        input.allocationId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to revoke allocation",
        });
      }

      return parseBackendContract(
        tokenAllocationDtoSchema,
        response.data,
        "revoked token allocation",
      );
    }),

  getAllocations: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getAllocations(
        input.configId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get allocations",
        });
      }

      return parseBackendContract(
        tokenAllocationListSchema,
        response.data,
        "token allocation list",
      );
    }),

  getAllocationsByPbi: protectedProcedure
    .input(z.object({ pbiId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getAllocationsByPbi(
        input.pbiId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get PBI allocations",
        });
      }

      return parseBackendContract(
        tokenAllocationListSchema,
        response.data,
        "PBI token allocation list",
      );
    }),

  getInvestors: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getInvestors(
        input.configId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to get project investors",
        });
      }

      return parseBackendContract(
        projectInvestmentListSchema,
        response.data,
        "project investment list",
      );
    }),

  // ---- Gate Management ----

  getGateStatus: protectedProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.projectTokens.getGateStatus(
        input.configId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Gate status not found",
        });
      }

      return parseBackendContract(
        projectGateStatusDtoSchema,
        response.data,
        "project gate status",
      );
    }),

  evaluateGate: adminProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.evaluateGate(
        input.configId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to evaluate gate",
        });
      }

      return parseBackendContract(
        gateTransitionResultDtoSchema,
        response.data,
        "gate transition",
      );
    }),

  clearGate: adminProcedure
    .input(
      z.object({
        configId: z.string().min(1),
        verifiedByUserId: z.string().min(1),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.clearGate(
        input.configId,
        input.verifiedByUserId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to clear gate",
        });
      }

      return parseBackendContract(
        gateTransitionResultDtoSchema,
        response.data,
        "gate transition",
      );
    }),

  failProject: adminProcedure
    .input(
      z.object({ configId: z.string().min(1) }).merge(failProjectDtoSchema),
    )
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.failProject(
        input.configId,
        {
          reason: input.reason,
        },
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to mark project as failed",
        });
      }

      return parseBackendContract(
        gateTransitionResultDtoSchema,
        response.data,
        "gate transition",
      );
    }),

  // ---- Failure Handling ----

  burnFounder: adminProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.burnFounder(
        input.configId,
      );

      if (response.error || response.data === undefined) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to burn founder tokens",
        });
      }

      return parseBackendContract(
        z.boolean(),
        response.data,
        "founder token burn result",
      );
    }),

  trustProtection: adminProcedure
    .input(z.object({ configId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await getAdminApiClient().projectTokens.trustProtection(
        input.configId,
      );

      if (response.error || response.data === undefined) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to process trust protection",
        });
      }

      return parseBackendContract(
        z.boolean(),
        response.data,
        "trust-protection processing result",
      );
    }),
});

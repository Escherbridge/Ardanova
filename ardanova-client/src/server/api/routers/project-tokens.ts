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

// ---------------------------------------------------------------------------
// Funding setup (self-serve project stake, W5.2)
//
// These two calls go through the base client rather than
// `apiClient.projectTokens`, whose endpoint module belongs to another item this
// wave. The paths are the same ones the .NET ProjectTokensController exposes.
// ---------------------------------------------------------------------------

const FUNDING_PROVISION_PATH = "/api/ProjectTokens/funding/provision";

/** Funding setup states, in the order a project reaches them. */
export const PROJECT_FUNDING_STATES = [
  "NotDeclared",
  "AwaitingUnits",
  "AwaitingTreasury",
  "AwaitingAllowances",
  "Ready",
  "Suspended",
] as const;

/**
 * A project's funding setup as a person reads it. Mirrors ProjectTokenFundingDto,
 * which deliberately carries no address, asset id or chain name.
 */
const projectTokenFundingSchema = z.object({
  projectId: z.string(),
  configId: z.string().nullish(),
  state: z.enum(PROJECT_FUNDING_STATES),
  isFundable: z.boolean(),
  ownerCanCompleteSetup: z.boolean(),
  setupMessage: z.string().nullish(),
  assetName: z.string().nullish(),
  unitName: z.string().nullish(),
  totalSupply: z.number().int(),
  contributorPartitionSupply: z.number().int(),
  investorPartitionSupply: z.number().int(),
  contributorUnitsRemaining: z.number().int(),
  investorUnitsRemaining: z.number().int(),
  fundingGoal: z.number(),
  fundingRaised: z.number(),
  disclosure: z.string().nullish(),
});

export type ProjectTokenFunding = z.infer<typeof projectTokenFundingSchema>;

/**
 * What an owner declares at publish. The bounds match
 * ProjectTokenService.ValidateDeclaration; the server checks them again, this copy
 * only spares the owner a round trip.
 */
export const provisionProjectFundingSchema = z.object({
  projectId: z.string().min(1),
  assetName: z.string().trim().min(3).max(32),
  unitName: z
    .string()
    .trim()
    .min(2)
    .max(8)
    .regex(/^[A-Za-z0-9]+$/u, "Use letters and digits only"),
  totalSupply: z.number().int().positive(),
  contributorSupply: z.number().int().nonnegative(),
  investorSupply: z.number().int().nonnegative(),
  fundingGoal: z.number().positive(),
  disclosure: z.string().trim().min(20).max(2000),
  reservedPercentage: z.number().min(0).max(100).default(0),
  successCriteria: z.string().trim().max(2000).optional(),
});

/** Keeps the backend's own refusal ("finish verifying your identity") in front of the owner. */
function toFundingError(status: number | undefined, message: string) {
  const code =
    status === 403
      ? "FORBIDDEN"
      : status === 404
        ? "NOT_FOUND"
        : status === 409
          ? "CONFLICT"
          : status === 400
            ? "BAD_REQUEST"
            : "INTERNAL_SERVER_ERROR";
  return new TRPCError({ code, message });
}

function readableFundingError(
  error: string | undefined,
  fallback: string,
): string {
  return !error || /^HTTP \d{3}$/u.test(error.trim()) ? fallback : error;
}

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

  // ---- Funding setup ----

  /**
   * A project's funding setup. Owner-only actions come back decided by the server
   * from the caller's verified identity, never from anything this client sends.
   */
  getFunding: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.get<unknown>(
        `/api/ProjectTokens/funding/by-project/${encodeURIComponent(input.projectId)}`,
      );

      if (response.error || !response.data) {
        throw toFundingError(
          response.status,
          readableFundingError(
            response.error,
            "This project's funding setup could not be read right now.",
          ),
        );
      }

      return parseBackendContract(
        projectTokenFundingSchema,
        response.data,
        "project funding setup",
      );
    }),

  /**
   * Runs the publish-time funding job. Safe to call again: the server resumes a
   * half-finished setup and never creates a project's stake twice.
   */
  provisionFunding: protectedProcedure
    .input(provisionProjectFundingSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.post<unknown>(
        FUNDING_PROVISION_PATH,
        input,
      );

      if (response.error || !response.data) {
        throw toFundingError(
          response.status,
          readableFundingError(
            response.error,
            "Funding setup could not be completed for this project.",
          ),
        );
      }

      return parseBackendContract(
        projectTokenFundingSchema,
        response.data,
        "project funding setup",
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

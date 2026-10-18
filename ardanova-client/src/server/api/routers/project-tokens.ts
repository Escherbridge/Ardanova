import { z } from "zod";
import { apiPath, segment } from "~/lib/api/path";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  adminProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { projectMoneyViewSchema } from "~/lib/contracts/project-money-contract";
import {
  PROJECT_FUNDING_GOAL_MAX_DOLLARS,
  PROJECT_FUNDING_GOAL_MIN_DOLLARS,
  PROJECT_FUNDING_UNIT,
} from "~/lib/contracts/project-create-contract";
import { getAdminApiClient } from "~/server/admin-api-client";
import {
  createFounderAllocationDtoSchema,
  createInvestorAllocationDtoSchema,
  createProjectTokenConfigDtoSchema,
  createTokenAllocationDtoSchema,
  failProjectDtoSchema,
  gateTransitionResultDtoSchema,
  projectGateStatusDtoSchema,
  projectInvestorsResponseSchema,
  projectTokenConfigDtoSchema,
  projectTokenMetadataBatchDtoSchema,
  tokenAllocationDtoSchema,
  tokenAllocationViewSchema,
} from "~/lib/contracts/tokenomics-contract";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

/**
 * The allocation routes branch their PAYLOAD on project membership (the GetInvestors
 * precedent), so this is a union and not an array: a non-member is answered with an
 * aggregate object, never with an empty list that would read as "nothing is set aside".
 */
const tokenAllocationListSchema = tokenAllocationViewSchema;

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

// ---------------------------------------------------------------------------
// Project money (per-project "Money" view, W10.1)
//
// Mirrors ProjectMoneyView from the .NET ProjectLedgerController. The balance and
// the entries are two different kinds of fact and are kept apart deliberately: the
// balance is the project's own funds account as the node reported it, and the
// entries only EXPLAIN it. Nothing in this file adds entries up.
// ---------------------------------------------------------------------------

const PROJECT_MONEY_PATH_PREFIX = "/api/ProjectLedger";

// The money page contract lives in ~/lib/contracts/project-money-contract.ts so it can be
// tested directly; a field missing from it is silently stripped off the server response.
export {
  PROJECT_MONEY_DIRECTIONS,
  PROJECT_MONEY_DENOMINATIONS,
  type ProjectMoneyEntry,
  type ProjectMoneyView,
} from "~/lib/contracts/project-money-contract";

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
  /**
   * Bounded and cent-precise, not merely positive. `z.number().positive()` let a $0.001 or a
   * $1,000,000,000,000 ask through this boundary while the create wizard refused both in the
   * browser - a bound one client applies is an affordance, not a control. The server refuses
   * these too (`ProjectFundingLedger.DescribeUnusableGoalAmount`); this copy spares a round trip.
   */
  fundingGoal: z
    .number()
    .positive()
    .min(PROJECT_FUNDING_GOAL_MIN_DOLLARS)
    .max(PROJECT_FUNDING_GOAL_MAX_DOLLARS)
    .refine(
      (goal) =>
        Number.isInteger(Math.round(goal * 100)) &&
        Math.abs(goal * 100 - Math.round(goal * 100)) < 1e-9,
      { message: "A funding goal cannot be finer than a cent." },
    ),
  /**
   * Declared, not assumed. `z.literal` means this client cannot send any other unit even by
   * mistake, and the server refuses one by name - a goal stored in a different unit from the
   * raise it is compared against is what mispriced every project's opening offer.
   */
  fundingGoalUnit: z.literal(PROJECT_FUNDING_UNIT),
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
// Approving completed work
//
// The owner-facing action needs an award id, and nothing a task carries names one: the
// allocation read-model does not publish the agreement it belongs to. So the id is resolved
// here, from facts the caller is already allowed to read, and only when the resolution is
// UNAMBIGUOUS. A guess would approve the wrong award, so an ambiguous read refuses and says so.
//
// None of this decides who may approve. That control is
// ProjectTokenService.ApproveWorkCompletionAsync, which reads the caller's verified identity,
// refuses anyone but the project's creator, and pays the person the accepted agreement names.
// ---------------------------------------------------------------------------

/** What one task's award looks like to the person deciding whether to approve it. */
export interface TaskWorkApproval {
  taskId: string;
  /** True only when the signed-in caller created the project this task belongs to. */
  viewerIsProjectOwner: boolean;
  /** The whole award the agreement records, in units. */
  awardUnits: number | null;
  /** The share held for the worker: less than the award when a guild takes part of it. */
  workerShareUnits: number | null;
  /** The share held for the worker's guild, when the accepted bid declared one. */
  guildShareUnits: number | null;
  /** The worker's own award row, resolved here and never sent by the browser. */
  allocationId: string | null;
  /** True once the award has been moved into the worker's own window. */
  isApproved: boolean;
  /** Why approval is not offered, said to the owner. Null when it is. */
  blockedReason: string | null;
}

const APPROVAL_UNREADABLE =
  "This task's award could not be read right now, so approving it is not offered.";
const APPROVAL_NOT_ASSIGNED =
  "This task has nobody assigned to it, so there is no completed work to approve.";
const APPROVAL_NO_AGREEMENT =
  "This task has no accepted assignment, so there is nothing to approve.";
const APPROVAL_NOT_SET_ASIDE =
  "No award of this size is set aside for this worker on this project, so there is nothing to hand over yet.";
const APPROVAL_AMBIGUOUS =
  "More than one award of this size is set aside for this worker on this project, so this screen cannot tell which one belongs to this task. Nothing was approved.";
const APPROVAL_NOT_OWNER =
  "Only the person who created this project can approve completed work on it.";

/**
 * The guild's declared share of the bid this task came from, in units; null when it cannot be
 * read. Null blocks approval rather than standing in for zero: a wrong share mis-sizes the
 * worker's own and could match somebody else's award.
 */
async function readGuildShareUnits(
  opportunityId: string | null | undefined,
): Promise<number | null> {
  if (!opportunityId) return 0;

  const response =
    await apiClient.opportunityBids.getByOpportunityId(opportunityId);
  if (response.error || !response.data) return null;

  const accepted = response.data.filter(
    (bid) => bid.status === "ACCEPTED" || bid.status === "COMPLETED",
  );
  if (accepted.length !== 1) return null;

  const declaredSplit = accepted[0]?.guildAwardSplit;
  return typeof declaredSplit === "number" ? declaredSplit : 0;
}

/** One task's award as its project owner may act on it. Every hop carries the caller's identity. */
async function resolveTaskWorkApproval(
  taskId: string,
  viewerUserId: string,
): Promise<TaskWorkApproval> {
  const blank: TaskWorkApproval = {
    taskId,
    viewerIsProjectOwner: false,
    awardUnits: null,
    workerShareUnits: null,
    guildShareUnits: null,
    allocationId: null,
    isApproved: false,
    blockedReason: null,
  };

  const taskResponse = await apiClient.tasks.getById(taskId);
  const task = taskResponse.data;
  if (taskResponse.error || !task) {
    return { ...blank, blockedReason: APPROVAL_UNREADABLE };
  }

  const projectResponse = await apiClient.projects.getById(task.projectId);
  const project = projectResponse.data;
  if (projectResponse.error || !project) {
    return { ...blank, blockedReason: APPROVAL_UNREADABLE };
  }

  // The two people this answers: the owner, who may approve, and the assigned worker, who is
  // owed a truthful account of their own share. Anyone else is told nothing.
  const viewerIsProjectOwner = project.createdById === viewerUserId;
  const workerUserId = task.assignedToId;
  const viewerIsAssignedWorker =
    Boolean(workerUserId) && workerUserId === viewerUserId;
  if (!viewerIsProjectOwner && !viewerIsAssignedWorker) return blank;

  const viewer = { ...blank, viewerIsProjectOwner };

  const commerceResponse = await apiClient.tasks.getCommerce(taskId);
  const commerce = commerceResponse.data;
  if (commerceResponse.error || !commerce) {
    return { ...viewer, blockedReason: APPROVAL_NO_AGREEMENT };
  }

  if (!workerUserId) return { ...viewer, blockedReason: APPROVAL_NOT_ASSIGNED };

  const guildShareUnits = await readGuildShareUnits(task.opportunityId);
  if (guildShareUnits === null) {
    return {
      ...viewer,
      awardUnits: commerce.awardAmount,
      blockedReason: APPROVAL_UNREADABLE,
    };
  }

  const workerShareUnits = commerce.awardAmount - guildShareUnits;
  const shares = {
    ...viewer,
    awardUnits: commerce.awardAmount,
    workerShareUnits,
    guildShareUnits: guildShareUnits > 0 ? guildShareUnits : null,
  };

  // The worker is told their share and where it stands; the award id and the control are the
  // owner's alone, and the window date the agreement already reports is the same fact.
  if (!viewerIsProjectOwner) {
    return { ...shares, isApproved: Boolean(commerce.workerHoldReleasesAt) };
  }

  const configResponse = await apiClient.projectTokens.getConfigByProject(
    task.projectId,
  );
  const config = configResponse.data;
  if (configResponse.error || !config) {
    return { ...shares, blockedReason: APPROVAL_UNREADABLE };
  }

  const allocationsResponse = await apiClient.projectTokens.getAllocations(
    config.id,
  );
  if (allocationsResponse.error || !allocationsResponse.data) {
    return { ...shares, blockedReason: APPROVAL_UNREADABLE };
  }

  // The route answers a non-member with an aggregate, so the shape is discriminated rather
  // than assumed: a summary carries no award to approve.
  const allocations = parseBackendContract(
    tokenAllocationListSchema,
    allocationsResponse.data,
    "token allocation list",
  );
  if (!Array.isArray(allocations)) {
    return { ...shares, blockedReason: APPROVAL_UNREADABLE };
  }

  const candidates = allocations.filter(
    (allocation) =>
      allocation.holderClass === "CONTRIBUTOR" &&
      allocation.recipientUserId === workerUserId &&
      allocation.tokenAmount === workerShareUnits &&
      (allocation.status === "RESERVED" || allocation.status === "DISTRIBUTED"),
  );

  if (candidates.length > 1) {
    return { ...shares, blockedReason: APPROVAL_AMBIGUOUS };
  }

  const [allocation] = candidates;
  if (!allocation) {
    return { ...shares, blockedReason: APPROVAL_NOT_SET_ASIDE };
  }

  return {
    ...shares,
    allocationId: allocation.id,
    isApproved: allocation.status === "DISTRIBUTED",
    blockedReason: null,
  };
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
        apiPath`/api/ProjectTokens/funding/by-project/${input.projectId}`,
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

  // ---- Money ----

  /**
   * One project's money: what it holds, and what happened to it. The server decides
   * from the caller's verified identity whether any counterparty may be named, so
   * nothing this client sends can widen what comes back.
   */
  getMoney: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.get<unknown>(
        `${PROJECT_MONEY_PATH_PREFIX}/${segment(input.projectId)}/money`,
      );

      if (response.error || !response.data) {
        throw toFundingError(
          response.status,
          readableFundingError(
            response.error,
            "This project's money could not be read right now.",
          ),
        );
      }

      return parseBackendContract(
        projectMoneyViewSchema,
        response.data,
        "project money view",
      );
    }),

  // ---- Allocations ----

  // Setting a project's own stake aside against a backlog item is the project owner's
  // decision, so the backend decides ownership from the caller's verified identity
  // (ActorAssertion) rather than from the service key. An admin-key call now 403s, which is
  // why this goes through `apiClient` — it carries the signed actor header — and not
  // `getAdminApiClient()`.
  allocateToPbi: protectedProcedure
    .input(
      z
        .object({ configId: z.string().min(1) })
        .merge(createTokenAllocationDtoSchema),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.allocateToPbi(
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

  // The owner-facing action is "approve completed work", not "distribute tokens": approving
  // moves the units already set aside for the worker into the worker's own window. The person
  // paid is the one the accepted agreement names, so no recipient is sent — a caller-supplied
  // recipient on an owned resource is the IDOR shape this codebase refuses everywhere else,
  // and the backend refuses a mismatch outright.
  approveWorkCompletion: protectedProcedure
    .input(z.object({ allocationId: z.string().min(1) }))
    .mutation(async ({ input }) => {
      const response = await apiClient.projectTokens.approveWorkCompletion(
        input.allocationId,
      );

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to approve the completed work",
        });
      }

      return parseBackendContract(
        tokenAllocationDtoSchema,
        response.data,
        "work-completion approval",
      );
    }),

  /**
   * Where one task's award stands for the person who might approve it. A non-owner is answered
   * with the same object carrying no award id, so the page can say what is happening without
   * being handed anything to act on.
   */
  getTaskWorkApproval: protectedProcedure
    .input(z.object({ taskId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      return resolveTaskWorkApproval(input.taskId, ctx.session.user.id);
    }),

  /**
   * The project owner approves one task's completed work. The award is resolved from the TASK
   * on this side of the wire: an allocation id supplied by the browser is the IDOR shape this
   * codebase refuses, and the backend refuses a caller who did not create the project anyway.
   */
  approveTaskWork: protectedProcedure
    .input(z.object({ taskId: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const approval = await resolveTaskWorkApproval(
        input.taskId,
        ctx.session.user.id,
      );

      if (!approval.viewerIsProjectOwner) {
        throw new TRPCError({ code: "FORBIDDEN", message: APPROVAL_NOT_OWNER });
      }

      if (approval.isApproved) return approval;

      if (!approval.allocationId) {
        throw new TRPCError({
          code: "CONFLICT",
          message: approval.blockedReason ?? APPROVAL_UNREADABLE,
        });
      }

      const response = await apiClient.projectTokens.approveWorkCompletion(
        approval.allocationId,
      );

      if (response.error || !response.data) {
        throw toFundingError(
          response.status,
          readableFundingError(
            response.error,
            "This work could not be approved right now.",
          ),
        );
      }

      // Parsed so a backend that answered with something else is not read as an approval.
      parseBackendContract(
        tokenAllocationDtoSchema,
        response.data,
        "work-completion approval",
      );

      return { ...approval, isApproved: true, blockedReason: null };
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

      // Members get itemised pledges, everyone else an aggregate summary. Validating the UNION
      // is what stops a summary being silently read as "no backers yet".
      return parseBackendContract(
        projectInvestorsResponseSchema,
        response.data,
        "project investors response",
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

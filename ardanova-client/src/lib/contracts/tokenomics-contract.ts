import {
  PROJECT_FUNDING_GOAL_MAX_DOLLARS,
  PROJECT_FUNDING_GOAL_MIN_DOLLARS,
} from "~/lib/contracts/project-create-contract";
import { z } from "zod";

export const projectTokenStatusSchema = z.enum([
  "PENDING",
  "ACTIVE",
  "FROZEN",
  "DISSOLVED",
]);

export const projectGateStatusSchema = z.enum([
  "FUNDING",
  "ACTIVE",
  "SUCCEEDED",
  "FAILED",
]);

export const tokenHolderClassSchema = z.enum([
  "CONTRIBUTOR",
  "INVESTOR",
  "FOUNDER",
]);

export const allocationStatusSchema = z.enum([
  "RESERVED",
  "DISTRIBUTED",
  "REVOKED",
  "BURNED",
]);

const dateTimeSchema = z.string().datetime({ offset: true });
const nonnegativeIntegerSchema = z.number().int().nonnegative();
const nonnegativeNumberSchema = z.number().finite().nonnegative();

export const projectTokenConfigDtoSchema = z
  .object({
    id: z.string().min(1),
    projectId: z.string().min(1),
    assetId: z.string().nullable(),
    assetName: z.string().min(1),
    unitName: z.string().min(1),
    assetScale: z.number().int().min(0).max(18).nullable(),
    totalSupply: nonnegativeIntegerSchema,
    allocatedSupply: nonnegativeIntegerSchema,
    distributedSupply: nonnegativeIntegerSchema,
    reservedSupply: nonnegativeIntegerSchema,
    mintTxHash: z.string().nullable(),
    status: projectTokenStatusSchema,
    fundingGoal: nonnegativeNumberSchema,
    fundingRaised: nonnegativeNumberSchema,
    gateStatus: projectGateStatusSchema,
    gate1ClearedAt: dateTimeSchema.nullable(),
    gate2ClearedAt: dateTimeSchema.nullable(),
    failedAt: dateTimeSchema.nullable(),
    contributorSupply: nonnegativeIntegerSchema,
    investorSupply: nonnegativeIntegerSchema,
    founderSupply: nonnegativeIntegerSchema,
    burnedSupply: nonnegativeIntegerSchema,
    successCriteria: z.string().nullable(),
    successVerifiedBy: z.string().nullable(),
    createdAt: dateTimeSchema,
    updatedAt: dateTimeSchema,
    availableSupply: z.number().int(),
  })
  .strict();

export const projectTokenMetadataDtoSchema = z
  .object({
    id: z.string().min(1),
    assetName: z.string().min(1),
    unitName: z.string().min(1),
  })
  .strict();

export const projectTokenMetadataBatchDtoSchema = z
  .object({
    items: z.array(projectTokenMetadataDtoSchema),
    missingIds: z.array(z.string().min(1)),
  })
  .strict();

export const createProjectTokenConfigDtoSchema = z
  .object({
    projectId: z.string().min(1),
    assetName: z.string().min(1),
    unitName: z.string().min(1),
    assetScale: z.number().int().min(0).max(18),
    totalSupply: z.number().int().positive(),
    // Bounded and cent-precise, like every other door that writes an ask. The server refuses
    // these independently (ProjectFundingLedger.DescribeUnusableGoalAmount).
    fundingGoal: z
      .number()
      .finite()
      .positive()
      .min(PROJECT_FUNDING_GOAL_MIN_DOLLARS)
      .max(PROJECT_FUNDING_GOAL_MAX_DOLLARS)
      .refine((goal) => Math.abs(goal * 100 - Math.round(goal * 100)) < 1e-9, {
        message: "A funding goal cannot be finer than a cent.",
      }),
    reservedPercentage: z.number().finite().min(0).max(100).optional(),
    successCriteria: z.string().nullable().optional(),
  })
  .strict();

export const tokenAllocationDtoSchema = z
  .object({
    id: z.string().min(1),
    projectTokenConfigId: z.string().min(1),
    pbiId: z.string().nullable(),
    recipientUserId: z.string().nullable(),
    equityPercentage: z.number().finite(),
    tokenAmount: nonnegativeIntegerSchema,
    status: allocationStatusSchema,
    holderClass: tokenHolderClassSchema,
    isLiquid: z.boolean(),
    distributedAt: dateTimeSchema.nullable(),
    distributionTxHash: z.string().nullable(),
    burnedAt: dateTimeSchema.nullable(),
    createdAt: dateTimeSchema,
    updatedAt: dateTimeSchema,
  })
  .strict();

/**
 * What a NON-MEMBER receives from the allocation routes: proof that work has been set aside,
 * with no per-worker award size, no recipient id and no settlement reference. The route
 * answers a member with an array and everyone else with this object, so a caller must
 * discriminate on the shape rather than read an empty array as "nothing is allocated".
 */
export const projectAllocationSummaryDtoSchema = z
  .object({
    allocationCount: nonnegativeIntegerSchema,
    totalUnitsAllocated: nonnegativeIntegerSchema,
    totalEquityPercentage: z.number().finite(),
    distributedCount: nonnegativeIntegerSchema,
  })
  .strict();

export type ProjectAllocationSummaryDto = z.infer<
  typeof projectAllocationSummaryDtoSchema
>;

/** Either the itemised allocations (a member) or the aggregate (everyone else). */
export const tokenAllocationViewSchema = z.union([
  z.array(tokenAllocationDtoSchema),
  projectAllocationSummaryDtoSchema,
]);

export type TokenAllocationView = z.infer<typeof tokenAllocationViewSchema>;

export const createTokenAllocationDtoSchema = z
  .object({
    pbiId: z.string().min(1).nullable().optional(),
    equityPercentage: z.number().finite().positive().max(100),
  })
  .strict();

export const createInvestorAllocationDtoSchema = z
  .object({
    userId: z.string().min(1),
    usdAmount: z.number().finite().positive(),
    tokenAmount: z.number().int().positive(),
  })
  .strict();

export const createFounderAllocationDtoSchema = z
  .object({
    userId: z.string().min(1),
    equityPercentage: z.number().finite().positive().max(100),
  })
  .strict();

export const projectGateStatusDtoSchema = z
  .object({
    projectTokenConfigId: z.string().min(1),
    gateStatus: projectGateStatusSchema,
    fundingGoal: nonnegativeNumberSchema,
    fundingRaised: nonnegativeNumberSchema,
    fundingProgress: nonnegativeNumberSchema,
    gate1ClearedAt: dateTimeSchema.nullable(),
    gate2ClearedAt: dateTimeSchema.nullable(),
    failedAt: dateTimeSchema.nullable(),
  })
  .strict();

export const gateTransitionResultDtoSchema = z
  .object({
    transitioned: z.boolean(),
    previousStatus: projectGateStatusSchema,
    newStatus: projectGateStatusSchema,
    tokensUnlocked: nonnegativeIntegerSchema,
    tokensBurned: nonnegativeIntegerSchema,
    trustProtectionPaid: nonnegativeNumberSchema,
  })
  .strict();

export const projectInvestmentDtoSchema = z
  .object({
    id: z.string().min(1),
    projectTokenConfigId: z.string().min(1),
    userId: z.string().min(1),
    usdAmount: nonnegativeNumberSchema,
    tokenAmount: nonnegativeIntegerSchema,
    investedAt: dateTimeSchema,
    protectionEligible: z.boolean(),
    protectionPaidOut: z.boolean(),
    protectionAmount: nonnegativeNumberSchema.nullable(),
    protectionPaidAt: dateTimeSchema.nullable(),
  })
  .strict();

/**
 * One pledge as a project MEMBER is allowed to read it. The backer's internal user id is not in
 * this shape at all, and `displayName` reads "Anonymous backer" unless that backer's own
 * per-pledge consent opted in - so a name can only appear because someone chose to show it.
 * Mirrors `ProjectInvestorLineDto`.
 */
export const projectInvestorLineDtoSchema = z
  .object({
    id: z.string().min(1),
    displayName: z.string().min(1),
    amount: nonnegativeNumberSchema,
    unitsPledged: nonnegativeIntegerSchema,
    investedAt: dateTimeSchema,
    protectionEligible: z.boolean(),
    protectionPaidOut: z.boolean(),
    protectionAmount: nonnegativeNumberSchema.nullable(),
  })
  .strict();

/**
 * What a NON-member sees: proof the project has backers, with no per-pledge amount and no name a
 * stranger could match to a real person. Mirrors `ProjectInvestorSummaryDto`.
 */
export const projectInvestorSummaryDtoSchema = z
  .object({
    backerCount: nonnegativeIntegerSchema,
    totalAmount: nonnegativeNumberSchema,
  })
  .strict();

/**
 * The GetInvestors response: itemised for members, aggregate-only for everyone else. Validated as
 * a union at the boundary so a summary can never be read as an empty list of pledges (which would
 * silently render "no backers yet" for a project that has them), and so an itemised list served to
 * a non-member would fail parsing here rather than leaking per-pledge rows into the UI.
 */
export const projectInvestorsResponseSchema = z.union([
  z.array(projectInvestorLineDtoSchema),
  projectInvestorSummaryDtoSchema,
]);

export const failProjectDtoSchema = z
  .object({ reason: z.string().trim().min(1) })
  .strict();

export type ProjectTokenStatus = z.infer<typeof projectTokenStatusSchema>;
export type ProjectGateStatus = z.infer<typeof projectGateStatusSchema>;
export type TokenHolderClass = z.infer<typeof tokenHolderClassSchema>;
export type AllocationStatus = z.infer<typeof allocationStatusSchema>;
export type ProjectTokenConfigDto = z.infer<typeof projectTokenConfigDtoSchema>;
export type ProjectTokenMetadataDto = z.infer<
  typeof projectTokenMetadataDtoSchema
>;
export type ProjectTokenMetadataBatchDto = z.infer<
  typeof projectTokenMetadataBatchDtoSchema
>;
export type CreateProjectTokenConfigDto = z.infer<
  typeof createProjectTokenConfigDtoSchema
>;
export type TokenAllocationDto = z.infer<typeof tokenAllocationDtoSchema>;
export type CreateTokenAllocationDto = z.infer<
  typeof createTokenAllocationDtoSchema
>;
export type CreateInvestorAllocationDto = z.infer<
  typeof createInvestorAllocationDtoSchema
>;
export type CreateFounderAllocationDto = z.infer<
  typeof createFounderAllocationDtoSchema
>;
export type ProjectGateStatusDto = z.infer<typeof projectGateStatusDtoSchema>;
export type GateTransitionResultDto = z.infer<
  typeof gateTransitionResultDtoSchema
>;
export type ProjectInvestmentDto = z.infer<typeof projectInvestmentDtoSchema>;
export type FailProjectDto = z.infer<typeof failProjectDtoSchema>;
export type ProjectInvestorLineDto = z.infer<typeof projectInvestorLineDtoSchema>;
export type ProjectInvestorSummaryDto = z.infer<
  typeof projectInvestorSummaryDtoSchema
>;
export type ProjectInvestorsResponseDto = z.infer<
  typeof projectInvestorsResponseSchema
>;

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
    fundingGoal: z.number().finite().positive(),
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
    stripePaymentIntentId: z.string().nullable(),
    investedAt: dateTimeSchema,
    protectionEligible: z.boolean(),
    protectionPaidOut: z.boolean(),
    protectionAmount: nonnegativeNumberSchema.nullable(),
    protectionPaidAt: dateTimeSchema.nullable(),
  })
  .strict();

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

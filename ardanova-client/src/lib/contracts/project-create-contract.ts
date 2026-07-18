import { z } from "zod";

import {
  projectDurationSchema,
  projectRoleSchema,
  projectTypeSchema,
} from "./project-contract";

export const PROJECT_CREATE_DRAFT_KEY = "ardanova:project-create:draft:v1";
export const PROJECT_CREATE_DRAFT_VERSION = 1 as const;
const PROJECT_CREATE_DRAFT_MAX_BYTES = 2_000_000;

export const PROJECT_CREATE_COMPENSATION_MODELS = [
  "FIXED_SHARES",
  "HOURLY_SHARES",
  "EQUITY_PERCENT",
  "HYBRID",
  "BOUNTY",
  "MILESTONE",
] as const;

const projectCreateCompensationModelSchema = z.enum(
  PROJECT_CREATE_COMPENSATION_MODELS,
);
const draftTextSchema = z.string().max(20_000);
const draftIdentifierSchema = z.string().min(1).max(256);

const projectCreateResourceSchema = z
  .object({
    id: draftIdentifierSchema,
    name: draftTextSchema,
    description: draftTextSchema,
    quantity: z.number().int().min(1).max(1_000_000),
    estimatedCost: z.string().max(64),
    recurringCost: z.string().max(64),
    recurringIntervalDays: z.number().int().min(1).max(365).nullable(),
    isRequired: z.boolean(),
  })
  .strict();

const projectCreateRoleSchema = z
  .object({
    id: draftIdentifierSchema,
    title: draftTextSchema,
    description: draftTextSchema,
    compensationModel: z.union([
      projectCreateCompensationModelSchema,
      z.literal(""),
    ]),
    shareAmount: z.string().max(64),
    equityPercent: z.string().max(64),
    isOpenForApplications: z.boolean(),
    projectRole: z.union([projectRoleSchema, z.literal("")]),
  })
  .strict();

const projectCreateMilestoneSchema = z
  .object({
    id: draftIdentifierSchema,
    title: draftTextSchema,
    description: draftTextSchema,
    targetDate: z.string().max(64),
  })
  .strict();

const projectCreateRecoverySchema = z
  .object({
    projectId: draftIdentifierSchema,
    projectSlug: draftIdentifierSchema,
    resourceIds: z.array(draftIdentifierSchema).max(250),
    milestoneIds: z.array(draftIdentifierSchema).max(250),
    roleIds: z.array(draftIdentifierSchema).max(250),
    founderMembershipPending: z.boolean(),
    warnings: z.array(z.string().min(1).max(2_000)).max(751),
  })
  .strict();

export const projectCreateFormDataSchema = z
  .object({
    title: draftTextSchema,
    problemStatement: draftTextSchema,
    solution: draftTextSchema,
    categories: z.array(z.string().max(256)).max(50),
    otherCategory: z.string().max(50),
    projectType: z.union([projectTypeSchema, z.literal("")]),
    duration: z.union([projectDurationSchema, z.literal("")]),
    targetAudience: draftTextSchema,
    expectedImpact: draftTextSchema,
    timeline: draftTextSchema,
    tags: z.array(z.string().max(256)).max(100),
    resources: z.array(projectCreateResourceSchema).max(250),
    roles: z.array(projectCreateRoleSchema).max(250),
    milestones: z.array(projectCreateMilestoneSchema).max(250),
  })
  .strict();

export const projectCreateDraftSchema = z
  .object({
    version: z.literal(PROJECT_CREATE_DRAFT_VERSION),
    currentStep: z.number().int().min(0).max(4),
    formData: projectCreateFormDataSchema,
    recovery: projectCreateRecoverySchema.optional(),
  })
  .strict();

export type ProjectCreateCompensationModel = z.infer<
  typeof projectCreateCompensationModelSchema
>;
export type ProjectCreateFormData = z.infer<typeof projectCreateFormDataSchema>;
export type ProjectCreateResource = z.infer<typeof projectCreateResourceSchema>;
export type ProjectCreateRole = z.infer<typeof projectCreateRoleSchema>;
export type ProjectCreateMilestone = z.infer<
  typeof projectCreateMilestoneSchema
>;
export type ProjectCreateRecovery = z.infer<typeof projectCreateRecoverySchema>;
export type ProjectCreateDraft = z.infer<typeof projectCreateDraftSchema>;

export interface ProjectRoleOpportunityInput {
  projectId: string;
  title: string;
  description: string;
  type: "PROJECT_ROLE";
  skills: string[];
  compensationModel: "fixed" | "hourly" | "negotiable";
  compensationAmount?: number;
  equityPercent?: number;
  isOpenForApplications: boolean;
  isRemote: true;
  projectRole?: Exclude<ProjectCreateRole["projectRole"], "">;
}

const opportunityCompensationByProjectModel: Record<
  ProjectCreateCompensationModel,
  "fixed" | "hourly" | "negotiable"
> = {
  FIXED_SHARES: "fixed",
  HOURLY_SHARES: "hourly",
  EQUITY_PERCENT: "negotiable",
  HYBRID: "negotiable",
  BOUNTY: "fixed",
  MILESTONE: "fixed",
};

export function createEmptyProjectFormData(): ProjectCreateFormData {
  return {
    title: "",
    problemStatement: "",
    solution: "",
    categories: [],
    otherCategory: "",
    projectType: "",
    duration: "",
    targetAudience: "",
    expectedImpact: "",
    timeline: "",
    tags: [],
    resources: [],
    roles: [],
    milestones: [],
  };
}

export function isProjectCreateCompensationModel(
  value: string,
): value is ProjectCreateCompensationModel {
  return projectCreateCompensationModelSchema.safeParse(value).success;
}

export function isProjectCreationCustodyReady(
  status:
    | {
        avatarId?: string | null;
        walletId?: string | null;
        walletAddress?: string | null;
        kycStatus: string;
        identityReady: boolean;
        kycReady: boolean;
        walletReady: boolean;
        ready: boolean;
      }
    | null
    | undefined,
): boolean {
  return Boolean(
    status?.ready &&
      status.identityReady &&
      status.avatarId?.trim() &&
      status.kycReady &&
      status.kycStatus === "Approved" &&
      status.walletReady &&
      status.walletId?.trim() &&
      status.walletAddress?.trim(),
  );
}

export function toOpportunityCompensation(
  model: ProjectCreateCompensationModel | "",
): "fixed" | "hourly" | "negotiable" {
  return model ? opportunityCompensationByProjectModel[model] : "negotiable";
}

export function toOptionalIsoDate(value: string): string | undefined {
  const normalized = value.trim();
  if (!normalized) return undefined;

  const date = new Date(normalized);
  if (Number.isNaN(date.getTime())) {
    throw new Error("Invalid milestone target date");
  }

  return date.toISOString();
}

function optionalPositiveNumber(
  value: string,
  label: string,
): number | undefined {
  const normalized = value.trim();
  if (!normalized) return undefined;

  const parsed = Number(normalized);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    throw new Error(`${label} must be a positive number`);
  }
  return parsed;
}

function optionalEquityPercent(value: string): number | undefined {
  const normalized = value.trim();
  if (!normalized) return undefined;

  const parsed = Number(normalized);
  if (!Number.isFinite(parsed) || parsed < 0 || parsed > 100) {
    throw new Error("Allocation percentage must be between 0 and 100");
  }
  return parsed;
}

export function toProjectRoleOpportunityInput(input: {
  projectId: string;
  projectTitle: string;
  tags: string[];
  role: ProjectCreateRole;
}): ProjectRoleOpportunityInput {
  const roleTitle = input.role.title.trim();
  const projectTitle = input.projectTitle.trim() || "this project";
  const enteredDescription = input.role.description.trim();
  const description =
    enteredDescription.length >= 20
      ? enteredDescription
      : [
          enteredDescription,
          `Help ${projectTitle} move forward in the ${roleTitle || "contributor"} role.`,
        ]
          .filter(Boolean)
          .join(" ");

  if (description.length < 20) {
    throw new Error("Role description must be at least 20 characters");
  }

  return {
    projectId: input.projectId,
    title: roleTitle,
    description,
    type: "PROJECT_ROLE",
    skills: input.tags.length > 0 ? input.tags : ["General"],
    compensationModel: toOpportunityCompensation(input.role.compensationModel),
    compensationAmount: optionalPositiveNumber(
      input.role.shareAmount,
      "Share amount",
    ),
    equityPercent: optionalEquityPercent(input.role.equityPercent),
    isOpenForApplications: input.role.isOpenForApplications,
    isRemote: true,
    projectRole: input.role.projectRole || undefined,
  };
}

export function parseProjectCreateDraft(
  serialized: string | null,
): ProjectCreateDraft | null {
  if (
    !serialized ||
    new TextEncoder().encode(serialized).byteLength >
      PROJECT_CREATE_DRAFT_MAX_BYTES
  ) {
    return null;
  }

  try {
    const result = projectCreateDraftSchema.safeParse(JSON.parse(serialized));
    return result.success ? result.data : null;
  } catch {
    return null;
  }
}

export function serializeProjectCreateDraft(
  draft: Omit<ProjectCreateDraft, "version">,
): string {
  const serialized = JSON.stringify(
    projectCreateDraftSchema.parse({
      version: PROJECT_CREATE_DRAFT_VERSION,
      ...draft,
    }),
  );
  if (
    new TextEncoder().encode(serialized).byteLength >
    PROJECT_CREATE_DRAFT_MAX_BYTES
  ) {
    throw new Error("Project draft exceeds the tab-storage limit");
  }
  return serialized;
}

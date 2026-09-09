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

/** The one-line ask: long enough to say something, short enough to read on a card. */
export const PROJECT_SUMMARY_MIN_LENGTH = 20;
export const PROJECT_SUMMARY_MAX_LENGTH = 200;

/** Upload limits mirrored by `attachment.getUploadUrls`, which is the enforcing side. */
export const PROJECT_MEDIA_MAX_FILES = 12;
export const PROJECT_MEDIA_MAX_FILE_BYTES = 25_000_000;

export const PROJECT_MEDIA_IMAGE_TYPES = [
  "image/png",
  "image/jpeg",
  "image/webp",
  "image/gif",
  "image/avif",
] as const;

export const PROJECT_MEDIA_VIDEO_TYPES = [
  "video/mp4",
  "video/webm",
  "video/quicktime",
] as const;

export const PROJECT_MEDIA_DOCUMENT_TYPES = [
  "application/pdf",
  "text/plain",
  "text/markdown",
] as const;

export const PROJECT_MEDIA_CONTENT_TYPES = [
  ...PROJECT_MEDIA_IMAGE_TYPES,
  ...PROJECT_MEDIA_VIDEO_TYPES,
  ...PROJECT_MEDIA_DOCUMENT_TYPES,
] as const;

export const PROJECT_MEDIA_ACCEPT_ATTRIBUTE =
  PROJECT_MEDIA_CONTENT_TYPES.join(",");

export type ProjectMediaKind = "image" | "video" | "document";

/**
 * One uploaded file as the project record stores it. `bucketPath` is the record of
 * truth - it is what the storage service issued and what a re-signed URL is derived
 * from; `url` is the public URL captured at upload time so a reader can render the
 * file without a second round trip.
 */
export const projectMediaItemSchema = z
  .object({
    bucketPath: z.string().min(1).max(1_024),
    url: z.string().max(2_048),
    fileName: z.string().max(256),
    contentType: z.string().max(128),
    kind: z.enum(["image", "video", "document"]),
  })
  .strict();

export type ProjectMediaItem = z.infer<typeof projectMediaItemSchema>;

const IMAGE_EXTENSIONS = ["png", "jpg", "jpeg", "webp", "gif", "avif", "svg"];
const VIDEO_EXTENSIONS = ["mp4", "webm", "mov", "m4v"];

/** Which strip a file belongs in, or null when the platform does not accept the type. */
export function classifyProjectMediaKind(
  contentType: string,
): ProjectMediaKind | null {
  const normalized = contentType.trim().toLowerCase();
  if ((PROJECT_MEDIA_IMAGE_TYPES as readonly string[]).includes(normalized))
    return "image";
  if ((PROJECT_MEDIA_VIDEO_TYPES as readonly string[]).includes(normalized))
    return "video";
  if ((PROJECT_MEDIA_DOCUMENT_TYPES as readonly string[]).includes(normalized))
    return "document";
  return null;
}

export function isSupportedProjectMediaType(contentType: string): boolean {
  return classifyProjectMediaKind(contentType) !== null;
}

function fileNameFromPath(path: string): string {
  const withoutQuery = path.split("?")[0] ?? path;
  const segments = withoutQuery.split("/");
  return segments[segments.length - 1] ?? withoutQuery;
}

function kindFromPath(path: string): ProjectMediaKind {
  const extension = fileNameFromPath(path).split(".").pop()?.toLowerCase() ?? "";
  if (IMAGE_EXTENSIONS.includes(extension)) return "image";
  if (VIDEO_EXTENSIONS.includes(extension)) return "video";
  return "document";
}

function isRenderableLocation(value: string): boolean {
  return (
    value.startsWith("https://") ||
    value.startsWith("http://") ||
    value.startsWith("/")
  );
}

/** A bare path or URL from a legacy comma-separated media column. */
function mediaItemFromLocation(location: string): ProjectMediaItem | null {
  const trimmed = location.trim();
  if (!trimmed) return null;

  return {
    bucketPath: trimmed,
    url: isRenderableLocation(trimmed) ? trimmed : "",
    fileName: fileNameFromPath(trimmed),
    contentType: "",
    kind: kindFromPath(trimmed),
  };
}

/** The stored form of a project's media: a JSON array, so readers never split strings. */
export function serializeProjectMediaItems(
  items: readonly ProjectMediaItem[],
): string {
  return JSON.stringify(z.array(projectMediaItemSchema).parse(items));
}

/**
 * Reads a stored media column. Accepts the JSON array this contract writes, a JSON
 * array of bare bucket paths, and the legacy comma-separated string the older `images`
 * column holds - so a project written before this field existed still renders instead
 * of printing its raw column value at a reader.
 */
export function parseProjectMediaItems(
  stored: string | null | undefined,
): ProjectMediaItem[] {
  const raw = stored?.trim();
  if (!raw) return [];

  let decoded: unknown;
  try {
    decoded = JSON.parse(raw);
  } catch {
    return raw
      .split(",")
      .map(mediaItemFromLocation)
      .filter((item): item is ProjectMediaItem => item !== null);
  }

  if (!Array.isArray(decoded)) return [];

  return decoded
    .map((entry): ProjectMediaItem | null => {
      if (typeof entry === "string") return mediaItemFromLocation(entry);
      const parsed = projectMediaItemSchema.safeParse(entry);
      return parsed.success ? parsed.data : null;
    })
    .filter((item): item is ProjectMediaItem => item !== null);
}

export const projectCreateFormDataSchema = z
  .object({
    title: draftTextSchema,
    // Additive with a default so a draft saved before these fields existed still parses.
    summary: draftTextSchema.default(""),
    fundingGoal: z.string().max(64).default(""),
    media: z.array(projectMediaItemSchema).max(PROJECT_MEDIA_MAX_FILES).default([]),
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
  compensationModel?: "fixed" | "hourly" | "negotiable";
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
    summary: "",
    fundingGoal: "",
    media: [],
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

/**
 * How a value-action account check resolved. Mirrors the four outcomes of the
 * server gate (`AzoaValueEligibilityService.RequireReadyAsync`) so an outage is
 * never presented as unfinished setup, and a person waiting on a human decision
 * is never told to redo a check they already passed.
 *
 * CHECKLIST NOTE - per-person limits. A limit of the form "one X per person" is
 * expressed as an AZOA uniqueness requirement - one verified person resolves to
 * exactly one avatar, and AZOA enforces that uniqueness at the identity - never
 * as a count of ArdaNova rows, which are per-account and so count accounts, not
 * people. A uniqueness denial reuses `VALUE_ACTION_SETUP_INCOMPLETE_MESSAGE`
 * verbatim: it must be indistinguishable from ordinary ineligibility (D23), or
 * the copy tells one tenant that its person exists on another.
 */
export type ValueActionAccountState =
  | "ready"
  | "setup-incomplete"
  | "under-review"
  | "service-unavailable";

/** The readiness projection the gate reads; further fields on the API DTO are ignored. */
export interface ValueActionAccountStatus {
  avatarId?: string | null;
  walletId?: string | null;
  walletAddress?: string | null;
  kycStatus: string;
  identityReady: boolean;
  kycReady: boolean;
  walletReady: boolean;
  ready: boolean;
  unavailableReason?: string | null;
  /** When the open compliance review is due a decision; set only under review. */
  reviewDueAt?: string | null;
}

export interface ValueActionAccountEvaluation {
  state: ValueActionAccountState;
  ready: boolean;
  message: string | null;
}

/** Only an approved identity check clears a value-bearing action; every other status is incomplete. */
export const VALUE_ACTION_APPROVED_KYC_STATUS = "Approved";

/**
 * Setup is finished and a reviewer still owes this account a decision. The wire value
 * must match AZOA TenantKycStatus member for member; the gateway parses it by name.
 */
export const VALUE_ACTION_UNDER_REVIEW_KYC_STATUS = "UnderReview";

export const VALUE_ACTION_SETUP_INCOMPLETE_MESSAGE =
  "Finish your account setup before you can fund work or be paid. You can keep creating projects and collaborating while setup is pending.";

export const VALUE_ACTION_SERVICE_UNAVAILABLE_MESSAGE =
  "We could not confirm your account setup just now, so nothing was moved. This is not a problem with your account, so please try again shortly.";

const reviewDecisionDateFormat = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

/**
 * What a held account is told. Mirrors ValueEligibilityMessages.UnderReview on the
 * server, and like it says nothing about why a review is open - not the check that
 * opened it, not a score, not a name match. It says the two things this person can act
 * on: nothing is left for them to redo, and a human can be reached.
 */
export function describeValueActionUnderReview(
  reviewDueAt?: string | null,
): string {
  const due = reviewDueAt ? new Date(reviewDueAt) : null;
  const decisionExpected =
    due && !Number.isNaN(due.getTime())
      ? `by ${reviewDecisionDateFormat.format(due)}`
      : "shortly";
  return `Your identity check is complete and our review team is finishing one last check on your account, so nothing was moved. A decision is expected ${decisionExpected} - there is nothing for you to redo. Contact support if you need it sooner.`;
}

/**
 * Classifies a value-action account. An unread, failed, or node-disabled account
 * is "service-unavailable" and must never be shown as unfinished setup; pass
 * `loadFailed` when the status request itself errored. An account held for a human
 * decision is "under-review" and must never be shown as either of those. A request
 * still in flight is none of them - render a loading state instead of calling this.
 */
export function evaluateValueActionAccount(
  status: ValueActionAccountStatus | null | undefined,
  options: { loadFailed?: boolean } = {},
): ValueActionAccountEvaluation {
  if (
    options.loadFailed === true ||
    !status ||
    status.unavailableReason?.trim()
  ) {
    return {
      state: "service-unavailable",
      ready: false,
      message: VALUE_ACTION_SERVICE_UNAVAILABLE_MESSAGE,
    };
  }

  // Checked before readiness, exactly as the server orders it: setup is done and a person
  // still owes a decision, so neither "try again shortly" nor "finish your setup" is true.
  if (status.kycStatus === VALUE_ACTION_UNDER_REVIEW_KYC_STATUS) {
    return {
      state: "under-review",
      ready: false,
      message: describeValueActionUnderReview(status.reviewDueAt),
    };
  }

  if (hasEveryValueActionReadinessSignal(status)) {
    return { state: "ready", ready: true, message: null };
  }

  return {
    state: "setup-incomplete",
    ready: false,
    message: VALUE_ACTION_SETUP_INCOMPLETE_MESSAGE,
  };
}

/** One readiness rule for every participant: funder, owner, assignee and beneficiary alike. */
function hasEveryValueActionReadinessSignal(
  status: ValueActionAccountStatus,
): boolean {
  return Boolean(
    status.ready &&
      status.identityReady &&
      status.avatarId?.trim() &&
      status.kycReady &&
      status.kycStatus === VALUE_ACTION_APPROVED_KYC_STATUS &&
      status.walletReady &&
      status.walletId?.trim() &&
      status.walletAddress?.trim(),
  );
}

/**
 * Full account readiness is required for value-bearing actions, not drafts. Every
 * non-"ready" outcome is false here; which one it was decides the words, never the
 * permission - read `evaluateValueActionAccount` when the words matter.
 */
export function isValueActionAccountReady(
  status: ValueActionAccountStatus | null | undefined,
): boolean {
  return evaluateValueActionAccount(status).state === "ready";
}

/** A role with compensation terms cannot be published before account setup. */
export function isProjectRoleValueBearing(role: ProjectCreateRole): boolean {
  return Boolean(
    role.compensationModel ||
      role.shareAmount.trim() ||
      role.equityPercent.trim(),
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
    compensationModel: input.role.compensationModel
      ? toOpportunityCompensation(input.role.compensationModel)
      : undefined,
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

/**
 * The unit a funding goal is denominated in. Mirrors `ProjectFundingUnits.Ledger` on the
 * server, and it is DOLLARS: the same unit the raise accumulates in, so the ask and the
 * progress against it can be compared and divided without a conversion anywhere.
 *
 * It used to be "ARDA" while the server's raise counted dollars, which made the opening
 * offering price about a hundred times the founder's intent. The server now refuses any
 * other unit by name rather than trusting this constant.
 */
export const PROJECT_FUNDING_UNIT = "USD";

/** Dollars carry two decimal places, so a goal cannot be finer than a cent. */
export const PROJECT_FUNDING_DECIMALS = 2;

/**
 * The genesis seed: 1 ARDA = $0.01 at launch. The live rate is backing-derived and floats
 * from there (server-side, `SwapService`), which is why this is only the fallback used
 * when the treasury has not been read yet - never a stored or quoted price.
 */
export const ARDA_GENESIS_USD_RATE = 0.01;

/**
 * The live ARDA/USD rate from a treasury status payload, or null when the node has no
 * usable rate yet (no genesis, no treasury row, or a shape this client does not know).
 * The caller decides what to say about a missing rate - this never invents one.
 */
export function readArdaUsdRate(treasuryStatus: unknown): number | null {
  if (typeof treasuryStatus !== "object" || treasuryStatus === null) return null;
  const rate = (treasuryStatus as Record<string, unknown>).ardaValueUsd;
  if (typeof rate !== "number" || !Number.isFinite(rate) || rate <= 0) {
    return null;
  }
  return rate;
}

export function formatUsdAmount(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: amount >= 100 ? 0 : 2,
  }).format(amount);
}

export function formatFundingAmount(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: PROJECT_FUNDING_DECIMALS,
  }).format(amount);
}

/**
 * The founder-facing caption under a dollar ask: how many platform units that ask is worth
 * at the rate this client actually has, said as an approximation and never as a price.
 *
 * The ARGUMENT is dollars. The platform unit is derived FROM it and shown only as a caption -
 * it is never what a person types and never what is stored, because the raise it will be
 * compared against counts dollars.
 */
export function describeFundingGoalInPlatformUnits(
  fundingGoalDollars: number,
  liveRate: number | null,
): string {
  const rate = liveRate ?? ARDA_GENESIS_USD_RATE;
  if (!(rate > 0)) return "";
  const units = formatFundingAmount(fundingGoalDollars / rate);
  return liveRate === null
    ? `about ${units} platform units at the launch reference rate`
    : `about ${units} platform units at today's rate`;
}

/**
 * Validates the typed funding goal, IN DOLLARS. Returns undefined for an empty field - the
 * goal is optional - and throws with sponsor-readable text for anything that is not a usable
 * amount, so a bad value never reaches the create call as NaN.
 */
export function parseFundingGoalDollars(value: string): number | undefined {
  const normalized = value.trim().replace(/,/g, "");
  if (!normalized) return undefined;

  const parsed = Number(normalized);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    throw new Error("Funding goal must be a positive amount");
  }
  if (parsed > 1_000_000_000) {
    throw new Error("Funding goal is larger than this platform supports");
  }

  const decimals = normalized.split(".")[1]?.length ?? 0;
  if (decimals > PROJECT_FUNDING_DECIMALS) {
    throw new Error(
      `Funding goal cannot be finer than ${PROJECT_FUNDING_DECIMALS} decimal places`,
    );
  }

  return parsed;
}

/** The one-line ask, or a message naming what is wrong with it. Empty is allowed. */
export function validateProjectSummary(value: string): string | null {
  const normalized = value.trim();
  if (!normalized) return null;
  if (normalized.length < PROJECT_SUMMARY_MIN_LENGTH) {
    return `Say a little more - at least ${PROJECT_SUMMARY_MIN_LENGTH} characters`;
  }
  if (normalized.length > PROJECT_SUMMARY_MAX_LENGTH) {
    return `Keep it to ${PROJECT_SUMMARY_MAX_LENGTH} characters`;
  }
  return null;
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

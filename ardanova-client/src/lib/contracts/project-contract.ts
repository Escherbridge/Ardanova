import { z } from "zod";

export const PROJECT_STATUSES = [
  "DRAFT",
  "PUBLISHED",
  "SEEKING_SUPPORT",
  "FUNDED",
  "IN_PROGRESS",
  "COMPLETED",
  "CANCELLED",
] as const;

export const PROJECT_TYPES = [
  "TEMPORARY",
  "LONG_TERM",
  "FOUNDATION",
  "BUSINESS",
  "PRODUCT",
  "OPEN_SOURCE",
  "COMMUNITY",
] as const;

export const PROJECT_DURATIONS = [
  "ONE_TWO_WEEKS",
  "ONE_THREE_MONTHS",
  "THREE_SIX_MONTHS",
  "SIX_TWELVE_MONTHS",
  "ONE_TWO_YEARS",
  "TWO_PLUS_YEARS",
  "ONGOING",
] as const;

export const PROJECT_ROLES = [
  "FOUNDER",
  "LEADER",
  "CORE_CONTRIBUTOR",
  "CONTRIBUTOR",
  "OBSERVER",
] as const;

export const MILESTONE_STATUSES = [
  "PLANNED",
  "IN_PROGRESS",
  "COMPLETED",
  "CANCELLED",
] as const;

export const PRIORITIES = ["LOW", "MEDIUM", "HIGH", "CRITICAL"] as const;

export const projectStatusSchema = z.enum(PROJECT_STATUSES);
export const projectTypeSchema = z.enum(PROJECT_TYPES);
export const projectDurationSchema = z.enum(PROJECT_DURATIONS);
export const projectRoleSchema = z.enum(PROJECT_ROLES);
export const milestoneStatusSchema = z.enum(MILESTONE_STATUSES);
export const prioritySchema = z.enum(PRIORITIES);
const dotNetDateTimeSchema = z.string().datetime({ offset: true, local: true });

export type ProjectStatus = z.infer<typeof projectStatusSchema>;
export type ProjectType = z.infer<typeof projectTypeSchema>;
export type ProjectDuration = z.infer<typeof projectDurationSchema>;
export type ProjectRole = z.infer<typeof projectRoleSchema>;
export type MilestoneStatus = z.infer<typeof milestoneStatusSchema>;
export type Priority = z.infer<typeof prioritySchema>;

export const projectCreatorDtoSchema = z.object({
  id: z.string().min(1),
  name: z.string().nullable(),
  image: z.string().nullable(),
});

export const projectDtoSchema = z.object({
  id: z.string().min(1),
  title: z.string(),
  slug: z.string(),
  description: z.string(),
  problemStatement: z.string(),
  solution: z.string(),
  categories: z.array(z.string()),
  status: projectStatusSchema,
  fundingGoal: z.number().nullable(),
  currentFunding: z.number(),
  supportersCount: z.number().int(),
  votesCount: z.number().int(),
  viewsCount: z.number().int(),
  featured: z.boolean(),
  tags: z.string().nullable(),
  images: z.string().nullable(),
  videos: z.string().nullable(),
  documents: z.string().nullable(),
  targetAudience: z.string().nullable(),
  expectedImpact: z.string().nullable(),
  timeline: z.string().nullable(),
  projectType: projectTypeSchema,
  duration: projectDurationSchema.nullable(),
  createdAt: z.string(),
  updatedAt: z.string(),
  publishedAt: z.string().nullable(),
  fundedAt: z.string().nullable(),
  completedAt: z.string().nullable(),
  commerceEnabled: z.boolean(),
  storefrontDescription: z.string().nullable(),
  createdById: z.string().min(1),
  createdBy: projectCreatorDtoSchema.nullable(),
  assignedGuildId: z.string().nullable(),
});

export const createProjectDtoSchema = z.object({
  title: z.string().min(1),
  description: z.string().min(1),
  problemStatement: z.string().min(1),
  solution: z.string().min(1),
  categories: z.array(z.string()).min(1),
  fundingGoal: z.number().nullable().optional(),
  tags: z.string().nullable().optional(),
  images: z.string().nullable().optional(),
  videos: z.string().nullable().optional(),
  documents: z.string().nullable().optional(),
  targetAudience: z.string().nullable().optional(),
  expectedImpact: z.string().nullable().optional(),
  timeline: z.string().nullable().optional(),
  projectType: projectTypeSchema.optional(),
  duration: projectDurationSchema.nullable().optional(),
  commerceEnabled: z.boolean().optional(),
  storefrontDescription: z.string().nullable().optional(),
});

export const updateProjectDtoSchema = createProjectDtoSchema
  .partial()
  .extend({ status: projectStatusSchema.optional() });

export const pagedProjectsDtoSchema = z.object({
  items: z.array(projectDtoSchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
  hasNextPage: z.boolean(),
  hasPreviousPage: z.boolean(),
});

export const projectMilestoneDtoSchema = z
  .object({
    id: z.string().min(1),
    projectId: z.string().min(1),
    title: z.string(),
    description: z.string().nullable(),
    targetDate: dotNetDateTimeSchema.nullable(),
    completedAt: dotNetDateTimeSchema.nullable(),
    status: milestoneStatusSchema,
    priority: prioritySchema,
    order: z.number().int(),
    createdAt: dotNetDateTimeSchema,
    updatedAt: dotNetDateTimeSchema,
  })
  .strict();

export const createProjectMilestoneDtoSchema = z
  .object({
    title: z.string().min(1),
    description: z.string().nullable().optional(),
    targetDate: z.string().datetime({ offset: true }).nullable().optional(),
    status: milestoneStatusSchema.optional(),
    priority: prioritySchema.optional(),
  })
  .strict();

export const updateProjectMilestoneDtoSchema =
  createProjectMilestoneDtoSchema.partial();

export type ProjectCreatorDto = z.infer<typeof projectCreatorDtoSchema>;
export type ProjectDto = z.infer<typeof projectDtoSchema>;
export type CreateProjectDto = z.infer<typeof createProjectDtoSchema>;
export type UpdateProjectDto = z.infer<typeof updateProjectDtoSchema>;
export type PagedProjectsDto = z.infer<typeof pagedProjectsDtoSchema>;
export type ProjectMilestoneDto = z.infer<typeof projectMilestoneDtoSchema>;
export type CreateProjectMilestoneDto = z.infer<
  typeof createProjectMilestoneDtoSchema
>;
export type UpdateProjectMilestoneDto = z.infer<
  typeof updateProjectMilestoneDtoSchema
>;

export function isProjectStatus(value: string): value is ProjectStatus {
  return projectStatusSchema.safeParse(value).success;
}

export function isProjectType(value: string): value is ProjectType {
  return projectTypeSchema.safeParse(value).success;
}

export function isProjectDuration(value: string): value is ProjectDuration {
  return projectDurationSchema.safeParse(value).success;
}

export function isProjectRole(value: string): value is ProjectRole {
  return projectRoleSchema.safeParse(value).success;
}

import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Project type enum - what kind of project this is
const ProjectType = z.enum([
  "TEMPORARY",
  "LONG_TERM",
  "FOUNDATION",
  "BUSINESS",
  "PRODUCT",
  "OPEN_SOURCE",
  "COMMUNITY",
]);

// Project duration enum - expected timeline
const ProjectDuration = z.enum([
  "ONE_TWO_WEEKS",
  "ONE_THREE_MONTHS",
  "THREE_SIX_MONTHS",
  "SIX_TWELVE_MONTHS",
  "ONE_TWO_YEARS",
  "TWO_PLUS_YEARS",
  "ONGOING",
]);

// Project status enum (matches .NET backend)
const ProjectStatus = z.enum([
  "DRAFT",
  "PUBLISHED",
  "SEEKING_SUPPORT",
  "FUNDED",
  "IN_PROGRESS",
  "COMPLETED",
  "CANCELLED",
]);

// Project creation input schema
const createProjectSchema = z.object({
  title: z.string().min(1, "Project title is required"),
  description: z.string().min(10, "Description must be at least 10 characters"),
  problemStatement: z.string().min(10, "Problem statement must be at least 10 characters"),
  solution: z.string().min(10, "Solution must be at least 10 characters"),
  categories: z.array(z.string()).min(1, "At least one category is required"),
  projectType: ProjectType.optional(),
  duration: ProjectDuration.optional(),
  targetAudience: z.string().optional(),
  expectedImpact: z.string().optional(),
  timeline: z.string().optional(),
  tags: z.string().optional(),
  images: z.string().optional(),
  videos: z.string().optional(),
  documents: z.string().optional(),
});

// Project update input schema
const updateProjectSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  problemStatement: z.string().min(10).optional(),
  solution: z.string().min(10).optional(),
  categories: z.array(z.string()).min(1).optional(),
  projectType: ProjectType.optional(),
  duration: ProjectDuration.optional(),
  status: ProjectStatus.optional(),
  targetAudience: z.string().optional(),
  expectedImpact: z.string().optional(),
  timeline: z.string().optional(),
  tags: z.string().optional(),
  images: z.string().optional(),
  videos: z.string().optional(),
  documents: z.string().optional(),
  fundingGoal: z.number().optional(),
});

// Member role enum
const MemberRole = z.enum([
  "FOUNDER",
  "LEADER",
  "CORE_CONTRIBUTOR",
  "CONTRIBUTOR",
  "OBSERVER",
]);

// Application status enum
const ApplicationStatus = z.enum([
  "PENDING",
  "ACCEPTED",
  "REJECTED",
  "WITHDRAWN",
]);

// Proposal type enum
const ProposalType = z.enum([
  "TREASURY",
  "GOVERNANCE",
  "STRATEGIC",
  "OPERATIONAL",
  "EMERGENCY",
]);

// Support type enum
const SupportType = z.enum([
  "VOTE",
  "SUBSCRIPTION",
  "VOLUNTEER",
  "RESOURCE",
]);

// Resource schemas
const addResourceSchema = z.object({
  projectId: z.string(),
  name: z.string().min(1),
  description: z.string().optional(),
  quantity: z.number().default(1),
  estimatedCost: z.number().optional(),
  recurringCost: z.number().min(0).optional(),
  recurringIntervalDays: z.number().int().min(1).max(365).optional(),
  isRequired: z.boolean().default(true),
});

const updateResourceSchema = z.object({
  resourceId: z.string(),
  name: z.string().min(1).optional(),
  description: z.string().optional(),
  quantity: z.number().optional(),
  estimatedCost: z.number().optional(),
  recurringCost: z.number().min(0).optional(),
  recurringIntervalDays: z.number().int().min(1).max(365).optional(),
  isRequired: z.boolean().optional(),
});

// Milestone schemas
const addMilestoneSchema = z.object({
  projectId: z.string(),
  title: z.string().min(1),
  description: z.string().optional(),
  targetDate: z.string(),
});

const updateMilestoneSchema = z.object({
  projectId: z.string(),
  milestoneId: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().optional(),
  targetDate: z.string().optional(),
});

// Member schemas
const addMemberSchema = z.object({
  projectId: z.string(),
  userId: z.string(),
  role: MemberRole,
});

const updateMemberRoleSchema = z.object({
  memberId: z.string(),
  role: MemberRole,
});

// Application schemas
const applyToProjectSchema = z.object({
  projectId: z.string(),
  roleTitle: z.string(),
  message: z.string().min(20),
  skills: z.string().optional(),
  experience: z.string().optional(),
  availability: z.string().optional(),
});

const reviewApplicationSchema = z.object({
  applicationId: z.string(),
  status: ApplicationStatus,
  reviewMessage: z.string().optional(),
});

// Proposal schemas
const createProposalSchema = z.object({
  projectId: z.string(),
  type: ProposalType,
  title: z.string().min(1),
  description: z.string().min(20),
  options: z.array(z.string()).min(2),
  quorum: z.number().default(50),
  threshold: z.number().default(51),
  votingDays: z.number().default(7),
});

const castVoteSchema = z.object({
  proposalId: z.string(),
  choice: z.number(),
  reason: z.string().optional(),
});

const updateProposalSchema = z.object({
  projectId: z.string(),
  proposalId: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(20).optional(),
  options: z.array(z.string()).min(2).optional(),
  quorum: z.number().optional(),
  threshold: z.number().optional(),
  votingDays: z.number().optional(),
});

const publishProposalSchema = z.object({
  projectId: z.string(),
  proposalId: z.string(),
});

const createProposalCommentSchema = z.object({
  proposalId: z.string(),
  content: z.string().min(1),
  parentId: z.string().optional(),
});

// Update schemas
const createUpdateSchema = z.object({
  projectId: z.string(),
  title: z.string().min(1),
  content: z.string().min(10),
  images: z.string().optional(),
});

// Comment schemas
const addCommentSchema = z.object({
  projectId: z.string(),
  content: z.string().min(1),
  parentId: z.string().optional(),
});

// Support schemas
const supportProjectSchema = z.object({
  projectId: z.string(),
  supportType: SupportType,
  monthlyAmount: z.number().optional(),
  message: z.string().optional(),
});

export const projectRouter = createTRPCRouter({
  // Create a new project
  create: protectedProcedure
    .input(createProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const { categories, ...rest } = input;
      const response = await apiClient.projects.create({
        ...rest,
        categories: categories,
        createdById: userId,
      } as any);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create project");
      }

      return response.data;
    }),

  // Get all projects with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        category: z.string().optional(),
        status: ProjectStatus.optional(),
        search: z.string().optional(),
      })
    )
    .query(async ({ input }) => {
      const { limit, page, category, status, search } = input;

      // Use search endpoint for all queries (handles filters and pagination)
      const response = await apiClient.projects.search({
        searchTerm: search,
        status: status,
        category: category,
        page: page,
        pageSize: limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage ? String(page + 1) : undefined,
        totalCount: response.data?.totalCount,
        totalPages: response.data?.totalPages,
      };
    }),

  // Get project by ID or slug
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // Try by ID first
      let response = await apiClient.projects.getById(input.id);

      // If not found, try by slug
      if (response.status === 404) {
        response = await apiClient.projects.getBySlug(input.id);
      }

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Project not found");
      }

      return response.data;
    }),

  // Get user's projects
  getMyProjects: protectedProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        status: ProjectStatus.optional(),
      })
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.getByUserId(userId);

      if (response.error) {
        throw new Error(response.error);
      }

      let items = response.data ?? [];

      // Filter by status if provided
      if (input.status) {
        items = items.filter(p => p.status === input.status);
      }

      return { items, nextCursor: undefined };
    }),

  // Get featured projects
  getFeatured: publicProcedure.query(async () => {
    const response = await apiClient.projects.getFeatured();

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Update project
  update: protectedProcedure
    .input(updateProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Verify ownership by fetching the project first
      const existing = await apiClient.projects.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Project not found");
      }

      if (existing.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.update(id, data as any);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update project");
      }

      return response.data;
    }),

  // Delete project
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const { id } = input;
      const userId = ctx.session.user.id;

      // Verify ownership by fetching the project first
      const existing = await apiClient.projects.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Project not found");
      }

      if (existing.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.delete(id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete project");
      }

      return { success: true };
    }),

  // Publish project
  publish: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const { id } = input;
      const userId = ctx.session.user.id;

      // Verify ownership by fetching the project first
      const existing = await apiClient.projects.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Project not found");
      }

      if (existing.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      if (existing.data.status !== "DRAFT") {
        throw new Error("Only draft projects can be published");
      }

      const response = await apiClient.projects.publish(id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to publish project");
      }

      return response.data;
    }),

  // Set featured status
  setFeatured: protectedProcedure
    .input(z.object({ id: z.string(), featured: z.boolean() }))
    .mutation(async ({ input }) => {
      const { id, featured } = input;

      const response = await apiClient.projects.setFeatured(id, featured);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update featured status");
      }

      return response.data;
    }),

  // ========================================
  // PROJECT RESOURCES
  // ========================================

  // Add resource to project
  addResource: protectedProcedure
    .input(addResourceSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      // Call API endpoint
      const response = await apiClient.projects.addResource(input.projectId, {
        name: input.name,
        description: input.description,
        quantity: input.quantity,
        estimatedCost: input.estimatedCost,
        recurringCost: input.recurringCost,
        recurringIntervalDays: input.recurringIntervalDays,
        isRequired: input.isRequired,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add resource");
      }

      return response.data;
    }),

  // Update resource
  updateResource: protectedProcedure
    .input(updateResourceSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const { resourceId, ...data } = input;

      // Get resource to verify ownership
      const resource = await apiClient.projects.getResourceById(resourceId);
      if (resource.error || !resource.data) {
        throw new Error("Resource not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(resource.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.updateResource(resourceId, data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update resource");
      }

      return response.data;
    }),

  // Delete resource
  deleteResource: protectedProcedure
    .input(z.object({ resourceId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get resource to verify ownership
      const resource = await apiClient.projects.getResourceById(input.resourceId);
      if (resource.error || !resource.data) {
        throw new Error("Resource not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(resource.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.deleteResource(input.resourceId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete resource");
      }

      return { success: true };
    }),

  // Get resources for project
  getResources: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getResources(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // ========================================
  // PROJECT MILESTONES
  // ========================================

  // Add milestone to project
  addMilestone: protectedProcedure
    .input(addMilestoneSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.addMilestone(input.projectId, {
        title: input.title,
        description: input.description,
        targetDate: input.targetDate,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add milestone");
      }

      return response.data;
    }),

  // Update milestone
  updateMilestone: protectedProcedure
    .input(updateMilestoneSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const { projectId, milestoneId, ...data } = input;

      // Verify project ownership
      const project = await apiClient.projects.getById(projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.updateMilestone(projectId, milestoneId, data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update milestone");
      }

      return response.data;
    }),

  // Delete milestone
  deleteMilestone: protectedProcedure
    .input(z.object({ projectId: z.string(), milestoneId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.deleteMilestone(input.projectId, input.milestoneId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete milestone");
      }

      return { success: true };
    }),

  // Get milestones for project
  getMilestones: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getMilestones(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Complete milestone
  completeMilestone: protectedProcedure
    .input(z.object({ projectId: z.string(), milestoneId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.completeMilestone(input.projectId, input.milestoneId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to complete milestone");
      }

      return response.data;
    }),

  // ========================================
  // PROJECT MEMBERS & ROLES
  // ========================================

  // Add member to project
  addMember: protectedProcedure
    .input(addMemberSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.addMember(input.projectId, {
        userId: input.userId,
        role: input.role,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add member");
      }

      return response.data;
    }),

  // Update member role
  updateMemberRole: protectedProcedure
    .input(updateMemberRoleSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get member to verify ownership
      const member = await apiClient.projects.getMemberById(input.memberId);
      if (member.error || !member.data) {
        throw new Error("Member not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(member.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.updateMemberRole(input.memberId, {
        role: input.role,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update member role");
      }

      return response.data;
    }),

  // Remove member from project
  removeMember: protectedProcedure
    .input(z.object({ memberId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get member to verify ownership
      const member = await apiClient.projects.getMemberById(input.memberId);
      if (member.error || !member.data) {
        throw new Error("Member not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(member.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.removeMember(input.memberId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to remove member");
      }

      return { success: true };
    }),

  // Get members of project
  getMembers: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getMembers(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // ========================================
  // PROJECT APPLICATIONS
  // ========================================

  // Apply to project
  applyToProject: protectedProcedure
    .input(applyToProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.applyToProject(input.projectId, {
        userId: userId,
        roleTitle: input.roleTitle,
        message: input.message,
        skills: input.skills,
        experience: input.experience,
        availability: input.availability,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to submit application");
      }

      return response.data;
    }),

  // Get applications for project (owner only)
  getApplications: protectedProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.getApplications(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Review application
  reviewApplication: protectedProcedure
    .input(reviewApplicationSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get application to verify ownership
      const application = await apiClient.projects.getApplicationById(input.applicationId);
      if (application.error || !application.data) {
        throw new Error("Application not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(application.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.reviewApplication(input.applicationId, {
        status: input.status,
        reviewMessage: input.reviewMessage,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to review application");
      }

      return response.data;
    }),

  // ========================================
  // PROPOSALS & VOTING
  // ========================================

  // Create proposal
  createProposal: protectedProcedure
    .input(createProposalSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify user is a member or founder of the project
      const [project, members] = await Promise.all([
        apiClient.projects.getById(input.projectId),
        apiClient.projects.getMembers(input.projectId),
      ]);

      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (members.error || !members.data) {
        throw new Error("Failed to verify membership");
      }

      const isFounder = project.data.createdById === userId;
      const isMember = members.data.some((m: any) => m.userId === userId);
      if (!isFounder && !isMember) {
        throw new Error("Only project members can create proposals");
      }

      const response = await apiClient.projects.createProposal(input.projectId, {
        createdById: userId,
        type: input.type,
        title: input.title,
        description: input.description,
        options: input.options,
        quorum: input.quorum,
        threshold: input.threshold,
        votingDays: input.votingDays,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create proposal");
      }

      return response.data;
    }),

  // Get proposals for project
  getProposals: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getProposals(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      // Handle paged response from backend (returns { items: [...], page, totalCount, etc. })
      const data = response.data;
      if (data && typeof data === 'object' && 'items' in data) {
        return (data as { items: any[] }).items ?? [];
      }

      // Fallback for direct array response
      return Array.isArray(data) ? data : [];
    }),

  // Get proposal by ID
  getProposalById: publicProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getProposalById(input.proposalId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Proposal not found");
      }

      return response.data;
    }),

  // Cast vote on proposal
  castVote: protectedProcedure
    .input(castVoteSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get proposal to verify membership
      const proposal = await apiClient.projects.getProposalById(input.proposalId);
      if (proposal.error || !proposal.data) {
        throw new Error("Proposal not found");
      }

      // Verify user is a member or founder of the project
      const [project, members] = await Promise.all([
        apiClient.projects.getById(proposal.data.projectId),
        apiClient.projects.getMembers(proposal.data.projectId),
      ]);

      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (members.error || !members.data) {
        throw new Error("Failed to verify membership");
      }

      const isFounder = project.data.createdById === userId;
      const isMember = members.data.some((m: any) => m.userId === userId);
      if (!isFounder && !isMember) {
        throw new Error("Only project members can vote");
      }

      // Verify user has an active MembershipCredential (governance right)
      const credential = await apiClient.membershipCredentials.getByProjectAndUser(proposal.data.projectId, userId);
      const hasActiveCredential = !credential.error && credential.data && credential.data.status === 'ACTIVE';

      if (!hasActiveCredential) {
        if (isFounder) {
          // Auto-grant FOUNDER credential if founder doesn't have one yet
          const granted = await apiClient.membershipCredentials.grant({
            projectId: proposal.data.projectId,
            userId,
            grantedVia: 'FOUNDER',
          });
          if (granted.error || !granted.data) {
            throw new Error("Failed to auto-grant founder credential. Please try again.");
          }
        } else {
          // Non-founders must complete KYC to get a credential
          throw new Error("CREDENTIAL_REQUIRED: You need an active membership credential to vote. Please complete identity verification (KYC) to receive your credential.");
        }
      }

      const response = await apiClient.projects.castVote(input.proposalId, {
        userId: userId,
        choice: input.choice,
        reason: input.reason,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to cast vote");
      }

      return response.data;
    }),

  // Close proposal
  closeProposal: protectedProcedure
    .input(z.object({ proposalId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get proposal to verify ownership
      const proposal = await apiClient.projects.getProposalById(input.proposalId);
      if (proposal.error || !proposal.data) {
        throw new Error("Proposal not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(proposal.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.closeProposal(input.proposalId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to close proposal");
      }

      return response.data;
    }),

  // Get proposal with vote summary
  getProposalWithVotes: publicProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input }) => {
      const proposalResponse = await apiClient.projects.getProposalById(input.proposalId);

      if (proposalResponse.error || !proposalResponse.data) {
        throw new Error(proposalResponse.error ?? "Proposal not found");
      }

      // Try to get vote summary from governance endpoint
      const voteSummary = await apiClient.governance.getVoteSummary(input.proposalId);

      return {
        ...proposalResponse.data,
        voteSummary: voteSummary.data ?? null,
      };
    }),

  // Get all votes for a proposal
  getProposalVotes: publicProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.governance.getVotes(input.proposalId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Update proposal (DRAFT only)
  updateProposal: protectedProcedure
    .input(updateProposalSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get proposal to verify ownership
      const proposal = await apiClient.projects.getProposalById(input.proposalId);
      if (proposal.error || !proposal.data) {
        throw new Error("Proposal not found");
      }
      if (proposal.data.creatorId !== userId) {
        throw new Error("Only the proposal creator can edit");
      }

      const response = await apiClient.projects.updateProposal(input.projectId, input.proposalId, {
        title: input.title,
        description: input.description,
        options: input.options ? JSON.stringify(input.options) : undefined,
        quorum: input.quorum,
        threshold: input.threshold,
        votingDays: input.votingDays,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update proposal");
      }

      return response.data;
    }),

  // Publish proposal (DRAFT -> ACTIVE)
  publishProposal: protectedProcedure
    .input(publishProposalSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get proposal to verify ownership
      const proposal = await apiClient.projects.getProposalById(input.proposalId);
      if (proposal.error || !proposal.data) {
        throw new Error("Proposal not found");
      }
      if (proposal.data.creatorId !== userId) {
        throw new Error("Only the proposal creator can publish");
      }

      const response = await apiClient.projects.publishProposal(input.projectId, input.proposalId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to publish proposal");
      }

      return response.data;
    }),

  // Get proposal comments
  getProposalComments: publicProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getProposalComments(input.proposalId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Create proposal comment
  createProposalComment: protectedProcedure
    .input(createProposalCommentSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.createProposalComment(input.proposalId, {
        userId,
        content: input.content,
        parentId: input.parentId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create comment");
      }

      return response.data;
    }),

  // ========================================
  // PROJECT UPDATES
  // ========================================

  // Create project update
  createUpdate: protectedProcedure
    .input(createUpdateSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify user is owner or member of the project
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      const isOwner = project.data.createdById === userId;

      if (!isOwner) {
        const members = await apiClient.projects.getMembers(input.projectId);
        if (members.error || !members.data) {
          throw new Error("Failed to verify membership");
        }

        const isMember = members.data.some((m: any) => m.userId === userId);
        if (!isMember) {
          throw new Error("Only project owner or members can post updates");
        }
      }

      const response = await apiClient.projects.createUpdate(input.projectId, {
        createdById: userId,
        title: input.title,
        content: input.content,
        images: input.images,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create update");
      }

      return response.data;
    }),

  // Get updates for project
  getUpdates: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getUpdates(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Delete update
  deleteUpdate: protectedProcedure
    .input(z.object({ updateId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get update to verify ownership
      const update = await apiClient.projects.getUpdateById(input.updateId);
      if (update.error || !update.data) {
        throw new Error("Update not found");
      }

      // Verify update ownership
      if (update.data.userId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.deleteUpdate(input.updateId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete update");
      }

      return { success: true };
    }),

  // ========================================
  // PROJECT COMMENTS
  // ========================================

  // Add comment to project
  addComment: protectedProcedure
    .input(addCommentSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.addComment(input.projectId, {
        projectId: input.projectId,
        userId: userId,
        content: input.content,
        parentId: input.parentId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add comment");
      }

      return response.data;
    }),

  // Get comments for project
  getComments: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getComments(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Delete comment
  deleteComment: protectedProcedure
    .input(z.object({ commentId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get comment to verify ownership
      const comment = await apiClient.projects.getCommentById(input.commentId);
      if (comment.error || !comment.data) {
        throw new Error("Comment not found");
      }

      // Verify comment ownership
      if (comment.data.userId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.deleteComment(input.commentId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete comment");
      }

      return { success: true };
    }),

  // ========================================
  // PROJECT SUPPORT
  // ========================================

  // Support a project
  supportProject: protectedProcedure
    .input(supportProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.supportProject(input.projectId, {
        userId: userId,
        supportType: input.supportType,
        monthlyAmount: input.monthlyAmount,
        message: input.message,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to support project");
      }

      return response.data;
    }),

  // Cancel support for project
  cancelSupport: protectedProcedure
    .input(z.object({ supportId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get support to verify ownership
      const support = await apiClient.projects.getSupportById(input.supportId);
      if (support.error || !support.data) {
        throw new Error("Support not found");
      }

      // Verify support ownership
      if (support.data.userId !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.projects.cancelSupport(input.supportId);

      if (response.error) {
        throw new Error(response.error ?? "Failed to cancel support");
      }

      return { success: true };
    }),

  // Get user's supported projects
  getMySupports: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;

    const response = await apiClient.projects.getUserSupports(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get supporters of a project
  getSupporters: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.projects.getSupporters(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),
});

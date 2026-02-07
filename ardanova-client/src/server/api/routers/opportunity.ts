import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import {
  canCreateGuildOpportunity,
  canCreateProjectOpportunity,
} from "~/server/api/lib/permissions";

// Enum values are API-driven from the backend enum service — validated server-side

// Opportunity creation input schema
const createOpportunitySchema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(20, "Description must be at least 20 characters"),
  type: z.string().min(1, "Type is required"),
  skills: z.array(z.string()).min(1, "At least one skill is required"),
  experienceLevel: z.string().optional(),
  compensationModel: z.string().optional(),
  compensationAmount: z.number().positive().optional(),
  location: z.string().optional(),
  isRemote: z.boolean().optional(),
  deadline: z.string().optional(),
  maxApplications: z.number().positive().optional(),
  projectId: z.string().optional(),
  guildId: z.string().optional(),
  taskId: z.string().optional(),
});

// Opportunity update input schema
const updateOpportunitySchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(20).optional(),
  type: z.string().optional(),
  skills: z.array(z.string()).optional(),
  experienceLevel: z.string().optional(),
  compensationModel: z.string().optional(),
  compensationAmount: z.number().positive().optional(),
  location: z.string().optional(),
  isRemote: z.boolean().optional(),
  deadline: z.string().optional(),
  maxApplications: z.number().positive().optional(),
});

export const opportunityRouter = createTRPCRouter({
  // Create a new opportunity
  create: protectedProcedure
    .input(createOpportunitySchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Validate permission based on entity type
      if (input.guildId) {
        const permission = await canCreateGuildOpportunity(ctx.db, userId, input.guildId);
        if (!permission.allowed) {
          throw new TRPCError({
            code: "FORBIDDEN",
            message: permission.reason || "You don't have permission to create opportunities for this guild",
          });
        }
      } else if (input.projectId) {
        const permission = await canCreateProjectOpportunity(ctx.db, userId, input.projectId);
        if (!permission.allowed) {
          throw new TRPCError({
            code: "FORBIDDEN",
            message: permission.reason || "You don't have permission to create opportunities for this project",
          });
        }
      } else {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: "An opportunity must be associated with a guild or project",
        });
      }

      const response = await apiClient.opportunities.create({
        posterId: userId,
        title: input.title,
        description: input.description,
        type: input.type,
        experienceLevel: input.experienceLevel,
        skills: input.skills.join(","),
        requirements: input.description,
        compensation: input.compensationAmount,
        compensationDetails: input.compensationModel,
        location: input.location,
        isRemote: input.isRemote ?? false,
        deadline: input.deadline,
        maxApplications: input.maxApplications,
        projectId: input.projectId,
        guildId: input.guildId,
        taskId: input.taskId,
      });

      if (response.error || !response.data) {
        const errorMessage = typeof response.error === "string" ? response.error : "Failed to create opportunity";
        throw new Error(errorMessage);
      }

      return response.data;
    }),

  // Get all opportunities with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        search: z.string().optional(),
        type: z.string().optional(),
        experienceLevel: z.string().optional(),
        skill: z.string().optional(),
        sourceType: z.enum(["guild", "project"]).optional(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.opportunities.search({
        searchTerm: input.search,
        type: input.type,
        experienceLevel: input.experienceLevel,
        skills: input.skill,
        sourceType: input.sourceType,
        page: input.page,
        pageSize: input.limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage ? String(input.page + 1) : undefined,
        totalCount: response.data?.totalCount ?? 0,
        totalPages: response.data?.totalPages ?? 0,
      };
    }),

  // Get opportunity by ID or slug
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      let response = await apiClient.opportunities.getById(input.id);

      // Fallback to slug lookup
      if (response.status === 404 || !response.data) {
        response = await apiClient.opportunities.getBySlug(input.id);
      }

      if (!response.data) {
        throw new Error("Opportunity not found");
      }

      return response.data;
    }),

  // Get user's posted opportunities
  getMyOpportunities: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.opportunities.getByPosterId(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get opportunities by guild ID
  getByGuildId: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunities.getByGuildId(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Get opportunities by project ID
  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunities.getByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Apply to an opportunity
  submitApplication: protectedProcedure
    .input(
      z.object({
        opportunityId: z.string(),
        coverLetter: z.string().min(20, "Cover letter must be at least 20 characters"),
        portfolio: z.string().url().optional(),
        additionalInfo: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.opportunities.apply(input.opportunityId, {
        applicantId: userId,
        coverLetter: input.coverLetter,
        portfolio: input.portfolio,
        additionalInfo: input.additionalInfo,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to apply for opportunity");
      }

      return { success: true, applicationId: response.data.id };
    }),

  // Get applications for an opportunity (only for poster)
  getApplications: protectedProcedure
    .input(z.object({ opportunityId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const opportunity = await apiClient.opportunities.getById(input.opportunityId);
      if (opportunity.error || !opportunity.data) {
        throw new Error("Opportunity not found");
      }

      if (opportunity.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.getApplications(input.opportunityId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Update application status (only for opportunity poster)
  updateApplicationStatus: protectedProcedure
    .input(
      z.object({
        applicationId: z.string(),
        opportunityId: z.string(),
        status: z.enum(["pending", "reviewing", "accepted", "rejected"]),
        reviewNotes: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership of opportunity
      const opportunity = await apiClient.opportunities.getById(input.opportunityId);
      if (opportunity.error || !opportunity.data) {
        throw new Error("Opportunity not found");
      }

      if (opportunity.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.updateApplicationStatus(input.applicationId, {
        status: input.status,
        reviewNotes: input.reviewNotes,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update application status");
      }

      return response.data;
    }),

  // Update opportunity
  update: protectedProcedure
    .input(updateOpportunitySchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.opportunities.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Opportunity not found");
      }

      if (existing.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.update(id, {
        title: data.title,
        description: data.description,
        type: data.type,
        experienceLevel: data.experienceLevel,
        skills: data.skills?.join(","),
        compensation: data.compensationAmount,
        compensationDetails: data.compensationModel,
        location: data.location,
        isRemote: data.isRemote,
        deadline: data.deadline,
        maxApplications: data.maxApplications,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update opportunity");
      }

      return response.data;
    }),

  // Close opportunity
  close: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.opportunities.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Opportunity not found");
      }

      if (existing.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.close(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // Delete opportunity
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.opportunities.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Opportunity not found");
      }

      if (existing.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.delete(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // ============ Updates ============

  // Get updates for an opportunity
  getUpdates: publicProcedure
    .input(z.object({ opportunityId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunities.getUpdates(input.opportunityId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Create an update (owner only)
  createUpdate: protectedProcedure
    .input(
      z.object({
        opportunityId: z.string(),
        title: z.string().min(1, "Title is required"),
        content: z.string().min(10, "Content must be at least 10 characters"),
        images: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const opportunity = await apiClient.opportunities.getById(input.opportunityId);
      if (opportunity.error || !opportunity.data) {
        throw new Error("Opportunity not found");
      }

      if (opportunity.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.createUpdate(input.opportunityId, {
        userId,
        title: input.title,
        content: input.content,
        images: input.images,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create update");
      }

      return response.data;
    }),

  // Delete an update (owner only)
  deleteUpdate: protectedProcedure
    .input(z.object({ updateId: z.string(), opportunityId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership of opportunity
      const opportunity = await apiClient.opportunities.getById(input.opportunityId);
      if (opportunity.error || !opportunity.data) {
        throw new Error("Opportunity not found");
      }

      if (opportunity.data.posterId !== userId) {
        throw new Error("Access denied: You do not own this opportunity");
      }

      const response = await apiClient.opportunities.deleteUpdate(input.updateId);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // ============ Comments ============

  // Get comments for an opportunity
  getComments: publicProcedure
    .input(z.object({ opportunityId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunities.getComments(input.opportunityId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Add a comment
  addComment: protectedProcedure
    .input(
      z.object({
        opportunityId: z.string(),
        content: z.string().min(1, "Comment cannot be empty"),
        parentId: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.opportunities.addComment(input.opportunityId, {
        userId,
        content: input.content,
        parentId: input.parentId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add comment");
      }

      return response.data;
    }),

  // Delete a comment (only comment author or opportunity owner)
  deleteComment: protectedProcedure
    .input(z.object({ commentId: z.string(), opportunityId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get opportunity to check ownership
      const opportunity = await apiClient.opportunities.getById(input.opportunityId);
      if (opportunity.error || !opportunity.data) {
        throw new Error("Opportunity not found");
      }

      // Get comments to find the specific comment
      const commentsResponse = await apiClient.opportunities.getComments(input.opportunityId);
      const comment = commentsResponse.data?.find((c) => c.id === input.commentId);

      if (!comment) {
        throw new Error("Comment not found");
      }

      // Allow deletion if user is opportunity owner or comment author
      const isOwner = opportunity.data.posterId === userId;
      const isAuthor = comment.userId === userId;

      if (!isOwner && !isAuthor) {
        throw new Error("Access denied: You cannot delete this comment");
      }

      const response = await apiClient.opportunities.deleteComment(input.commentId);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),
});

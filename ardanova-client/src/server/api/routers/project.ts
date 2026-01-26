import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Project category enum (matches .NET backend)
const ProjectCategory = z.enum([
  "TECHNOLOGY",
  "HEALTHCARE",
  "EDUCATION",
  "ENVIRONMENT",
  "SOCIAL_IMPACT",
  "BUSINESS",
  "ARTS_CULTURE",
  "AGRICULTURE",
  "FINANCE",
  "OTHER",
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
  category: ProjectCategory,
  targetAudience: z.string().optional(),
  expectedImpact: z.string().optional(),
  timeline: z.string().optional(),
  tags: z.string().optional(),
  images: z.string().optional(),
  videos: z.string().optional(),
  documents: z.string().optional(),
  fundingGoal: z.number().optional(),
});

// Project update input schema
const updateProjectSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  problemStatement: z.string().min(10).optional(),
  solution: z.string().min(10).optional(),
  category: ProjectCategory.optional(),
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

export const projectRouter = createTRPCRouter({
  // Create a new project
  create: protectedProcedure
    .input(createProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.create({
        ...input,
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
        category: ProjectCategory.optional(),
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
});

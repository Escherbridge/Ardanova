import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const SprintStatus = z.enum(['PLANNING', 'ACTIVE', 'COMPLETED', 'CANCELLED']);

const createSprintSchema = z.object({
  projectId: z.string().min(1),
  name: z.string().min(1).max(100),
  goal: z.string().optional(),
  startDate: z.string().datetime(),
  endDate: z.string().datetime(),
});

const updateSprintSchema = z.object({
  name: z.string().min(1).max(100).optional(),
  goal: z.string().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  status: SprintStatus.optional(),
});

const createSprintItemSchema = z.object({
  sprintId: z.string().min(1),
  backlogItemId: z.string().min(1),
  order: z.number().int().min(0),
});

const updateSprintItemSchema = z.object({
  order: z.number().int().min(0).optional(),
  completed: z.boolean().optional(),
});

export const sprintRouter = createTRPCRouter({
  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.sprints.getByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.sprints.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Sprint not found");
      }

      return response.data;
    }),

  create: protectedProcedure
    .input(createSprintSchema)
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

      const response = await apiClient.sprints.create(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create sprint");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateSprintSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.id);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update sprint");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.id);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete sprint");
      }

      return { success: true };
    }),

  start: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.id);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.start(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to start sprint");
      }

      return response.data;
    }),

  complete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.id);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.complete(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to complete sprint");
      }

      return response.data;
    }),

  cancel: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.id);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.cancel(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to cancel sprint");
      }

      return response.data;
    }),

  // ========================================
  // SPRINT ITEM OPERATIONS
  // ========================================

  getItems: publicProcedure
    .input(z.object({ sprintId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.sprints.getItems(input.sprintId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getItemById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.sprints.getItemById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Sprint item not found");
      }

      return response.data;
    }),

  addItem: protectedProcedure
    .input(createSprintItemSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.addItem(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add item to sprint");
      }

      return response.data;
    }),

  updateItem: protectedProcedure
    .input(z.object({ id: z.string(), data: updateSprintItemSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint item to verify ownership
      const item = await apiClient.sprints.getItemById(input.id);
      if (item.error || !item.data) {
        throw new Error("Sprint item not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(item.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.updateItem(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update sprint item");
      }

      return response.data;
    }),

  removeItem: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint item to verify ownership
      const item = await apiClient.sprints.getItemById(input.id);
      if (item.error || !item.data) {
        throw new Error("Sprint item not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(item.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(sprint.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.sprints.removeItem(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to remove item from sprint");
      }

      return { success: true };
    }),
});

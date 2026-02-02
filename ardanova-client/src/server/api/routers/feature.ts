import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const FeatureStatusEnum = z.enum(['PLANNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED']);
export const FeaturePriority = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

const createFeatureSchema = z.object({
  sprintId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  priority: FeaturePriority.optional(),
  order: z.number().optional(),
});

const updateFeatureSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  description: z.string().optional(),
  priority: FeaturePriority.optional(),
  status: FeatureStatusEnum.optional(),
  order: z.number().optional(),
  assigneeId: z.string().nullable().optional(),
});

export const featureRouter = createTRPCRouter({
  getBySprintId: publicProcedure
    .input(z.object({ sprintId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.features.getBySprintId(input.sprintId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.features.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Feature not found");
      }

      return response.data;
    }),

  create: protectedProcedure
    .input(createFeatureSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(input.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(sprint.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get milestone to verify ownership
      const milestone = await apiClient.milestones.getById(epic.data.milestoneId);
      if (milestone.error || !milestone.data) {
        throw new Error("Milestone not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(milestone.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.features.create(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create feature");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateFeatureSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(input.id);
      if (feature.error || !feature.data) {
        throw new Error("Feature not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(feature.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(sprint.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get milestone to verify ownership
      const milestone = await apiClient.milestones.getById(epic.data.milestoneId);
      if (milestone.error || !milestone.data) {
        throw new Error("Milestone not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(milestone.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.features.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update feature");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(input.id);
      if (feature.error || !feature.data) {
        throw new Error("Feature not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(feature.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(sprint.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get milestone to verify ownership
      const milestone = await apiClient.milestones.getById(epic.data.milestoneId);
      if (milestone.error || !milestone.data) {
        throw new Error("Milestone not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(milestone.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.features.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete feature");
      }

      return { success: true };
    }),

  assign: protectedProcedure
    .input(z.object({ id: z.string(), userId: z.string().nullable() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(input.id);
      if (feature.error || !feature.data) {
        throw new Error("Feature not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(feature.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(sprint.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get milestone to verify ownership
      const milestone = await apiClient.milestones.getById(epic.data.milestoneId);
      if (milestone.error || !milestone.data) {
        throw new Error("Milestone not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(milestone.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.features.assign(input.id, input.userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to assign feature");
      }

      return response.data;
    }),

  updateStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: FeatureStatusEnum }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(input.id);
      if (feature.error || !feature.data) {
        throw new Error("Feature not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(feature.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(sprint.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get milestone to verify ownership
      const milestone = await apiClient.milestones.getById(epic.data.milestoneId);
      if (milestone.error || !milestone.data) {
        throw new Error("Milestone not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(milestone.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.features.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update feature status");
      }

      return response.data;
    }),

  updatePriority: protectedProcedure
    .input(z.object({ id: z.string(), priority: FeaturePriority }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(input.id);
      if (feature.error || !feature.data) {
        throw new Error("Feature not found");
      }

      // Get sprint to verify ownership
      const sprint = await apiClient.sprints.getById(feature.data.sprintId);
      if (sprint.error || !sprint.data) {
        throw new Error("Sprint not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(sprint.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get milestone to verify ownership
      const milestone = await apiClient.milestones.getById(epic.data.milestoneId);
      if (milestone.error || !milestone.data) {
        throw new Error("Milestone not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(milestone.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.features.updatePriority(input.id, input.priority);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update feature priority");
      }

      return response.data;
    }),
});

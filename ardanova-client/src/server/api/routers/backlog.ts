import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// PBI enums
export const PBIType = z.enum(['FEATURE', 'ENHANCEMENT', 'BUG', 'TECHNICAL_DEBT', 'SPIKE']);
export const PBIStatus = z.enum(['NEW', 'READY', 'IN_PROGRESS', 'DONE', 'REMOVED']);

export const Priority = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

// PBI schemas
const createPbiSchema = z.object({
  featureId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  type: PBIType.default('FEATURE'),
  priority: Priority.default('MEDIUM'),
  storyPoints: z.number().int().min(0).optional(),
  acceptanceCriteria: z.string().optional(),
});

const updatePbiSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  description: z.string().optional(),
  type: PBIType.optional(),
  priority: Priority.optional(),
  status: PBIStatus.optional(),
  storyPoints: z.number().int().min(0).optional(),
  acceptanceCriteria: z.string().optional(),
  assigneeId: z.string().nullable().optional(),
});


export const backlogRouter = createTRPCRouter({
  // ========================================
  // PBI OPERATIONS
  // ========================================

  getPbisByFeatureId: publicProcedure
    .input(z.object({ featureId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.pbis.getByFeatureId(input.featureId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getPbiById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.pbis.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "PBI not found");
      }

      return response.data;
    }),

  createPbi: protectedProcedure
    .input(createPbiSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(input.featureId);
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

      const response = await apiClient.pbis.create(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create PBI");
      }

      return response.data;
    }),

  updatePbi: protectedProcedure
    .input(z.object({ id: z.string(), data: updatePbiSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(input.id);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(pbi.data.featureId);
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

      const response = await apiClient.pbis.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update PBI");
      }

      return response.data;
    }),

  deletePbi: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(input.id);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(pbi.data.featureId);
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

      const response = await apiClient.pbis.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete PBI");
      }

      return { success: true };
    }),

  assignPbi: protectedProcedure
    .input(z.object({ id: z.string(), userId: z.string().nullable() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(input.id);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(pbi.data.featureId);
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

      const response = await apiClient.pbis.assign(input.id, input.userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to assign PBI");
      }

      return response.data;
    }),

  updatePbiStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: PBIStatus }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(input.id);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get feature to verify ownership
      const feature = await apiClient.features.getById(pbi.data.featureId);
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

      const response = await apiClient.pbis.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update PBI status");
      }

      return response.data;
    }),

});

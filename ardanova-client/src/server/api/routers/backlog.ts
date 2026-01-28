import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// PBI enums
export const PBIType = z.enum(['FEATURE', 'ENHANCEMENT', 'BUG', 'TECHNICAL_DEBT', 'SPIKE']);
export const PBIStatus = z.enum(['NEW', 'READY', 'IN_PROGRESS', 'DONE', 'REMOVED']);

// BacklogItem enums
export const BacklogItemType = z.enum(['FEATURE', 'BUG', 'IMPROVEMENT', 'RESEARCH', 'DOCUMENTATION']);
export const BacklogStatus = z.enum(['NEW', 'REFINED', 'READY', 'IN_PROGRESS', 'DONE']);
export const Priority = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

// PBI schemas
const createPbiSchema = z.object({
  epicId: z.string().min(1),
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

// BacklogItem schemas
const createBacklogItemSchema = z.object({
  pbiId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  type: BacklogItemType.default('FEATURE'),
  priority: Priority.default('MEDIUM'),
  estimatedHours: z.number().int().min(0).optional(),
});

const updateBacklogItemSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  description: z.string().optional(),
  type: BacklogItemType.optional(),
  priority: Priority.optional(),
  status: BacklogStatus.optional(),
  estimatedHours: z.number().int().min(0).optional(),
  assigneeId: z.string().nullable().optional(),
});

export const backlogRouter = createTRPCRouter({
  // ========================================
  // PBI OPERATIONS
  // ========================================

  getPbisByEpicId: publicProcedure
    .input(z.object({ epicId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.pbis.getByEpicId(input.epicId);

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

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(input.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
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

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
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

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
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

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
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

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
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

  // ========================================
  // BACKLOG ITEM OPERATIONS
  // ========================================

  getItemsByPbiId: publicProcedure
    .input(z.object({ pbiId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.backlogItems.getByPbiId(input.pbiId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getItemById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.backlogItems.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Backlog item not found");
      }

      return response.data;
    }),

  createItem: protectedProcedure
    .input(createBacklogItemSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(input.pbiId);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.backlogItems.create(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create backlog item");
      }

      return response.data;
    }),

  updateItem: protectedProcedure
    .input(z.object({ id: z.string(), data: updateBacklogItemSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get backlog item to verify ownership
      const item = await apiClient.backlogItems.getById(input.id);
      if (item.error || !item.data) {
        throw new Error("Backlog item not found");
      }

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(item.data.pbiId);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.backlogItems.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update backlog item");
      }

      return response.data;
    }),

  deleteItem: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get backlog item to verify ownership
      const item = await apiClient.backlogItems.getById(input.id);
      if (item.error || !item.data) {
        throw new Error("Backlog item not found");
      }

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(item.data.pbiId);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.backlogItems.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete backlog item");
      }

      return { success: true };
    }),

  assignItem: protectedProcedure
    .input(z.object({ id: z.string(), userId: z.string().nullable() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get backlog item to verify ownership
      const item = await apiClient.backlogItems.getById(input.id);
      if (item.error || !item.data) {
        throw new Error("Backlog item not found");
      }

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(item.data.pbiId);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.backlogItems.assign(input.id, input.userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to assign backlog item");
      }

      return response.data;
    }),

  updateItemStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: BacklogStatus }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get backlog item to verify ownership
      const item = await apiClient.backlogItems.getById(input.id);
      if (item.error || !item.data) {
        throw new Error("Backlog item not found");
      }

      // Get PBI to verify ownership
      const pbi = await apiClient.pbis.getById(item.data.pbiId);
      if (pbi.error || !pbi.data) {
        throw new Error("PBI not found");
      }

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(pbi.data.epicId);
      if (epic.error || !epic.data) {
        throw new Error("Epic not found");
      }

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(epic.data.phaseId);
      if (phase.error || !phase.data) {
        throw new Error("Phase not found");
      }

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(phase.data.roadmapId);
      if (roadmap.error || !roadmap.data) {
        throw new Error("Roadmap not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(roadmap.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.backlogItems.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update backlog item status");
      }

      return response.data;
    }),
});

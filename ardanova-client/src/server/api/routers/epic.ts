import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const EpicStatus = z.enum(['DRAFT', 'READY', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED']);
export const EpicPriority = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

const createEpicSchema = z.object({
  phaseId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  priority: EpicPriority.default('MEDIUM'),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
});

const updateEpicSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  description: z.string().optional(),
  priority: EpicPriority.optional(),
  status: EpicStatus.optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  assigneeId: z.string().nullable().optional(),
});

export const epicRouter = createTRPCRouter({
  getByPhaseId: publicProcedure
    .input(z.object({ phaseId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.epics.getByPhaseId(input.phaseId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.epics.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Epic not found");
      }

      return response.data;
    }),

  create: protectedProcedure
    .input(createEpicSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(input.phaseId);
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

      const response = await apiClient.epics.create(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create epic");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateEpicSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(input.id);
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

      const response = await apiClient.epics.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update epic");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(input.id);
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

      const response = await apiClient.epics.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete epic");
      }

      return { success: true };
    }),

  assign: protectedProcedure
    .input(z.object({ id: z.string(), userId: z.string().nullable() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(input.id);
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

      const response = await apiClient.epics.assign(input.id, input.userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to assign epic");
      }

      return response.data;
    }),

  updateStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: EpicStatus }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(input.id);
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

      const response = await apiClient.epics.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update epic status");
      }

      return response.data;
    }),

  updatePriority: protectedProcedure
    .input(z.object({ id: z.string(), priority: EpicPriority }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get epic to verify ownership
      const epic = await apiClient.epics.getById(input.id);
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

      const response = await apiClient.epics.updatePriority(input.id, input.priority);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update epic priority");
      }

      return response.data;
    }),
});

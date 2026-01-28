import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const RoadmapStatus = z.enum(['DRAFT', 'ACTIVE', 'COMPLETED', 'ARCHIVED']);
export const PhaseStatus = z.enum(['NOT_STARTED', 'IN_PROGRESS', 'COMPLETED', 'ON_HOLD']);

const createRoadmapSchema = z.object({
  projectId: z.string().min(1),
  title: z.string().min(1).max(200),
  vision: z.string().optional(),
});

const updateRoadmapSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  vision: z.string().optional(),
  status: RoadmapStatus.optional(),
  assigneeId: z.string().nullable().optional(),
});

const createPhaseSchema = z.object({
  roadmapId: z.string().min(1),
  name: z.string().min(1).max(100),
  description: z.string().optional(),
  order: z.number().int().min(0),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
});

const updatePhaseSchema = z.object({
  name: z.string().min(1).max(100).optional(),
  description: z.string().optional(),
  order: z.number().int().min(0).optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  status: PhaseStatus.optional(),
});

export const roadmapRouter = createTRPCRouter({
  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.roadmaps.getByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? null;
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.roadmaps.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Roadmap not found");
      }

      return response.data;
    }),

  create: protectedProcedure
    .input(createRoadmapSchema)
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

      const response = await apiClient.roadmaps.create(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create roadmap");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateRoadmapSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(input.id);
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

      const response = await apiClient.roadmaps.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update roadmap");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(input.id);
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

      const response = await apiClient.roadmaps.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete roadmap");
      }

      return { success: true };
    }),

  assign: protectedProcedure
    .input(z.object({ id: z.string(), userId: z.string().nullable() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(input.id);
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

      const response = await apiClient.roadmaps.assign(input.id, input.userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to assign roadmap");
      }

      return response.data;
    }),

  updateStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: RoadmapStatus }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(input.id);
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

      const response = await apiClient.roadmaps.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update roadmap status");
      }

      return response.data;
    }),

  // ========================================
  // PHASE OPERATIONS
  // ========================================

  getPhases: publicProcedure
    .input(z.object({ roadmapId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.roadmaps.getPhases(input.roadmapId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getPhaseById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.roadmaps.getPhaseById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Phase not found");
      }

      return response.data;
    }),

  createPhase: protectedProcedure
    .input(createPhaseSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get roadmap to verify ownership
      const roadmap = await apiClient.roadmaps.getById(input.roadmapId);
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

      const response = await apiClient.roadmaps.createPhase(input);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create phase");
      }

      return response.data;
    }),

  updatePhase: protectedProcedure
    .input(z.object({ id: z.string(), data: updatePhaseSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(input.id);
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

      const response = await apiClient.roadmaps.updatePhase(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update phase");
      }

      return response.data;
    }),

  deletePhase: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(input.id);
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

      const response = await apiClient.roadmaps.deletePhase(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete phase");
      }

      return { success: true };
    }),

  updatePhaseStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: PhaseStatus }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get phase to verify ownership
      const phase = await apiClient.roadmaps.getPhaseById(input.id);
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

      const response = await apiClient.roadmaps.updatePhaseStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update phase status");
      }

      return response.data;
    }),
});

import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// PBI enums - match the API's PbiStatus type
export const PBIType = z.enum(['FEATURE', 'ENHANCEMENT', 'BUG', 'TECHNICAL_DEBT', 'SPIKE']);
export const PBIStatus = z.enum(['NEW', 'READY', 'IN_PROGRESS', 'DONE', 'CANCELLED']);

export const Priority = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

// PBI schemas - projectId required, all parent FKs optional (flexible hierarchy)
const createPbiSchema = z.object({
  projectId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  type: PBIType.default('FEATURE'),
  priority: Priority.default('MEDIUM'),
  storyPoints: z.number().int().min(0).optional(),
  acceptanceCriteria: z.string().optional(),
  // Flexible hierarchy - attach to any valid ancestor (all optional)
  featureId: z.string().optional(),
  sprintId: z.string().optional(),
  epicId: z.string().optional(),
  milestoneId: z.string().optional(),
  guildId: z.string().optional(),
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
  guildId: z.string().nullable().optional(),
});


export const backlogRouter = createTRPCRouter({
  // ========================================
  // PBI OPERATIONS
  // ========================================

  getPbisByFeatureId: publicProcedure
    .input(z.object({ featureId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.backlog.getPbisByFeatureId(input.featureId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getPbisByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.backlog.getPbisByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getPbiById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.backlog.getPbiById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "PBI not found");
      }

      return response.data;
    }),

  createPbi: protectedProcedure
    .input(createPbiSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.backlog.createPbi({
        projectId: input.projectId,
        title: input.title,
        description: input.description,
        type: input.type,
        priority: input.priority,
        storyPoints: input.storyPoints,
        acceptanceCriteria: input.acceptanceCriteria,
        featureId: input.featureId,
        sprintId: input.sprintId,
        epicId: input.epicId,
        milestoneId: input.milestoneId,
        guildId: input.guildId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create PBI");
      }

      return response.data;
    }),

  updatePbi: protectedProcedure
    .input(z.object({ id: z.string(), data: updatePbiSchema }))
    .mutation(async ({ input }) => {
      const response = await apiClient.backlog.updatePbi(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update PBI");
      }

      return response.data;
    }),

  deletePbi: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.backlog.deletePbi(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete PBI");
      }

      return { success: true };
    }),

  assignPbi: protectedProcedure
    .input(z.object({ id: z.string(), userId: z.string().nullable() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.backlog.assignPbi(input.id, input.userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to assign PBI");
      }

      return response.data;
    }),

  updatePbiStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: PBIStatus }))
    .mutation(async ({ input }) => {
      const response = await apiClient.backlog.updatePbiStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update PBI status");
      }

      return response.data;
    }),

});

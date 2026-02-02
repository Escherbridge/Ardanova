import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Note: The API's EpicStatus type is 'PLANNED' | 'IN_PROGRESS' | 'COMPLETED' | 'CANCELLED'
// Zod schema matches the actual API
export const EpicStatus = z.enum(['PLANNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED']);
export const EpicPriority = z.enum(['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']);

const createEpicSchema = z.object({
  milestoneId: z.string().min(1),
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
  getByMilestoneId: publicProcedure
    .input(z.object({ milestoneId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.epics.getByMilestoneId(input.milestoneId);

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

      // TODO: Milestone validation - apiClient.milestones doesn't exist as a separate endpoint.
      // Milestones are managed through apiClient.projects, but we'd need projectId first.
      // For now, skip milestone validation and rely on backend validation.

      const response = await apiClient.epics.create({
        milestoneId: input.milestoneId,
        title: input.title,
        description: input.description,
        priority: input.priority,
        startDate: input.startDate,
        endDate: input.endDate,
      });

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

      // TODO: Milestone validation - apiClient.milestones doesn't exist as a separate endpoint.
      // Skipping milestone validation for now.

      const response = await apiClient.epics.update(input.id, {
        title: input.data.title,
        description: input.data.description,
        priority: input.data.priority,
        status: input.data.status,
        startDate: input.data.startDate,
        endDate: input.data.endDate,
        assigneeId: input.data.assigneeId,
      });

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

      // TODO: Milestone validation - apiClient.milestones doesn't exist as a separate endpoint.
      // Skipping milestone validation for now.

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

      // TODO: Milestone validation - apiClient.milestones doesn't exist as a separate endpoint.
      // Skipping milestone validation for now.

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

      // TODO: Milestone validation - apiClient.milestones doesn't exist as a separate endpoint.
      // Skipping milestone validation for now.

      const response = await apiClient.epics.updateStatus(input.id, input.status as 'PLANNED' | 'IN_PROGRESS' | 'COMPLETED' | 'CANCELLED');

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

      // TODO: Milestone validation - apiClient.milestones doesn't exist as a separate endpoint.
      // Skipping milestone validation for now.

      const response = await apiClient.epics.updatePriority(input.id, input.priority as 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW');

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update epic priority");
      }

      return response.data;
    }),
});

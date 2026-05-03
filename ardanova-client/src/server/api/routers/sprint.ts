import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { CreateSprint } from "~/lib/api/ardanova/endpoints/sprints";
import { authorizeChildCreation, authorizeRootCreation } from "~/server/api/lib/hierarchy-auth";

export const SprintStatus = z.enum(['PLANNED', 'ACTIVE', 'COMPLETED', 'CANCELLED']);

const createSprintSchema = z.object({
  projectId: z.string().min(1),
  epicId: z.string().optional(),
  name: z.string().min(1).max(100),
  goal: z.string().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
});

const updateSprintSchema = z.object({
  name: z.string().min(1).max(100).optional(),
  goal: z.string().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  status: SprintStatus.optional(),
});

export const sprintRouter = createTRPCRouter({
  getByEpicId: publicProcedure
    .input(z.object({ epicId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.sprints.getByEpicId(input.epicId);

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

      // Auth: if attaching to epic, must be assignee or project manager
      if (input.epicId) {
        await authorizeChildCreation(
          { userId, projectId: input.projectId },
          "epic",
          input.epicId
        );
      } else {
        await authorizeRootCreation({ userId, projectId: input.projectId });
      }

      const response = await apiClient.sprints.create({ ...input } as CreateSprint);

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

      const response = await apiClient.sprints.cancel(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to cancel sprint");
      }

      return response.data;
    }),
});

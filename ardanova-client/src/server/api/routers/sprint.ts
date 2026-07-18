import { z } from "zod";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { CreateSprint } from "~/lib/api/ardanova/endpoints/sprints";
import { hierarchyAuthorization } from "~/server/api/lib/hierarchy-auth";

export const SprintStatus = z.enum([
  "PLANNED",
  "ACTIVE",
  "COMPLETED",
  "CANCELLED",
]);

const createSprintSchema = z.object({
  projectId: z.string().min(1),
  epicId: z.string().min(1),
  name: z.string().min(1).max(100),
  goal: z.string().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  equityBudget: z.number().nonnegative().optional(),
  assigneeId: z.string().min(1).optional(),
});

const updateSprintSchema = z
  .object({
    name: z.string().min(1).max(100).optional(),
    goal: z.string().optional(),
    startDate: z.string().datetime().optional(),
    endDate: z.string().datetime().optional(),
    equityBudget: z.number().nonnegative().optional(),
    velocity: z.number().nonnegative().optional(),
    status: SprintStatus.optional(),
  })
  .strict();

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

      await hierarchyAuthorization.authorizeCreation(
        {
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        },
        [{ level: "epic", id: input.epicId }],
      );
      if (input.assigneeId || input.equityBudget !== undefined) {
        await hierarchyAuthorization.requireProjectManager({
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        });
      }
      if (input.assigneeId) {
        await hierarchyAuthorization.requireProjectMember(
          input.projectId,
          input.assigneeId,
        );
      }

      const response = await apiClient.sprints.create({
        epicId: input.epicId,
        name: input.name,
        goal: input.goal,
        startDate: input.startDate,
        endDate: input.endDate,
        equityBudget: input.equityBudget,
        assigneeId: input.assigneeId,
      } satisfies CreateSprint);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create sprint");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateSprintSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "sprint",
        input.id,
        input.data.equityBudget !== undefined ? "structure" : "work",
        ctx.session.user.role === "ADMIN",
      );

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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "sprint",
        input.id,
        "structure",
        ctx.session.user.role === "ADMIN",
      );

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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "sprint",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "sprint",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "sprint",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.sprints.cancel(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to cancel sprint");
      }

      return response.data;
    }),
});

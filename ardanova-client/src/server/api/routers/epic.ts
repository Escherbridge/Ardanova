import { z } from "zod";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { hierarchyAuthorization } from "~/server/api/lib/hierarchy-auth";

// Note: The API's EpicStatus type is 'PLANNED' | 'IN_PROGRESS' | 'COMPLETED' | 'CANCELLED'
// Zod schema matches the actual API
export const EpicStatus = z.enum([
  "PLANNED",
  "IN_PROGRESS",
  "COMPLETED",
  "CANCELLED",
]);
export const EpicPriority = z.enum(["CRITICAL", "HIGH", "MEDIUM", "LOW"]);

const createEpicSchema = z.object({
  projectId: z.string().min(1),
  milestoneId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  priority: EpicPriority.default("MEDIUM"),
  equityBudget: z.number().nonnegative().optional(),
  startDate: z.string().datetime().optional(),
  targetDate: z.string().datetime().optional(),
  assigneeId: z.string().min(1).optional(),
});

const updateEpicSchema = z
  .object({
    title: z.string().min(1).max(200).optional(),
    description: z.string().optional(),
    priority: EpicPriority.optional(),
    status: EpicStatus.optional(),
    equityBudget: z.number().nonnegative().optional(),
    progress: z.number().min(0).max(100).optional(),
    startDate: z.string().datetime().optional(),
    targetDate: z.string().datetime().optional(),
  })
  .strict();

export const epicRouter = createTRPCRouter({
  getByMilestoneId: publicProcedure
    .input(z.object({ milestoneId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.epics.getByMilestoneId(
        input.milestoneId,
      );

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

      await hierarchyAuthorization.authorizeCreation(
        {
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        },
        [{ level: "milestone", id: input.milestoneId }],
      );
      if (input.assigneeId) {
        await hierarchyAuthorization.requireProjectManager({
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        });
        await hierarchyAuthorization.requireProjectMember(
          input.projectId,
          input.assigneeId,
        );
      }

      const response = await apiClient.epics.create({
        milestoneId: input.milestoneId,
        title: input.title,
        description: input.description,
        priority: input.priority,
        equityBudget: input.equityBudget,
        startDate: input.startDate,
        targetDate: input.targetDate,
        assigneeId: input.assigneeId,
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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "epic",
        input.id,
        input.data.equityBudget !== undefined ? "structure" : "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.epics.update(input.id, {
        title: input.data.title,
        description: input.data.description,
        priority: input.data.priority,
        status: input.data.status,
        equityBudget: input.data.equityBudget,
        progress: input.data.progress,
        startDate: input.data.startDate,
        targetDate: input.data.targetDate,
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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "epic",
        input.id,
        "structure",
        ctx.session.user.role === "ADMIN",
      );

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

      const epic = await hierarchyAuthorization.authorizeMutation(
        userId,
        "epic",
        input.id,
        "structure",
        ctx.session.user.role === "ADMIN",
      );
      if (input.userId) {
        await hierarchyAuthorization.requireProjectMember(
          epic.projectId,
          input.userId,
        );
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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "epic",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.epics.updateStatus(
        input.id,
        input.status,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update epic status");
      }

      return response.data;
    }),

  updatePriority: protectedProcedure
    .input(z.object({ id: z.string(), priority: EpicPriority }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "epic",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.epics.updatePriority(
        input.id,
        input.priority,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update epic priority");
      }

      return response.data;
    }),
});

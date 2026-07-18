import { z } from "zod";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { hierarchyAuthorization } from "~/server/api/lib/hierarchy-auth";

export const FeatureStatusEnum = z.enum([
  "PLANNED",
  "IN_PROGRESS",
  "COMPLETED",
  "CANCELLED",
]);
export const FeaturePriority = z.enum(["CRITICAL", "HIGH", "MEDIUM", "LOW"]);

const createFeatureSchema = z.object({
  projectId: z.string().min(1),
  sprintId: z.string().min(1),
  title: z.string().min(1).max(200),
  description: z.string().optional(),
  priority: FeaturePriority.optional(),
  order: z.number().optional(),
});

const updateFeatureSchema = z
  .object({
    title: z.string().min(1).max(200).optional(),
    description: z.string().optional(),
    priority: FeaturePriority.optional(),
    status: FeatureStatusEnum.optional(),
    order: z.number().optional(),
  })
  .strict();

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

      await hierarchyAuthorization.authorizeCreation(
        {
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        },
        [{ level: "sprint", id: input.sprintId }],
      );
      if (input.order !== undefined) {
        await hierarchyAuthorization.requireProjectManager({
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        });
      }

      const response = await apiClient.features.create({
        sprintId: input.sprintId,
        title: input.title,
        description: input.description,
        priority: input.priority,
        order: input.order,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create feature");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateFeatureSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "feature",
        input.id,
        input.data.order !== undefined ? "structure" : "work",
        ctx.session.user.role === "ADMIN",
      );

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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "feature",
        input.id,
        "structure",
        ctx.session.user.role === "ADMIN",
      );

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

      const feature = await hierarchyAuthorization.authorizeMutation(
        userId,
        "feature",
        input.id,
        "structure",
        ctx.session.user.role === "ADMIN",
      );
      if (input.userId) {
        await hierarchyAuthorization.requireProjectMember(
          feature.projectId,
          input.userId,
        );
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

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "feature",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.features.updateStatus(
        input.id,
        input.status,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update feature status");
      }

      return response.data;
    }),

  updatePriority: protectedProcedure
    .input(z.object({ id: z.string(), priority: FeaturePriority }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      await hierarchyAuthorization.authorizeMutation(
        userId,
        "feature",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.features.updatePriority(
        input.id,
        input.priority,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update feature priority");
      }

      return response.data;
    }),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const activityRouter = createTRPCRouter({
  getMyActivity: protectedProcedure
    .input(
      z.object({
        page: z.number().min(1).default(1),
        pageSize: z.number().min(1).max(100).default(20),
      })
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.activities.getByUserIdPaged(userId, input.page, input.pageSize);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? { items: [], page: input.page, pageSize: input.pageSize, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false };
    }),

  getByUserId: publicProcedure
    .input(
      z.object({
        userId: z.string(),
        page: z.number().min(1).optional(),
        pageSize: z.number().min(1).max(100).optional(),
      })
    )
    .query(async ({ input }) => {
      if (input.page !== undefined && input.pageSize !== undefined) {
        const response = await apiClient.activities.getByUserIdPaged(input.userId, input.page, input.pageSize);
        if (response.error) {
          throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
        }
        return response.data ?? [];
      }
      const response = await apiClient.activities.getByUserId(input.userId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  getByProjectId: publicProcedure
    .input(
      z.object({
        projectId: z.string(),
        page: z.number().min(1).optional(),
        pageSize: z.number().min(1).max(100).optional(),
      })
    )
    .query(async ({ input }) => {
      if (input.page !== undefined && input.pageSize !== undefined) {
        const response = await apiClient.activities.getByProjectIdPaged(input.projectId, input.page, input.pageSize);
        if (response.error) {
          throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
        }
        return response.data ?? [];
      }
      const response = await apiClient.activities.getByProjectId(input.projectId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const notificationRouter = createTRPCRouter({
  getAll: protectedProcedure
    .input(
      z.object({
        page: z.number().min(1).default(1),
        pageSize: z.number().min(1).max(100).default(20),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.notifications.getPaged(input.page, input.pageSize);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? { items: [], page: input.page, pageSize: input.pageSize, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false };
    }),

  getUnread: protectedProcedure.query(async () => {
    const response = await apiClient.notifications.getUnread();
    if (response.error) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
    }
    return response.data ?? [];
  }),

  getUnreadCount: protectedProcedure.query(async () => {
    const response = await apiClient.notifications.getSummary();
    if (response.error || !response.data) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to load summary" });
    }
    return response.data.unreadCount;
  }),

  getSummary: protectedProcedure.query(async () => {
    const response = await apiClient.notifications.getSummary();
    if (response.error || !response.data) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to load summary" });
    }
    return response.data;
  }),

  markAsRead: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.notifications.markAsRead(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to mark read" });
      }
      return response.data;
    }),

  markAllAsRead: protectedProcedure.mutation(async () => {
    const response = await apiClient.notifications.markAllAsRead();
    if (response.error) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
    }
    return response.data ?? { success: true };
  }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.notifications.delete(input.id);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return { success: true };
    }),

  deleteAll: protectedProcedure.mutation(async () => {
    const response = await apiClient.notifications.deleteAll();
    if (response.error) {
      throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
    }
    return { success: true };
  }),
});

import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const postRouter = createTRPCRouter({
  getFeed: publicProcedure
    .input(
      z.object({
        page: z.number().min(1).default(1),
        pageSize: z.number().min(1).max(100).default(20),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.posts.getFeed(input.page, input.pageSize);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data;
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.posts.getById(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "NOT_FOUND", message: response.error ?? "Post not found" });
      }
      return response.data;
    }),

  getByUserId: publicProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.posts.getByUserId(input.userId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  create: protectedProcedure
    .input(
      z.object({
        projectId: z.string().optional(),
        guildId: z.string().optional(),
        type: z.string().optional(),
        visibility: z.string().optional(),
        title: z.string().optional(),
        content: z.string().min(1),
        metadata: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const authorId = ctx.session.user.id;
      const response = await apiClient.posts.create({
        authorId,
        content: input.content,
        projectId: input.projectId,
        guildId: input.guildId,
        type: input.type,
        visibility: input.visibility,
        title: input.title,
        metadata: input.metadata,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to create post" });
      }
      return response.data;
    }),

  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        title: z.string().optional(),
        content: z.string().optional(),
        visibility: z.string().optional(),
        metadata: z.string().optional(),
        isPinned: z.boolean().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const authorId = ctx.session.user.id;
      const { id, ...data } = input;
      const response = await apiClient.posts.update(id, authorId, data);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to update post" });
      }
      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const authorId = ctx.session.user.id;
      const response = await apiClient.posts.delete(input.id, authorId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return { success: true };
    }),

  like: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.posts.like(input.id, userId);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to like" });
      }
      return response.data;
    }),

  unlike: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.posts.unlike(input.id, userId);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to unlike" });
      }
      return response.data;
    }),

  share: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        sharedToProjectId: z.string().optional(),
        sharedToGuildId: z.string().optional(),
        comment: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const { id, ...rest } = input;
      const hasPayload = rest.sharedToProjectId ?? rest.sharedToGuildId ?? rest.comment;
      const response = await apiClient.posts.share(id, userId, hasPayload ? rest : undefined);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to share" });
      }
      return response.data;
    }),

  bookmark: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.posts.bookmark(input.id, userId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? true;
    }),

  unbookmark: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.posts.unbookmark(input.id, userId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? true;
    }),

  getComments: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.posts.getComments(input.id);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return response.data ?? [];
    }),

  addComment: protectedProcedure
    .input(
      z.object({
        postId: z.string(),
        parentId: z.string().optional(),
        content: z.string().min(1),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const authorId = ctx.session.user.id;
      const response = await apiClient.posts.addComment(input.postId, {
        authorId,
        parentId: input.parentId,
        content: input.content,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to add comment" });
      }
      return response.data;
    }),

  deleteComment: protectedProcedure
    .input(
      z.object({
        postId: z.string(),
        commentId: z.string(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const authorId = ctx.session.user.id;
      const response = await apiClient.posts.deleteComment(input.postId, input.commentId, authorId);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return { success: true };
    }),
});

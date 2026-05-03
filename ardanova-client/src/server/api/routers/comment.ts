import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

const CommentTargetType = z.enum([
  "PROJECT",
  "MILESTONE",
  "EPIC",
  "SPRINT",
  "FEATURE",
  "PBI",
  "TASK",
]);

export const commentRouter = createTRPCRouter({
  // Get comments for any target entity
  getByTarget: publicProcedure
    .input(
      z.object({
        targetType: CommentTargetType,
        targetId: z.string(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.projects.getCommentsByTarget(
        input.targetType,
        input.targetId
      );

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Add a comment to any target entity
  add: protectedProcedure
    .input(
      z.object({
        projectId: z.string(),
        targetType: CommentTargetType,
        targetId: z.string(),
        content: z.string().min(1, "Comment cannot be empty"),
        parentId: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.projects.addComment(input.projectId, {
        userId,
        content: input.content,
        parentId: input.parentId,
        targetType: input.targetType,
        targetId: input.targetId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add comment");
      }

      return response.data;
    }),

  // Delete a comment
  delete: protectedProcedure
    .input(
      z.object({
        projectId: z.string(),
        commentId: z.string(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.projects.deleteComment(
        input.projectId,
        input.commentId
      );

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete comment");
      }

      return { success: true };
    }),
});

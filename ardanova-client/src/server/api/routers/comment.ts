import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  publicProcedure,
  protectedProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import {
  canManageProject,
  hierarchyAuthorization,
  type HierarchyLevel,
} from "~/server/api/lib/hierarchy-auth";

const CommentTargetType = z.enum([
  "PROJECT",
  "MILESTONE",
  "EPIC",
  "SPRINT",
  "FEATURE",
  "PBI",
  "TASK",
]);

type CommentTarget = z.infer<typeof CommentTargetType>;

const HIERARCHY_LEVEL_BY_TARGET: Partial<
  Record<CommentTarget, HierarchyLevel>
> = {
  MILESTONE: "milestone",
  EPIC: "epic",
  SPRINT: "sprint",
  FEATURE: "feature",
  PBI: "pbi",
  TASK: "task",
};

async function requireTargetProject(
  targetType: CommentTarget,
  targetId: string,
  projectId: string,
): Promise<void> {
  if (targetType === "PROJECT") {
    const project = await apiClient.projects.getById(projectId);
    if (
      targetId !== projectId ||
      project.error ||
      project.data?.id !== projectId
    ) {
      throw new TRPCError({
        code: "BAD_REQUEST",
        message: "Comment target does not belong to this project",
      });
    }
    return;
  }

  const level = HIERARCHY_LEVEL_BY_TARGET[targetType];
  if (!level) {
    throw new TRPCError({
      code: "BAD_REQUEST",
      message: "Unsupported comment target",
    });
  }
  await hierarchyAuthorization.resolve(level, targetId, projectId);
}

export const commentRouter = createTRPCRouter({
  // Get comments for any target entity
  getByTarget: publicProcedure
    .input(
      z.object({
        targetType: CommentTargetType,
        targetId: z.string(),
      }),
    )
    .query(async ({ input }) => {
      const response = await apiClient.projects.getCommentsByTarget(
        input.targetType,
        input.targetId,
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
      }),
    )
    .mutation(async ({ input }) => {
      await requireTargetProject(
        input.targetType,
        input.targetId,
        input.projectId,
      );
      if (input.parentId) {
        const parent = await apiClient.projects.getCommentById(
          input.projectId,
          input.parentId,
        );
        if (
          parent.error ||
          !parent.data ||
          parent.data.projectId !== input.projectId ||
          parent.data.targetType !== input.targetType ||
          parent.data.targetId !== input.targetId
        ) {
          throw new TRPCError({
            code: "BAD_REQUEST",
            message: "Reply parent does not match this comment target",
          });
        }
      }

      const response = await apiClient.projects.addComment(input.projectId, {
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
      }),
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const comment = await apiClient.projects.getCommentById(
        input.projectId,
        input.commentId,
      );
      if (comment.error || !comment.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Comment not found",
        });
      }
      if (comment.data.projectId !== input.projectId) {
        throw new TRPCError({
          code: "FORBIDDEN",
          message: "Comment does not belong to this project",
        });
      }
      if (
        comment.data.userId !== userId &&
        !(await canManageProject({
          userId,
          projectId: comment.data.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        }))
      ) {
        throw new TRPCError({
          code: "FORBIDDEN",
          message:
            "Only the author or a project manager can delete this comment",
        });
      }

      const response = await apiClient.projects.deleteComment(
        comment.data.projectId,
        input.commentId,
      );

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete comment");
      }

      return { success: true };
    }),
});

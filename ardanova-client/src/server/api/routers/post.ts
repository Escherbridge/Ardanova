import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  protectedProcedure,
  publicProcedure,
} from "~/server/api/trpc";
import { runWithActorAssertion } from "~/server/actor-assertion";
import { apiClient } from "~/lib/api";

/**
 * Field notes are project-scoped; there is no global feed.
 *
 * Writes are `protectedProcedure`, so the API takes the author from the signed actor
 * assertion and a caller can never post as someone else. Reads stay public, but attach
 * the reader as an actor when a session exists: signed out, the API returns only notes
 * shared with everyone; signed in, it adds the ones that reader is entitled to see.
 */

type FieldNoteReader =
  | { id?: string | null; role?: string }
  | null
  | undefined;

/** Runs a read with the reader attached as the actor, or anonymously when signed out. */
async function readAsViewer<T>(
  reader: FieldNoteReader,
  call: () => Promise<T>,
): Promise<T> {
  if (!reader?.id) return call();
  return runWithActorAssertion({ subject: reader.id, role: reader.role }, call);
}

const PostTypeSchema = z.enum([
  "POST",
  "PROJECT_UPDATE",
  "GUILD_ACTIVITY",
  "TASK_COMPLETED",
  "MILESTONE",
  "PROPOSAL",
  "SHOP_ITEM",
]);

const PostVisibilitySchema = z.enum([
  "PUBLIC",
  "PROJECT_MEMBERS",
  "GUILD_MEMBERS",
  "PRIVATE",
]);

export const postRouter = createTRPCRouter({
  getProjectFieldNotes: publicProcedure
    .input(
      z.object({
        projectId: z.string().min(1),
        page: z.number().min(1).default(1),
        pageSize: z.number().min(1).max(100).default(20),
      }),
    )
    .query(async ({ input, ctx }) => {
      const response = await readAsViewer(ctx.session?.user, () =>
        apiClient.posts.getProjectFieldNotes(
          input.projectId,
          input.page,
          input.pageSize,
        ),
      );
      if (response.error) {
        // A project this reader may not see 404s at the API. Reporting that as a 500
        // would tell the browser the node broke when in fact it answered correctly.
        throw new TRPCError({
          code: response.status === 404 ? "NOT_FOUND" : "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data;
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input, ctx }) => {
      const response = await readAsViewer(ctx.session?.user, () =>
        apiClient.posts.getById(input.id),
      );
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "Field note not found",
        });
      }
      return response.data;
    }),

  getByUserId: publicProcedure
    .input(z.object({ userId: z.string() }))
    .query(async ({ input, ctx }) => {
      const response = await readAsViewer(ctx.session?.user, () =>
        apiClient.posts.getByUserId(input.userId),
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  create: protectedProcedure
    .input(
      z.object({
        projectId: z.string().optional(),
        guildId: z.string().optional(),
        type: PostTypeSchema.optional(),
        visibility: PostVisibilitySchema.optional(),
        title: z.string().optional(),
        content: z.string().min(1),
        metadata: z.string().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.create({
        content: input.content,
        projectId: input.projectId,
        guildId: input.guildId,
        type: input.type,
        visibility: input.visibility,
        title: input.title,
        metadata: input.metadata,
      });
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to post this field note",
        });
      }
      return response.data;
    }),

  update: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        title: z.string().optional(),
        content: z.string().optional(),
        visibility: PostVisibilitySchema.optional(),
        metadata: z.string().optional(),
        isPinned: z.boolean().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const { id, ...data } = input;
      const response = await apiClient.posts.update(id, data);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to update this field note",
        });
      }
      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.delete(input.id);
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return { success: true };
    }),

  like: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.like(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to like",
        });
      }
      return response.data;
    }),

  unlike: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.unlike(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to unlike",
        });
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
      }),
    )
    .mutation(async ({ input }) => {
      const { id, ...rest } = input;
      const hasPayload =
        rest.sharedToProjectId ?? rest.sharedToGuildId ?? rest.comment;
      const response = await apiClient.posts.share(
        id,
        hasPayload ? rest : undefined,
      );
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to share",
        });
      }
      return response.data;
    }),

  bookmark: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.bookmark(input.id);
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? true;
    }),

  unbookmark: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.unbookmark(input.id);
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? true;
    }),

  getComments: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input, ctx }) => {
      const response = await readAsViewer(ctx.session?.user, () =>
        apiClient.posts.getComments(input.id),
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return response.data ?? [];
    }),

  addComment: protectedProcedure
    .input(
      z.object({
        postId: z.string(),
        parentId: z.string().optional(),
        content: z.string().min(1),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.addComment(input.postId, {
        parentId: input.parentId,
        content: input.content,
      });
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to add reply",
        });
      }
      return response.data;
    }),

  deleteComment: protectedProcedure
    .input(
      z.object({
        postId: z.string(),
        commentId: z.string(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.posts.deleteComment(
        input.postId,
        input.commentId,
      );
      if (response.error) {
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: response.error,
        });
      }
      return { success: true };
    }),
});
